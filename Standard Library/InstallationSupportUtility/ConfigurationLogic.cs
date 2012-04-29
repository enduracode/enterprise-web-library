using System;
using System.IO;
using System.ServiceModel;
using RedStapler.StandardLibrary.IO;
using RedStapler.StandardLibrary.InstallationSupportUtility.RsisInterface.Messages.SystemListMessage;

namespace RedStapler.StandardLibrary.InstallationSupportUtility {
	public static class ConfigurationLogic {
		private const string providerName = "Isu";
		private static SystemIsuProvider provider;

		internal static void Init( Type systemLogicType ) {
			provider = StandardLibraryMethods.GetSystemLibraryProvider( systemLogicType, providerName ) as SystemIsuProvider;
		}

		internal static SystemIsuProvider SystemProvider {
			get {
				if( provider == null )
					throw StandardLibraryMethods.CreateProviderNotFoundException( providerName );
				return provider;
			}
		}

		private static ChannelFactory<RsisInterface.ServiceContracts.Isu> isuServiceFactory;
		private static ChannelFactory<RsisInterface.ServiceContracts.ProgramRunner> programRunnerServiceFactory;
		private static ChannelFactory<RsisInterface.ServiceContracts.ProgramRunnerUnstreamed> programRunnerUnstreamedServiceFactory;

		public static SystemList RsisSystemList { get; private set; }

		/// <summary>
		/// Loads the machine configuration file and RSIS system list.
		/// </summary>
		public static void Init() {
			if( !File.Exists( AppTools.MachineConfigXmlFilePath ) )
				throw new UserCorrectableException( "Missing machine configuration file. File should be located at " + AppTools.MachineConfigXmlFilePath + "." );

			isuServiceFactory = getNetTcpChannelFactory<RsisInterface.ServiceContracts.Isu>( "Isu.svc" );
			programRunnerServiceFactory = getNetTcpChannelFactory<RsisInterface.ServiceContracts.ProgramRunner>( "ProgramRunner.svc" );
			programRunnerUnstreamedServiceFactory = getHttpChannelFactory<RsisInterface.ServiceContracts.ProgramRunnerUnstreamed>( "ProgramRunnerUnstreamed.svc" );

			RefreshSystemList();
		}

		private static ChannelFactory<T> getHttpChannelFactory<T>( string serviceFileName ) {
			var binding = new BasicHttpBinding { Security = { Mode = BasicHttpSecurityMode.Transport } };
			binding.ReaderQuotas.MaxStringContentLength = int.MaxValue;
			binding.SendTimeout = TimeSpan.FromMinutes( 10 );

			// This prevents certificate validation problems on dev machines with self-signed certificates.
			// This can probably be done anywhere before we try to get the system list. 
			if( SystemProvider.RsisHttpBaseUrl.StartsWith( "https://localhost" ) )
				System.Net.ServicePointManager.ServerCertificateValidationCallback = ( ( sender, certificate, chain, sslPolicyErrors ) => true );

			return new ChannelFactory<T>( binding, NetTools.CombineUrls( SystemProvider.RsisHttpBaseUrl, "Service/" + serviceFileName ) );
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

			var factory = new ChannelFactory<T>( binding, NetTools.CombineUrls( SystemProvider.RsisTcpBaseUrl, "Service/" + serviceFileName ) );
			factory.Credentials.Windows.ClientCredential.UserName = SystemProvider.RsisTcpUserName;
			factory.Credentials.Windows.ClientCredential.Password = SystemProvider.RsisTcpPassword;
			return factory;
		}

		/// <summary>
		/// The action should be a noun, e.g. "logic package download".
		/// </summary>
		public static void ExecuteIsuServiceMethod( Action<RsisInterface.ServiceContracts.Isu> method, string action ) {
			executeWebMethod( method, isuServiceFactory, action );
		}

		/// <summary>
		/// The action should be a noun, e.g. "logic package download".
		/// </summary>
		public static void ExecuteProgramRunnerServiceMethod( Action<RsisInterface.ServiceContracts.ProgramRunner> method, string action ) {
			executeWebMethod( method, programRunnerServiceFactory, action );
		}

		/// <summary>
		/// The action should be a noun, e.g. "logic package download".
		/// </summary>
		public static void ExecuteProgramRunnerUnstreamedServiceMethod( Action<RsisInterface.ServiceContracts.ProgramRunnerUnstreamed> method, string action ) {
			executeWebMethod( method, programRunnerUnstreamedServiceFactory, action );
		}

		/// <summary>
		/// The action should be a noun, e.g. "logic package download".
		/// </summary>
		public static T ExecuteIsuServiceMethod<T>( Func<RsisInterface.ServiceContracts.Isu, T> method, string action ) {
			return executeWebMethodWithResult( method, isuServiceFactory, action );
		}

		/// <summary>
		/// The action should be a noun, e.g. "logic package download".
		/// </summary>
		public static T ExecuteProgramRunnerServiceMethod<T>( Func<RsisInterface.ServiceContracts.ProgramRunner, T> method, string action ) {
			return executeWebMethodWithResult( method, programRunnerServiceFactory, action );
		}

		/// <summary>
		/// The action should be a noun, e.g. "logic package download".
		/// </summary>
		public static T ExecuteProgramRunnerUnstreamedServiceMethod<T>( Func<RsisInterface.ServiceContracts.ProgramRunnerUnstreamed, T> method, string action ) {
			return executeWebMethodWithResult( method, programRunnerUnstreamedServiceFactory, action );
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

		private static ResultType executeWebMethodWithResult<ContractType, ResultType>( Func<ContractType, ResultType> method, ChannelFactory<ContractType> factory,
		                                                                                string action ) {
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
			if( innerException is EndpointNotFoundException ) {
				throw new UserCorrectableException(
					generalMessage + " The web service could not be reached - this could be due to a network error or a configuration error.", innerException );
			}
			if( innerException is FaultException ) {
				// We do not pass the fault exception as an inner exception because its message includes a big ugly stack trace.
				throw new UserCorrectableException( generalMessage +
				                                    " The web service was reachable but did not execute properly. This could be due to a database error on the server. Try again, as these types of errors usually do not persist." );
			}

			// EndpointNotFoundException and FaultException are derived from CommunicationException, so their conditions must be before this condition.
			if( innerException is CommunicationException )
				throw new UserCorrectableException( generalMessage + " There was a network problem.", innerException );

			throw new UserCorrectableException(
				generalMessage + " This may have been caused by a network problem. The exception type is " + innerException.GetType().Name + ".", innerException );
		}

		/// <summary>
		/// Gets a new system list from RSIS.
		/// </summary>
		public static void RefreshSystemList() {
			// When deserializing the system list below, do not perform schema validation since we don't want to be forced into redeploying Program Runner after every
			// schema change. We also don't have access to the schema on non-development machines.
			var cachedSystemListFilePath = StandardLibraryMethods.CombinePaths( AppTools.RedStaplerFolderPath, "RSIS System List.xml" );
			try {
				var serializedSystemList = ExecuteProgramRunnerUnstreamedServiceMethod( channel => channel.GetSystemList( AuthenticationKey ), "system list download" );
				RsisSystemList = XmlOps.DeserializeFromString<SystemList>( serializedSystemList, false );

				// Cache the system list so something is available in the future if the machine is offline.
				try {
					XmlOps.SerializeIntoFile( RsisSystemList, cachedSystemListFilePath );
				}
				catch( Exception e ) {
					const string generalMessage = "The RSIS system list cannot be cached on disk.";
					if( e is UnauthorizedAccessException )
						throw new UserCorrectableException( generalMessage + " If the program is running as a non built in administrator, you may need to disable UAC.", e );

					// An IOException probably means the file is locked. In this case we want to ignore the problem and move on.
					if( !( e is IOException ) )
						throw new UserCorrectableException( generalMessage, e );
				}
			}
			catch( UserCorrectableException e ) {
				if( e.InnerException == null || !( e.InnerException is EndpointNotFoundException ) )
					throw;

				// Use the cached version of the system list if it is available.
				if( File.Exists( cachedSystemListFilePath ) )
					RsisSystemList = XmlOps.DeserializeFromFile<SystemList>( cachedSystemListFilePath, false );
				else
					throw new UserCorrectableException( "RSIS cannot be reached to download the system list and a cached version is not available.", e );
			}
		}

		public static string AuthenticationKey {
			get {
				if( AppTools.MachineConfiguration.RsisAuthenticationKey == null )
					throw new UserCorrectableException( "Missing RSIS authentication key in machine configuration file." );
				return AppTools.MachineConfiguration.RsisAuthenticationKey;
			}
		}

		/// <summary>
		/// The path to installed installations on this machine.  For example, C:\Inetpub.
		/// </summary>
		public static string InstallationsFolderPath {
			get {
				if( AppTools.MachineConfiguration.InstallationsFolderPath != null )
					return AppTools.MachineConfiguration.InstallationsFolderPath;
				return @"C:\Inetpub";
			}
		}

		public static string RevisionControlFolderPath {
			get {
				if( AppTools.MachineConfiguration.VaultWorkingFolderPath != null )
					return AppTools.MachineConfiguration.VaultWorkingFolderPath;
				return @"C:\Red Stapler\Revision Control";
			}
		}

		/// <summary>
		/// This should only be used by the Installation Support Utility.
		/// </summary>
		public static string DownloadedDataPackagesFolderPath = StandardLibraryMethods.CombinePaths( AppTools.RedStaplerFolderPath, "Downloaded Data Packages" );

		/// <summary>
		/// This should only be used by the Web Site.
		/// </summary>
		public static string DataPackageRepositoryPath = StandardLibraryMethods.CombinePaths( AppTools.RedStaplerFolderPath, "RSIS Web Site Data Packages" );

		public static string GetBuildFilePath( int systemId ) {
			return StandardLibraryMethods.CombinePaths( DataPackageRepositoryPath
			                                            /*NOTE: Make this the generic web-site-accessible folder and change this and DataPackageRepositoryPath to be a subfolder of that.*/,
			                                            "Latest Builds",
			                                            systemId.ToString() );
		}

		/// <summary>
		/// This should only be used by the Installation Support Utility.
		/// </summary>
		public static string DownloadedTransactionLogsFolderPath = StandardLibraryMethods.CombinePaths( AppTools.RedStaplerFolderPath, "Downloaded Transaction Logs" );

		public static string TransactionLogBackupsPath = StandardLibraryMethods.CombinePaths( AppTools.RedStaplerFolderPath, "Transaction Log Backups" );

		public static string OracleSysPassword {
			get {
				if( AppTools.MachineConfiguration.OracleSysPassword == null )
					throw new UserCorrectableException( "Missing Oracle sys password in machine configuration file." );
				return AppTools.MachineConfiguration.OracleSysPassword;
			}
		}
	}
}