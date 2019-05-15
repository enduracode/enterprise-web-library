using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.InstallationSupportUtility.SystemManagerInterface.Messages.SystemListMessage;
using EnterpriseWebLibrary.IO;
using Humanizer;

namespace EnterpriseWebLibrary.InstallationSupportUtility {
	public static class SystemListStatics {
		public static SystemList RsisSystemList { get; private set; }

		/// <summary>
		/// Gets a new system list.
		/// </summary>
		public static void RefreshSystemList() {
			// Do not perform schema validation since we don't want to be forced into redeploying Program Runner after every schema change. We also don't have access
			// to the schema on non-development machines.
			var cacheFilePath = EwlStatics.CombinePaths( ConfigurationStatics.EwlFolderPath, "System List.xml" );
			var cacheUsed = false;
			try {
				ConfigurationLogic.ExecuteWithSystemManagerClient(
					client => {
						Task.Run(
								async () => {
									using( var response = await client.GetAsync( "system-list", HttpCompletionOption.ResponseHeadersRead ) ) {
										response.EnsureSuccessStatusCode();
										using( var stream = await response.Content.ReadAsStreamAsync() )
											RsisSystemList = XmlOps.DeserializeFromStream<SystemList>( stream, false );
									}
								} )
							.Wait();
					} );
			}
			catch( Exception e ) {
				// Use the cached version of the system list if it is available.
				if( File.Exists( cacheFilePath ) ) {
					RsisSystemList = XmlOps.DeserializeFromFile<SystemList>( cacheFilePath, false );
					cacheUsed = true;
				}
				else
					throw new UserCorrectableException( "Failed to download the system list and a cached version is not available.", e );
			}

			StatusStatics.SetStatus(
				cacheUsed ? "Failed to download the system list; loaded a cached version from \"{0}\".".FormatWith( cacheFilePath ) : "Downloaded the system list." );

			// Cache the system list so something is available in the future if the machine is offline.
			try {
				XmlOps.SerializeIntoFile( RsisSystemList, cacheFilePath );
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
	}
}