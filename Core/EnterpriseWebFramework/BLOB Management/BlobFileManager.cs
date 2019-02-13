using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.DataAccess.BlobStorage;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.InputValidation;
using EnterpriseWebLibrary.IO;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A control for managing a single file stored in a database.
	/// </summary>
	public sealed class BlobFileManager: WebControl, INamingContainer {
		private int? fileCollectionId;
		private RsFile uploadedFile;

		/// <summary>
		/// Default is false. Set to true if you do not want the manager to show "No existing file" when there is no file in the database.
		/// </summary>
		public bool HideNoExistingFileMessage { get; set; }

		/// <summary>
		/// Sets the method used to get thumbnail URLs for files with the image content type. The method takes a file ID and returns a resource info object.
		/// </summary>
		public Func<decimal, ResourceInfo> ThumbnailResourceInfoCreator { private get; set; }

		public BlobFileManager( int? fileCollectionId, bool requireUploadIfNoFile, Action<RsFile, Validator> validationMethod ) {
			this.fileCollectionId = fileCollectionId;

			var file = fileCollectionId != null ? BlobStorageStatics.GetFirstFileFromCollection( fileCollectionId.Value ) : null;

			var controlStack = ControlStack.Create( true );
			if( file != null ) {
				var download = new PostBackButton(
					new TextActionControlStyle( Translation.DownloadExisting + " (" + file.FileName + ")" ),
					usesSubmitBehavior: false,
					postBack: PostBack.CreateFull(
						id: PostBack.GetCompositeId( "ewfFile", file.FileId.ToString() ),
						actionGetter: () => {
							// Refresh the file here in case a new one was uploaded on the same post-back.
							return new PostBackAction(
								new PageReloadBehavior(
									secondaryResponse: new SecondaryResponse(
										new BlobFileResponse( BlobStorageStatics.GetFirstFileFromCollection( fileCollectionId.Value ).FileId, () => true ),
										false ) ) );
						} ) );
				controlStack.AddControls( download );
			}
			else if( !HideNoExistingFileMessage )
				controlStack.AddControls( new Label { Text = Translation.NoExistingFile } );

			var fileUploadDisplayedPmv = new PageModificationValue<string>();
			controlStack.AddControls(
				new FileUpload(
						displaySetup: fileUploadDisplayedPmv.ToCondition( bool.TrueString.ToCollection() ).ToDisplaySetup(),
						validationMethod: ( postBackValue, validator ) => {
							if( requireUploadIfNoFile && file == null && postBackValue == null ) {
								validator.NoteErrorAndAddMessage( Translation.PleaseUploadAFile );
								return;
							}

							uploadedFile = postBackValue;
							validationMethod( postBackValue, validator );
						} ).ToFormItem()
					.ToControl() );
			var fileUploadDisplayedHiddenFieldId = new HiddenFieldId();
			new EwfHiddenField( ( file == null ).ToString(), id: fileUploadDisplayedHiddenFieldId, pageModificationValue: fileUploadDisplayedPmv ).PageComponent
				.ToCollection()
				.AddEtherealControls( controlStack );
			if( file != null )
				controlStack.AddControls(
					new PlaceHolder().AddControlsReturnThis(
						new EwfButton(
								new StandardButtonStyle( Translation.ClickHereToReplaceExistingFile, buttonSize: ButtonSize.ShrinkWrap ),
								displaySetup: fileUploadDisplayedPmv.ToCondition( bool.FalseString.ToCollection() ).ToDisplaySetup(),
								behavior: new ChangeValueBehavior( fileUploadDisplayedHiddenFieldId, bool.TrueString ) ).ToCollection()
							.GetControls() ) );

			this.AddControlsReturnThis( BlobManagementStatics.GetThumbnailControl( file, ThumbnailResourceInfoCreator ) );
			Controls.Add( controlStack );
		}

		/// <summary>
		/// Call this during ModifyData.  This returns the file collection ID of the existing or just-inserted file collection.
		/// This will return an ID even if there are 0 files in the collection (in other words, no file). No file should always be
		/// represented by a non-null file collection ID which happens to have 0 files in it.  Null file collection IDs are not supported.
		/// </summary>
		public int ModifyData() {
			if( fileCollectionId == null )
				fileCollectionId = BlobStorageStatics.SystemProvider.InsertFileCollection();

			if( uploadedFile != null ) {
				BlobStorageStatics.SystemProvider.DeleteFilesLinkedToFileCollection( fileCollectionId.Value );
				BlobStorageStatics.SystemProvider.InsertFile(
					fileCollectionId.Value,
					uploadedFile.FileName,
					uploadedFile.Contents,
					BlobStorageStatics.GetContentTypeForPostedFile( uploadedFile ) );
			}
			return fileCollectionId.Value;
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey => HtmlTextWriterTag.Div;
	}
}