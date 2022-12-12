using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Email;
using EnterpriseWebLibrary.InstallationSupportUtility.SystemManagerInterface.Messages.SystemListMessage;
using Tewl.IO;

namespace EnterpriseWebLibrary.InstallationSupportUtility {
	public static class SystemManagerConnectionStatics {
		public const string ServersUrlSegment = "servers";
		public const string ServerConfigurationUrlSegment = "configuration";
		public const string BuildsUrlSegment = "builds";
		public const string ServerSideLogicPackageUrlSegment = "server-side-logic-package";
		public const string ClientSideAppPackageUrlSegment = "client-side-app-package";
		public const string InstallationConfigurationPackagesUrlSegment = "installation-configuration-packages";
		public const string InstallationsUrlSegment = "installations";
		public const string DataPackageUrlSegment = "data-package";

		public static SystemList SystemList { get; private set; }

		public static void Init( bool? refreshProgramRunnerDataOnly = false ) {
			if( refreshProgramRunnerDataOnly.HasValue )
				RefreshLocalData( forProgramRunner: refreshProgramRunnerDataOnly.Value );
		}

		/// <summary>
		/// Gets new System Manager local data.
		/// </summary>
		public static void RefreshLocalData( bool forProgramRunner = false ) {
			// Do not perform system-list schema validation since we don’t want to be forced into redeploying Program Runner after every schema change. We also don’t
			// have access to the schema on non-development machines.
			byte[] emailInterface = null;
			var systemListFilePath = EwlStatics.CombinePaths( ConfigurationStatics.EwlFolderPath, "System List" + FileExtensions.Xml );
			var cacheUsed = false;
			try {
				ExecuteWithSystemManagerClient(
					client => {
						Task.Run(
								async () => {
									if( !forProgramRunner )
										using( var response = await client.GetAsync( "email-interface", HttpCompletionOption.ResponseHeadersRead ) ) {
											response.EnsureSuccessStatusCode();
											emailInterface = await response.Content.ReadAsByteArrayAsync();
										}

									using( var response = await client.GetAsync( "system-list", HttpCompletionOption.ResponseHeadersRead ) ) {
										response.EnsureSuccessStatusCode();
										await using( var stream = await response.Content.ReadAsStreamAsync() )
											SystemList = XmlOps.DeserializeFromStream<SystemList>( stream, false );
									}
								} )
							.Wait();
					} );
			}
			catch( Exception e ) {
				// Use the cached version of the data if available.
				if( File.Exists( systemListFilePath ) ) {
					if( !forProgramRunner )
						emailInterface = File.ReadAllBytes( EmailStatics.SystemManagerInterfaceFilePath );
					SystemList = XmlOps.DeserializeFromFile<SystemList>( systemListFilePath, false );
					cacheUsed = true;
				}
				else
					throw new UserCorrectableException( "Failed to download System Manager data and a cached version is not available.", e );
			}

			StatusStatics.SetStatus(
				cacheUsed
					? "Failed to download System Manager data; loaded a cached version from \"{0}\".".FormatWith( ConfigurationStatics.EwlFolderPath )
					: "Downloaded System Manager data." );

			// Cache the data so something is available in the future if the System Manager is unavailable.
			try {
				if( !forProgramRunner )
					File.WriteAllBytes( EmailStatics.SystemManagerInterfaceFilePath, emailInterface );
				XmlOps.SerializeIntoFile( SystemList, systemListFilePath );
			}
			catch( Exception e ) {
				const string generalMessage = "Failed to cache System Manager data on disk.";
				if( e is UnauthorizedAccessException )
					throw new UserCorrectableException( generalMessage + " If the program is running as a non built in administrator, you may need to disable UAC.", e );

				// An IOException probably means the file is locked. In this case we want to ignore the problem and move on.
				if( !( e is IOException ) )
					throw new UserCorrectableException( generalMessage, e );
			}
		}

		public static void ExecuteWithSystemManagerClient( Action<HttpClient> method, bool useLongTimeouts = false ) {
			using var client = new HttpClient();

			client.Timeout = useLongTimeouts ? new TimeSpan( 0, 2, 0 ) : new TimeSpan( 0, 0, 10 );
			client.BaseAddress = new Uri( Configuration.HttpBaseUrl + "/" );
			client.DefaultRequestHeaders.TryAddWithoutValidation( "Authorization", Configuration.AccessToken );

			method( client );
		}

		/// <summary>
		/// The action should be a noun, e.g. “logic package download”.
		/// </summary>
		public static void ExecuteActionWithSystemManagerClient( string action, Action<HttpClient> method, bool supportLargePayload = false ) {
			using var client = new HttpClient();

			client.Timeout = supportLargePayload ? Timeout.InfiniteTimeSpan : new TimeSpan( 0, 2, 0 );
			client.BaseAddress = new Uri( Configuration.HttpBaseUrl + "/" );
			client.DefaultRequestHeaders.TryAddWithoutValidation( "Authorization", Configuration.AccessToken );

			StatusStatics.SetStatus( "Performing {0}.".FormatWith( action ) );
			try {
				method( client );
			}
			catch( Exception e ) {
				throw createWebServiceException( action, e );
			}
			StatusStatics.SetStatus( "Performed {0}.".FormatWith( action ) );
		}

		private static Exception createWebServiceException( string action, Exception innerException ) {
			var generalMessage = "Failed during " + action + ".";
			//if( innerException is EndpointNotFoundException )
			//	throw new UserCorrectableException(
			//		generalMessage + " The web service could not be reached - this could be due to a network error or a configuration error.",
			//		innerException );
			//if( innerException is FaultException )
			//	// We do not pass the fault exception as an inner exception because its message includes a big ugly stack trace.
			//	throw new UserCorrectableException(
			//		generalMessage +
			//		" The web service was reachable but did not execute properly. This could be due to a database error on the server. Try again, as these types of errors usually do not persist." );

			//// EndpointNotFoundException and FaultException are derived from CommunicationException, so their conditions must be before this condition.
			//if( innerException is CommunicationException )
			//	throw new UserCorrectableException( generalMessage + " There was a network problem.", innerException );

			throw new UserCorrectableException(
				generalMessage + " This may have been caused by a network problem. The exception type is " + innerException.GetType().Name + ".",
				innerException );
		}

		public static Configuration.Machine.MachineConfigurationSystemManager Configuration =>
			ConfigurationStatics.MachineConfiguration == null || ConfigurationStatics.MachineConfiguration.SystemManager == null
				? throw new UserCorrectableException( "Missing System Manager element in machine configuration file." )
				: ConfigurationStatics.MachineConfiguration.SystemManager;
	}
}