using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A control for managing a single file stored in a database.
	/// </summary>
	public sealed class BlobFileManager: WebControl, INamingContainer {
		private int? fileCollectionId;
		private readonly BlobFile file;
		private readonly EwfFileUpload uploadedFile;
		private string[] acceptableFileExtensions;

		/// <summary>
		/// Default is false. Set to true if you do not want the manager to show "No existing file" when there is no file in the database.
		/// </summary>
		public bool HideNoExistingFileMessage { get; set; }

		/// <summary>
		/// Sets the method used to get thumbnail URLs for files with the image content type. The method takes a file ID and returns a resource info object.
		/// </summary>
		public Func<decimal, ResourceInfo> ThumbnailResourceInfoCreator { private get; set; }

		/// <summary>
		/// Prevents the user from uploading a file of a type other than those provided. File type constants found in EnterpriseWebLibrary.FileExtensions.
		/// </summary>
		public void SetFileTypeFilter( params string[] acceptableFileExtensions ) {
			this.acceptableFileExtensions = acceptableFileExtensions;
		}

		public BlobFileManager( int? fileCollectionId ) {
			this.fileCollectionId = fileCollectionId;

			if( fileCollectionId != null )
				file = BlobFileOps.GetFirstFileFromCollection( fileCollectionId.Value );

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
										new BlobFileResponse( BlobFileOps.GetFirstFileFromCollection( fileCollectionId.Value ).FileId, () => true ),
										false ) ) );
						} ) );
				controlStack.AddControls( download );
			}
			else if( !HideNoExistingFileMessage )
				controlStack.AddControls( new Label { Text = Translation.NoExistingFile } );

			uploadedFile = new EwfFileUpload();
			if( file != null ) {
				uploadedFile.SetInitialDisplay( false );
				var replaceExistingFileLink = new ToggleButton(
					uploadedFile.ToCollection(),
					new TextActionControlStyle( Translation.ClickHereToReplaceExistingFile ),
					false,
					( postBackValue, validator ) => {} ) { AlternateText = "" };
				controlStack.AddControls( replaceExistingFileLink );
			}

			controlStack.AddControls( uploadedFile );

			this.AddControlsReturnThis( BlobFileOps.GetThumbnailControl( file, ThumbnailResourceInfoCreator ) );
			Controls.Add( controlStack );
		}

		/// <summary>
		/// Call this during ValidateFormValues.
		/// </summary>
		public void ValidateFormValues( Validator validator, string subject, bool requireUploadIfNoFile ) {
			validateFormValues( validator, subject, requireUploadIfNoFile, delegate {}, false );
		}

		/// <summary>
		/// Performs validate form values, but also forces the file to be an image.
		/// </summary>
		public void ValidateAsImage( Validator validator, string subject, bool requireUploadIfNoFile ) {
			ValidateAsImage( validator, subject, requireUploadIfNoFile, delegate {} );
		}

		/// <summary>
		/// Performs validate form values, but also forces the file to be an image of the specified dimensions.
		/// </summary>
		public void ValidateAsImage( Validator validator, string subject, bool requireUploadIfNoFile, int width, int height ) {
			ValidateAsImage( validator, subject, requireUploadIfNoFile, BlobFileOps.GetWidthAndHeightImageValidationMethod( subject, width, height ) );
		}

		/// <summary>
		/// Performs validate form values, but also forces the file to be an image and lets you impose additional restrictions (width, etc.)
		/// using the validateImage method.
		/// </summary>
		public void ValidateAsImage( Validator validator, string subject, bool requireUploadIfNoFile, Action<Validator, System.Drawing.Image> validateImage ) {
			validateFormValues( validator, subject, requireUploadIfNoFile, validateImage, true );
		}

		private void validateFormValues(
			Validator validator, string subject, bool requireUploadIfNoFile, Action<Validator, System.Drawing.Image> validateImage, bool mustBeImage ) {
			BlobFileOps.ValidateUploadedFile( validator, uploadedFile, acceptableFileExtensions, validateImage, mustBeImage );
			if( requireUploadIfNoFile && file == null && !uploadedFile.ValueChangedOnPostBack( AppRequestState.Instance.EwfPageRequestState.PostBackValues ) )
				validator.NoteErrorAndAddMessage( Translation.PleaseUploadAFile + " '" + subject + "'." );
		}

		/// <summary>
		/// Call this during ModifyData.  This returns the file collection ID of the existing or just-inserted file collection.
		/// This will return an ID even if there are 0 files in the collection (in other words, no file). No file should always be
		/// represented by a non-null file collection ID which happens to have 0 files in it.  Null file collection IDs are not supported.
		/// </summary>
		public int ModifyData() {
			if( fileCollectionId == null )
				fileCollectionId = BlobFileOps.SystemProvider.InsertFileCollection();

			var rsFile = uploadedFile.GetPostBackValue( AppRequestState.Instance.EwfPageRequestState.PostBackValues );
			if( rsFile != null ) {
				BlobFileOps.SystemProvider.DeleteFilesLinkedToFileCollection( fileCollectionId.Value );
				BlobFileOps.SystemProvider.InsertFile( fileCollectionId.Value, rsFile.FileName, rsFile.Contents, BlobFileOps.GetContentTypeForPostedFile( rsFile ) );
			}
			return fileCollectionId.Value;
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey => HtmlTextWriterTag.Div;
	}
}