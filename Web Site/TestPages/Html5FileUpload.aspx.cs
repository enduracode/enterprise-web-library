using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.IO;

namespace RedStapler.TestWebSite.TestPages {
	public partial class Html5FileUpload: EwfPage {
		public partial class Info {
			protected override void init( DBConnection cn ) {}
		}

		protected override void LoadData( DBConnection cn ) {
			// NOTE: The attributes need to have a value...
			ph.AddControlsReturnThis( new FancyFileManager( "first one!", "a" ) );
			var dataModification = new DataModification( firstValidationMethod: v => v.NoteErrorAndAddMessage( "error" ) );
			ph.AddControlsReturnThis( FormItemBlock.CreateFormItemTable( "woo",
			                                                             formItems:
			                                                             	new FormItem[] { FormItem.Create( "Files", new FancyFileManager( "second one!", "" ) ) } ) );

			ph.AddControlsReturnThis( new PostBackButton( dataModification, () => { } ) );
		}

		public static string HandleUploadedFiles( string fileUploaderIdentifier, string parameters, RsFile file ) {
			return "";
		}
	}
}