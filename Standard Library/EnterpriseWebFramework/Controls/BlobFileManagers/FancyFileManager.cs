using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.Encryption;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A cool HTML5 upload control.
	/// </summary>
	public class FancyFileManager: WebControl, INamingContainer, ControlTreeDataLoader {
		private const string uniqueIdentifierAttribute = "uniqueIdentifier";
		private const string parmetersAttribute = "parameters";

		/// <summary>
		/// A string used to uniquely identify this specific uploader.
		/// </summary>
		public string UniqueUdentifier { get; private set; }

		/// <summary>
		/// The string that will be passed back to the file handling method.
		/// </summary>
		public string Parameters { get; private set; }

		/// <summary>
		/// Creates a new FancyFileManager.
		/// </summary>
		/// <param name="uniqueIdentifier">Use this to uniquely identify this specific uploader.</param>
		/// <param name="parameters">This string will be passed back to your file handling method.</param>
		public FancyFileManager( string uniqueIdentifier, string parameters ) {
			UniqueUdentifier = uniqueIdentifier;
			Parameters = parameters;
		}

		/// <summary>
		/// 
		/// </summary>
		public void LoadData( DBConnection cn ) {
			Attributes.Add( uniqueIdentifierAttribute, UniqueUdentifier );
			Attributes.Add( parmetersAttribute, Parameters );
			// This gives us some ability to be slightly more strongly typed, allowing us to change the actual attribute names here while not breaking script.
			EwfPage.Instance.ClientScript.RegisterClientScriptBlock( GetType(),
			                                                         uniqueIdentifierAttribute,
			                                                         @"uniqueIdentifierAttribute = '{0}';".FormatWith( uniqueIdentifierAttribute ),
			                                                         true );

			EwfPage.Instance.ClientScript.RegisterClientScriptBlock( GetType(), parmetersAttribute, @"parameters = '{0}';".FormatWith( parmetersAttribute ), true );
			// Provides a handle to the upload service for the script.
			EwfPage.Instance.ClientScript.RegisterClientScriptBlock( GetType(),
			                                                         "uploadServicePath",
			                                                         @"uploadServicePath = '{0}';".FormatWith( ResolveClientUrl( "~/Ewf/FileUploader/Upload.aspx" ) ),
			                                                         true );
			// Handle to the path of the progress image. jquery.progressbar needs to know where the images are to be used.
			// Just because this appears above the definition of jquery.progressbar doesn't mean it appears before
			// it in the resulting document, which causes the progressbar file to consider it undefined. To fix this, 
			// I wrapped the jquery.progressbar file in a $(document).ready() call so that it is not defined until the 
			// entire document loads, where imagesPath will be already defined, no matter its position.
			EwfPage.Instance.ClientScript.RegisterClientScriptBlock( GetType(),
			                                                         "imagesPath",
			                                                         @"imagesPath = '{0}';".FormatWith( ResolveClientUrl( "~/Ewf/FileUploader/" ) ),
			                                                         true );

			// NOTE: So this won't work if this control is used in a user control...
			var encryptedFullyQualifiedName = EncryptionOps.GetEncryptedString( EncryptionOps.GenerateInitVector(), EwfPage.Instance.GetType().BaseType.FullName );
			EwfPage.Instance.ClientScript.RegisterClientScriptBlock( GetType(), "pageHandle", @"pageHandle = '{0}';".FormatWith( encryptedFullyQualifiedName ), true );

			EwfPage.Instance.ClientScript.RegisterClientScriptInclude( GetType(), "ClientSide", this.GetClientUrl( "~/Ewf/FileUploader/ClientSide.js" ) );
			EwfPage.Instance.ClientScript.RegisterClientScriptInclude( GetType(),
			                                                           "jquery.progressbar",
			                                                           this.GetClientUrl( "~/Ewf/FileUploader/jquery.progressbar.min.js" ) );

			// Choose between dropping files onto the page or browse for them.
			var chooseUploadMethod = new EwfListControl { Type = EwfListControl.ListControlType.HorizontalRadioButton };
			chooseUploadMethod.FillWithTrueFalse( "Drag and drop files", "Browse for files" );

			var dragFilesHerePanel = new Panel { CssClass = "dropZone" }.AddControlsReturnThis( new Paragraph( "Drop files here" ) { CssClass = "dropFilesHereMessage" } );

			// Not using an ASP.NET control because I want full control without any magic.
			var browseForFiles = new HtmlGenericControl( "input" );
			browseForFiles.Attributes.Add( "type", "file" );
			browseForFiles.Attributes.Add( "multiple", "multiple" );
			browseForFiles.Attributes.Add( "onchange", @"inputChanged(this);" );

			chooseUploadMethod.AddDisplayLink( true.ToString(), true, dragFilesHerePanel );
			chooseUploadMethod.AddDisplayLink( false.ToString(), true, browseForFiles );

			var uploadPending = new Box( "Files to be uploaded",
			                             new Panel { CssClass = "queuedFilesContentArea" }.AddControlsReturnThis( new Paragraph( "No files are currently in the queue." ) ),
			                             new Heading { CssClass = "upload-count" } ) { CssClass = "queuedFiles" };

			Controls.Add( new Box( new Panel { CssClass = "ewfErrorMessageListBlock" },
			                       chooseUploadMethod,
			                       new Panel { CssClass = "dropWrapper" }.AddControlsReturnThis( dragFilesHerePanel ),
			                       browseForFiles,
			                       uploadPending,
			                       new CustomButton( @"uploadButtonClicked(this);" )
			                       	{ ActionControlStyle = new ButtonActionControlStyle( "Begin upload" ), CssClass = "beginUploadButton" } ) { CssClass = "upload-box" } );
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}