using System;
using System.IO;
using System.Net.Http;
using System.ServiceModel;
using System.Threading.Tasks;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.InstallationSupportUtility.SystemManagerInterface.Messages.SystemListMessage;
using Humanizer;
using Tewl.IO;

namespace EnterpriseWebLibrary.InstallationSupportUtility {
	public static class SystemManagerConnectionStatics {
		/// <summary>
		/// This should only be used by the Installation Support Utility.
		/// </summary>
		public static readonly string DownloadedDataPackagesFolderPath = EwlStatics.CombinePaths( ConfigurationStatics.EwlFolderPath, "Downloaded Data Packages" );

		public static SystemList SystemList { get; private set; }

		private static ChannelFactory<SystemManagerInterface.ServiceContracts.Isu> isuServiceFactory;
		private static ChannelFactory<SystemManagerInterface.ServiceContracts.ProgramRunner> programRunnerServiceFactory;

		public static void Init() {
			RefreshSystemList();

			isuServiceFactory = getNetTcpChannelFactory<SystemManagerInterface.ServiceContracts.Isu>( "Isu.svc" );
			programRunnerServiceFactory = getNetTcpChannelFactory<SystemManagerInterface.ServiceContracts.ProgramRunner>( "ProgramRunner.svc" );
		}

		/// <summary>
		/// Gets a new system list.
		/// </summary>
		public static void RefreshSystemList() {
			// Do not perform schema validation since we don't want to be forced into redeploying Program Runner after every schema change. We also don't have access
			// to the schema on non-development machines.
			var cacheFilePath = EwlStatics.CombinePaths( ConfigurationStatics.EwlFolderPath, "System List.xml" );
			var cacheUsed = false;
			try {
				ExecuteWithSystemManagerClient(
					client => {
						Task.Run(
								async () => {
									using( var response = await client.GetAsync( "system-list", HttpCompletionOption.ResponseHeadersRead ) ) {
										response.EnsureSuccessStatusCode();
										using( var stream = await response.Content.ReadAsStreamAsync() )
											SystemList = XmlOps.DeserializeFromStream<SystemList>( stream, false );
									}
								} )
							.Wait();
					} );
			}
			catch( Exception e ) {
				// Use the cached version of the system list if it is available.
				if( File.Exists( cacheFilePath ) ) {
					SystemList = XmlOps.DeserializeFromFile<SystemList>( cacheFilePath, false );
					cacheUsed = true;
				}
				else
					throw new UserCorrectableException( "Failed to download the system list and a cached version is not available.", e );
			}

			StatusStatics.SetStatus(
				cacheUsed ? "Failed to download the system list; loaded a cached version from \"{0}\".".FormatWith( cacheFilePath ) : "Downloaded the system list." );

			// Cache the system list so something is available in the future if the machine is offline.
			try {
				XmlOps.SerializeIntoFile( SystemList, cacheFilePath );
			}
			catch( Exception e ) {
				const string generalMessage = "Failed to cache the system list on disk.";
				if( e is UnauthorizedAccessException )
					throw new UserCorrectableException( generalMessage + " If the program is running as a non built in administrator, you may need to disable UAC.", e );

				// An IOException probably means the file is locked. In this case we want to ignore the problem and move on.
				if( !( e is IOException ) )
					throw new UserCorrectableException( generalMessage, e );
			}
		}

		private static ChannelFactory<T> getHttpChannelFactory<T>( string serviceFileName ) {
			var binding = new BasicHttpBinding { Security = { Mode = BasicHttpSecurityMode.Transport } };
			binding.ReaderQuotas.MaxStringContentLength = int.MaxValue;
			binding.SendTimeout = TimeSpan.FromMinutes( 10 );

			// This prevents certificate validation problems on dev machines with self-signed certificates.
			// This can probably be done anywhere before we try to get the system list. 
			if( SystemManager.HttpBaseUrl.StartsWith( "https://localhost" ) )
				System.Net.ServicePointManager.ServerCertificateValidationCallback = ( ( sender, certificate, chain, sslPolicyErrors ) => true );

			return new ChannelFactory<T>( binding, Tewl.Tools.NetTools.CombineUrls( SystemManager.HttpBaseUrl, "Service/" + serviceFileName ) );
		}

		private static ChannelFactory<T> getNetTcpChannelFactory<T>( string serviceFileName ) {
			var binding = new NetTcpBinding
				{
					// Troubleshooting (we give unique values to timeouts so we can figure out which one is the culprit if we have a problem).
					TransferMode = TransferMode.Streamed,
					SendTimeout = TimeSpan.FromMinutes( 123 ),
					MaxReceivedMessageSize = long.MaxValue,
					// Ideally, ReceiveTimeout (which is really inactivity timeout) would be very low. We've found that 3 minutes is too low, though.
					ReceiveTimeout = TimeSpan.FromMinutes( 6 ),
					OpenTimeout = TimeSpan.FromMinutes( 17 ),
					CloseTimeout = TimeSpan.FromMinutes( 23 )
				};

			// Performance
			binding.MaxBufferSize = binding.ReaderQuotas.MaxBytesPerRead = 65536;

			var factory = new ChannelFactory<T>( binding, Tewl.Tools.NetTools.CombineUrls( SystemManager.TcpBaseUrl, "Service/" + serviceFileName ) );
			factory.Credentials.Windows.ClientCredential.UserName = SystemManager.TcpUsername;
			factory.Credentials.Windows.ClientCredential.Password = SystemManager.TcpPassword;
			return factory;
		}

		public static void ExecuteWithSystemManagerClient( Action<HttpClient> method, bool useLongTimeouts = false ) {
			using( var client = new HttpClient() ) {
				client.Timeout = useLongTimeouts ? new TimeSpan( 0, 2, 0 ) : new TimeSpan( 0, 0, 10 );
				client.BaseAddress = new Uri( SystemManager.HttpBaseUrl + "/" );
				client.DefaultRequestHeaders.TryAddWithoutValidation( "Authorization", SystemManager.AccessToken );

				method( client );
			}
		}

		/// <summary>
		/// The action should be a noun, e.g. "logic package download".
		/// </summary>
		public static void ExecuteIsuServiceMethod( Action<SystemManagerInterface.ServiceContracts.Isu> method, string action ) {
			executeWebMethod( method, isuServiceFactory, action );
		}

		/// <summary>
		/// The action should be a noun, e.g. "logic package download".
		/// </summary>
		public static void ExecuteProgramRunnerServiceMethod( Action<SystemManagerInterface.ServiceContracts.ProgramRunner> method, string action ) {
			executeWebMethod( method, programRunnerServiceFactory, action );
		}

		/// <summary>
		/// The action should be a noun, e.g. "logic package download".
		/// </summary>
		public static T ExecuteIsuServiceMethod<T>( Func<SystemManagerInterface.ServiceContracts.Isu, T> method, string action ) {
			return executeWebMethodWithResult( method, isuServiceFactory, action );
		}

		/// <summary>
		/// The action should be a noun, e.g. "logic package download".
		/// </summary>
		public static T ExecuteProgramRunnerServiceMethod<T>( Func<SystemManagerInterface.ServiceContracts.ProgramRunner, T> method, string action ) {
			return executeWebMethodWithResult( method, programRunnerServiceFactory, action );
		}

		private static void executeWebMethod<ContractType>( Action<ContractType> method, ChannelFactory<ContractType> factory, string action ) {
			StatusStatics.SetStatus( "Performing " + action + "." );
			try {
				using( var channel = (IDisposable)factory.CreateChannel() )
					method( (ContractType)channel );
			}
			catch( Exception e ) {
				throw createWebServiceException( action, e );
			}
			StatusStatics.SetStatus( "Performed " + action + "." );
		}

		private static ResultType executeWebMethodWithResult<ContractType, ResultType>(
			Func<ContractType, ResultType> method, ChannelFactory<ContractType> factory, string action ) {
			StatusStatics.SetStatus( "Performing " + action + "." );
			ResultType ret;
			try {
				using( var channel = (IDisposable)factory.CreateChannel() )
					ret = method( (ContractType)channel );
			}
			catch( Exception e ) {
				throw createWebServiceException( action, e );
			}
			StatusStatics.SetStatus( "Performed " + action + "." );
			return ret;
		}

		private static Exception createWebServiceException( string action, Exception innerException ) {
			var generalMessage = "Failed during " + action + ".";
			if( innerException is EndpointNotFoundException )
				throw new UserCorrectableException(
					generalMessage + " The web service could not be reached - this could be due to a network error or a configuration error.",
					innerException );
			if( innerException is FaultException )
				// We do not pass the fault exception as an inner exception because its message includes a big ugly stack trace.
				throw new UserCorrectableException(
					generalMessage +
					" The web service was reachable but did not execute properly. This could be due to a database error on the server. Try again, as these types of errors usually do not persist." );

			// EndpointNotFoundException and FaultException are derived from CommunicationException, so their conditions must be before this condition.
			if( innerException is CommunicationException )
				throw new UserCorrectableException( generalMessage + " There was a network problem.", innerException );

			throw new UserCorrectableException(
				generalMessage + " This may have been caused by a network problem. The exception type is " + innerException.GetType().Name + ".",
				innerException );
		}

		public static string SystemManagerAccessToken => SystemManager.AccessToken;

		public static Configuration.Machine.MachineConfigurationSystemManager SystemManager {
			get {
				if( ConfigurationStatics.MachineConfiguration == null || ConfigurationStatics.MachineConfiguration.SystemManager == null )
					throw new UserCorrectableException( "Missing System Manager element in machine configuration file." );
				return ConfigurationStatics.MachineConfiguration.SystemManager;
			}
		}
	}
}