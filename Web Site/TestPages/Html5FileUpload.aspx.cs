using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.IO;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class Html5FileUpload: EwfPage {
		public partial class Info {
			protected override void init( DBConnection cn ) {}
		}

		public static string HandleUploadedFiles( string fileUploaderIdentifier, string parameters, RsFile file ) {
			return "";
		}

		protected override void LoadData( DBConnection cn ) {
			// NOTE: The attributes need to have a value...
			ph.AddControlsReturnThis( new FancyFileManager( "first one!", "a" ) );
			var dataModification = new DataModification( firstTopValidationMethod: ( pbv, v ) => v.NoteErrorAndAddMessage( "error" ) );
			ph.AddControlsReturnThis( FormItemBlock.CreateFormItemTable( heading: "woo",
			                                                             formItems:
				                                                             new FormItem[] { FormItem.Create( "Files", new FancyFileManager( "second one!", "" ) ) } ) );

			ph.AddControlsReturnThis( new PostBackButton( dataModification, () => { } ) );
		}
	}
}