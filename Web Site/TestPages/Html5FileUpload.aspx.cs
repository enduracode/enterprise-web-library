using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.IO;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class Html5FileUpload: EwfPage {
		public static string HandleUploadedFiles( string fileUploaderIdentifier, string parameters, RsFile file ) {
			return "";
		}

		protected override void loadData() {
			// NOTE: The attributes need to have a value...
			ph.AddControlsReturnThis( new FancyFileManager( "first one!", "a" ) );
			var postBack = PostBack.CreateFull( firstTopValidationMethod: ( pbv, v ) => v.NoteErrorAndAddMessage( "error" ) );
			ph.AddControlsReturnThis( FormItemBlock.CreateFormItemTable( heading: "woo",
			                                                             formItems:
				                                                             new FormItem[] { FormItem.Create( "Files".GetLiteralControl(), new FancyFileManager( "second one!", "" ) ) } ) );

			ph.AddControlsReturnThis( new PostBackButton( postBack ) );
		}
	}
}