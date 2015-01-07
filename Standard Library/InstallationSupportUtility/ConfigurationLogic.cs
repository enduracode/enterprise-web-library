using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using RedStapler.StandardLibrary.Configuration;

namespace RedStapler.StandardLibrary.InstallationSupportUtility {
	public static class ConfigurationLogic {
		private const string providerName = "Isu";

		/// <summary>
		/// This should only be used by the Installation Support Utility.
		/// </summary>
		public static string DownloadedDataPackagesFolderPath = StandardLibraryMethods.CombinePaths(
			ConfigurationStatics.RedStaplerFolderPath,
			"Downloaded Data Packages" );

		/// <summary>
		/// This should only be used by the Web Site.
		/// </summary>
		public static string DataPackageRepositoryPath = StandardLibraryMethods.CombinePaths(
			ConfigurationStatics.RedStaplerFolderPath,
			"RSIS Web Site Data Packages" );

		/// <summary>
		/// This should only be used by the Installation Support Utility.
		/// </summary>
		public static string DownloadedTransactionLogsFolderPath = StandardLibraryMethods.CombinePaths(
			ConfigurationStatics.RedStaplerFolderPath,
			"Downloaded Transaction Logs" );

		public static string TransactionLogBackupsPath = StandardLibraryMethods.CombinePaths( ConfigurationStatics.RedStaplerFolderPath, "Transaction Log Backups" );

		private static SystemIsuProvider provider;

		private static ChannelFactory<RsisInterface.ServiceContracts.Isu> isuServiceFactory;
		private static ChannelFactory<RsisInterface.ServiceContracts.ProgramRunner> programRunnerServiceFactory;
		private static ChannelFactory<RsisInterface.ServiceContracts.ProgramRunnerUnstreamed> programRunnerUnstreamedServiceFactory;

		internal static void Init1() {
			provider = ConfigurationStatics.GetSystemLibraryProvider( providerName ) as SystemIsuProvider;

			if( NDependIsPresent ) {
				AppDomain.CurrentDomain.AssemblyResolve += ( sender, args ) => {
					var assemblyName = new AssemblyName( args.Name ).Name;
					if( !new[] { "NDepend.API", "NDepend.Core" }.Contains( assemblyName ) )
						return null;
					return
						Assembly.LoadFrom(
							StandardLibraryMethods.CombinePaths(
								Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ),
								provider.NDependFolderPathInUserProfileFolder,
								"Lib",
								assemblyName + ".dll" ) );
				};
			}
		}

		/// <summary>
		/// Standard Library and Development Utility use only.
		/// </summary>
		public static bool SystemProviderExists { get { return provider != null; } }

		/// <summary>
		/// Standard Library and Development Utility use only.
		/// </summary>
		public static SystemIsuProvider SystemProvider {
			get {
				if( provider == null )
					throw ConfigurationStatics.CreateProviderNotFoundException( providerName );
				return provider;
			}
		}

		/// <summary>
		/// Standard Library and Development Utility use only.
		/// </summary>
		public static bool NDependIsPresent {
			get {
				return SystemProviderExists && SystemProvider.NDependFolderPathInUserProfileFolder.Any() &&
				       Directory.Exists(
					       StandardLibraryMethods.CombinePaths(
						       Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ),
						       provider.NDependFolderPathInUserProfileFolder ) );
			}
		}

		public static void Init2() {
			isuServiceFactory = getNetTcpChannelFactory<RsisInterface.ServiceContracts.Isu>( "Isu.svc" );
			programRunnerServiceFactory = getNetTcpChannelFactory<RsisInterface.ServiceContracts.ProgramRunner>( "ProgramRunner.svc" );
			programRunnerUnstreamedServiceFactory = getHttpChannelFactory<RsisInterface.ServiceContracts.ProgramRunnerUnstreamed>( "ProgramRunnerUnstreamed.svc" );
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
			if( innerException is EndpointNotFoundException ) {
				throw new UserCorrectableException(
					generalMessage + " The web service could not be reached - this could be due to a network error or a configuration error.",
					innerException );
			}
			if( innerException is FaultException ) {
				// We do not pass the fault exception as an inner exception because its message includes a big ugly stack trace.
				throw new UserCorrectableException(
					generalMessage +
					" The web service was reachable but did not execute properly. This could be due to a database error on the server. Try again, as these types of errors usually do not persist." );
			}

			// EndpointNotFoundException and FaultException are derived from CommunicationException, so their conditions must be before this condition.
			if( innerException is CommunicationException )
				throw new UserCorrectableException( generalMessage + " There was a network problem.", innerException );

			throw new UserCorrectableException(
				generalMessage + " This may have been caused by a network problem. The exception type is " + innerException.GetType().Name + ".",
				innerException );
		}

		public static string AuthenticationKey {
			get {
				if( ConfigurationStatics.MachineConfiguration == null || ConfigurationStatics.MachineConfiguration.RsisAuthenticationKey == null )
					throw new UserCorrectableException( "Missing RSIS authentication key in machine configuration file." );
				return ConfigurationStatics.MachineConfiguration.RsisAuthenticationKey;
			}
		}

		/// <summary>
		/// The path to installed installations on this machine.
		/// </summary>
		public static string InstallationsFolderPath {
			get {
				if( ConfigurationStatics.MachineConfiguration != null && ConfigurationStatics.MachineConfiguration.InstallationsFolderPath != null )
					return ConfigurationStatics.MachineConfiguration.InstallationsFolderPath;
				return StandardLibraryMethods.CombinePaths( ConfigurationStatics.RedStaplerFolderPath, "Installations" );
			}
		}

		public static string RevisionControlFolderPath {
			get {
				if( ConfigurationStatics.MachineConfiguration != null && ConfigurationStatics.MachineConfiguration.VaultWorkingFolderPath != null )
					return ConfigurationStatics.MachineConfiguration.VaultWorkingFolderPath;
				return StandardLibraryMethods.CombinePaths( ConfigurationStatics.RedStaplerFolderPath, "Revision Control" );
			}
		}

		public static string GetBuildFilePath( int systemId ) {
			return StandardLibraryMethods.CombinePaths(
				DataPackageRepositoryPath
				/*NOTE: Make this the generic web-site-accessible folder and change this and DataPackageRepositoryPath to be a subfolder of that.*/,
				"Latest Builds",
				systemId.ToString() );
		}

		public static string OracleSysPassword {
			get {
				if( ConfigurationStatics.MachineConfiguration == null || ConfigurationStatics.MachineConfiguration.OracleSysPassword == null )
					throw new UserCorrectableException( "Missing Oracle sys password in machine configuration file." );
				return ConfigurationStatics.MachineConfiguration.OracleSysPassword;
			}
		}
	}
}