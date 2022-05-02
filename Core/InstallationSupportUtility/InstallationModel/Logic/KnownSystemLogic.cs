using System.Net.Http;
using System.Threading.Tasks;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.InstallationSupportUtility.SystemManagerInterface.Messages.SystemListMessage;
using EnterpriseWebLibrary.IO;

namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel {
	public class KnownSystemLogic {
		private readonly SoftwareSystem rsisSystem;

		public KnownSystemLogic( SoftwareSystem rsisSystem ) {
			this.rsisSystem = rsisSystem;
		}

		public SoftwareSystem RsisSystem => rsisSystem;

		public void DownloadAsposeLicenses( string configurationFolderPath ) {
			SystemManagerConnectionStatics.ExecuteWithSystemManagerClient(
				client => {
					Task.Run(
							async () => {
								using( var response = await client.GetAsync( "Pages/Public/AsposeLicensePackage.aspx", HttpCompletionOption.ResponseHeadersRead ) ) {
									response.EnsureSuccessStatusCode();
									using( var stream = await response.Content.ReadAsStreamAsync() )
										ZipOps.UnZipStreamAsFolder( stream, EwlStatics.CombinePaths( configurationFolderPath, InstallationConfiguration.AsposeLicenseFolderName ) );
								}
							} )
						.Wait();
				} );
		}
	}
}