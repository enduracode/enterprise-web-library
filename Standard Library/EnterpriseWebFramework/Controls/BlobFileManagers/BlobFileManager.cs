using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.Validation;
using RedStapler.StandardLibrary.WebFileSending;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A control for managing a single file stored in a database.
	/// </summary>
	public class BlobFileManager: WebControl, INamingContainer, ControlTreeDataLoader {
		private int? fileCollectionId;
		private BlobFile file;
		private EwfFileUpload uploadedFile;
		private string[] acceptableFileExtensions;

		/// <summary>
		/// Default is false. Set to true if you do not want the manager to show "No existing file" when there is no file in the database.
		/// </summary>
		public bool HideNoExistingFileMessage { get; set; }

		/// <summary>
		/// Sets the method used to get thumbnail URLs for files with the image content type. The method takes a file ID and returns a page info object.
		/// </summary>
		public Func<decimal, PageInfo> ThumbnailPageInfoCreator { private get; set; }

		/// <summary>
		/// Call this during LoadData.  This does not need to be called if there is no existing file collection.
		/// </summary>
		public void LoadData( DBConnection cn, int fileCollectionId ) {
			this.fileCollectionId = fileCollectionId;
		}

		/// <summary>
		/// Prevents the user from uploading a file of a type other than those provided. File type constants found in RedStapler.StandardLibrary.FileExtensions.
		/// </summary>
		public void SetFileTypeFilter( params string[] acceptableFileExtensions ) {
			this.acceptableFileExtensions = acceptableFileExtensions;
		}

		// NOTE: EVERYTHING should be done here. We shouldn't have LoadData. We should audit everyone using this control and see if we can improve things.
		// NOTE: This should also be full of delegates that run when events (such as deleting a file) are occurring.
		// NOTE: There should be a way to tell if a file was uploaded.
		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			if( fileCollectionId != null )
				file = BlobFileOps.GetFirstFileFromCollection( cn, fileCollectionId.Value );

			var controlStack = ControlStack.Create( true );
			if( file != null ) {
				var download = new PostBackButton( new DataModification(),
				                                   () =>
				                                   EwfPage.Instance.EhModifyDataAndSendFile( new FileCreator( // Refresh the file here in case a new one was uploaded on the same post back
				                                                                             	cn1 =>
				                                                                             	BlobFileOps.GetFirstFileFromCollection( cn1, fileCollectionId.Value ).FileId ) ),
				                                   new TextActionControlStyle( Translation.DownloadExisting + " (" + file.FileName + ")" ),
				                                   false );
				controlStack.AddControls( download );
			}
			else if( !HideNoExistingFileMessage )
				controlStack.AddControls( new Label { Text = Translation.NoExistingFile } );

			uploadedFile = new EwfFileUpload();
			if( file != null ) {
				uploadedFile.SetInitialDisplay( false );
				var replaceExistingFileLink = new ToggleButton( uploadedFile.ToSingleElementArray(),
				                                                new TextActionControlStyle( Translation.ClickHereToReplaceExistingFile ) ) { AlternateText = "" };
				controlStack.AddControls( replaceExistingFileLink );
			}

			controlStack.AddControls( uploadedFile );

			var thumbnailControl = BlobFileOps.GetThumbnailControl( file, ThumbnailPageInfoCreator );
			if( thumbnailControl != null )
				Controls.Add( thumbnailControl );
			Controls.Add( controlStack );
		}

		/// <summary>
		/// Call this during ValidateFormValues.
		/// </summary>
		public void ValidateFormValues( Validator validator, string subject, bool requireUploadIfNoFile ) {
			validateFormValues( validator, subject, requireUploadIfNoFile, delegate { }, false );
		}

		/// <summary>
		/// Performs validate form values, but also forces the file to be an image.
		/// </summary>
		public void ValidateAsImage( Validator validator, string subject, bool requireUploadIfNoFile ) {
			ValidateAsImage( validator, subject, requireUploadIfNoFile, delegate { } );
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

		private void validateFormValues( Validator validator, string subject, bool requireUploadIfNoFile, Action<Validator, System.Drawing.Image> validateImage,
		                                 bool mustBeImage ) {
			BlobFileOps.ValidateUploadedFile( validator, uploadedFile, acceptableFileExtensions, validateImage, mustBeImage );
			if( requireUploadIfNoFile && file == null && !uploadedFile.ValueChangedOnPostBack( AppRequestState.Instance.EwfPageRequestState.PostBackValues ) )
				validator.NoteErrorAndAddMessage( Translation.PleaseUploadAFile + " '" + subject + "'." );
		}

		/// <summary>
		/// Call this during ModifyData.  This returns the file collection ID of the existing or just-inserted file collection.
		/// This will return an ID even if there are 0 files in the collection (in other words, no file). No file should always be
		/// represented by a non-null file collection ID which happens to have 0 files in it.  Null file collection IDs are not supported.
		/// </summary>
		public int ModifyData( DBConnection cn ) {
			if( fileCollectionId == null )
				fileCollectionId = BlobFileOps.SystemProvider.InsertFileCollection( cn );

			var rsFile = uploadedFile.GetPostBackValue( AppRequestState.Instance.EwfPageRequestState.PostBackValues );
			if( rsFile != null ) {
				BlobFileOps.SystemProvider.DeleteFilesLinkedToFileCollection( cn, fileCollectionId.Value );
				BlobFileOps.SystemProvider.InsertFile( cn, fileCollectionId.Value, rsFile.FileName, rsFile.Contents, BlobFileOps.GetContentTypeForPostedFile( rsFile ) );
			}
			return fileCollectionId.Value;
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}