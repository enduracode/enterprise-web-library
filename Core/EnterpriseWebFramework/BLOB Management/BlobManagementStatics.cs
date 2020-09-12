using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnterpriseWebLibrary.DataAccess.BlobStorage;
using EnterpriseWebLibrary.InputValidation;
using EnterpriseWebLibrary.IO;
using Tewl;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public static class BlobManagementStatics {
		/// <summary>
		/// If file is null, this will be a no-op.
		/// Pass null for acceptableFileExtensions if there is no restriction on file extension.
		/// PerformAdditionalImageValidation cannot be null but may be an empty delegate.
		/// </summary>
		/// <param name="validator"></param>
		/// <param name="file"></param>
		/// <param name="acceptableFileExtensions">Prevents the user from uploading a file of a type other than those provided. File type constants found in
		/// EnterpriseWebLibrary.FileExtensions. Do not use this to force the file to be a specific type of file, such as an image (which consists of several file
		/// extensions). Instead, use mustBeRenderableImage.</param>
		/// <param name="mustBeRenderableImage">Pass true to only accept images (of any renderable type - jpgs, pngs, but not nefs).</param>
		public static void ValidateUploadedFile( Validator validator, RsFile file, string[] acceptableFileExtensions, bool mustBeRenderableImage ) {
			if( file == null )
				return;

			// Perform generic file validation.
			if( acceptableFileExtensions != null && !FileExtensions.MatchesAGivenExtension( file.FileName, acceptableFileExtensions ) ) {
				validator.NoteErrorAndAddMessage( Translation.UnacceptableFileExtension + " " + acceptableFileExtensions.GetCommaDelimitedStringFromCollection() );
				// Don't bother trying to see if it's an image and parse the image. The file extension message be more detailed than the messages those errors produce.
				return;
			}

			// Perform image-specific validation if necessary.
			if( mustBeRenderableImage )
				// Make sure it is an image according to its content type.
				if( !ContentTypes.IsImageType( BlobStorageStatics.GetContentTypeForPostedFile( file ) ) )
					validator.NoteErrorAndAddMessage( "Please upload a valid image file." );
				else
					// Make sure it is an image type that we understand. Also perform optional custom validation.
					try {
						using( var stream = new MemoryStream( file.Contents ) )
							System.Drawing.Image.FromStream( stream );
					}
					catch( ArgumentException ) {
						// If we end up in this catch block, it means that System.Drawing.Image does not understand our image. Since we already know that our content type
						// is image at this point, this usually means that the file is some sort of unsupported image format, like NEF.
						validator.NoteErrorAndAddMessage( "The uploaded image file is in an unsupported format." );
					}
		}

		/// <summary>
		/// Returns null if the file is null, the file is not an image, or there is no thumbnail resource getter.
		/// </summary>
		internal static IReadOnlyCollection<PhrasingComponent> GetThumbnailControl( BlobFile file, Func<int, ResourceInfo> thumbnailResourceGetter ) {
			// NOTE: We'd like to check here whether the file is a renderable image or not. But we can't because we don't have the file contents.
			// So, we'll have to make sure that all ThumbnailPageInfoCreators provide a page that knows how to handle NEF files (ideally we'd want
			// it to behave as if there was no thumbnail at all if there is an unrenderable image file).
			// The only alternative to this that I can think of is creating a new file table field called "IsRenderable" that we store when
			// we first save the image.
			if( file == null || !ContentTypes.IsImageType( file.ContentType ) || thumbnailResourceGetter == null )
				return Enumerable.Empty<PhrasingComponent>().Materialize();
			return new EwfImage( new ImageSetup( null, sizesToAvailableWidth: true ), thumbnailResourceGetter( file.FileId ) ).ToCollection();
		}

		// NOTE: Use this from blob file manager, etc.
		/// <summary>
		/// Returns a link to download a file with the given file collection ID.
		/// If no file is associated with the given file collection ID, returns a literal control with textIfNoFile text.
		/// The file name is used as the label unless labelOverride is specified.
		/// SystemBlobFileManagementProvider must be implemented.
		/// </summary>
		public static IReadOnlyCollection<PhrasingComponent> GetFileButton( int fileCollectionId, string labelOverride = null, string textIfNoFile = "" ) {
			var file = BlobStorageStatics.GetFirstFileFromCollection( fileCollectionId );
			if( file == null )
				return textIfNoFile.ToComponents();
			return new EwfButton(
				new StandardButtonStyle( labelOverride ?? file.FileName, buttonSize: ButtonSize.ShrinkWrap ),
				behavior: new PostBackBehavior(
					postBack: PostBack.CreateFull(
						id: PostBack.GetCompositeId( "ewfFile", file.FileId.ToString() ),
						actionGetter: () => new PostBackAction(
							new PageReloadBehavior(
								secondaryResponse: new SecondaryResponse(
									new BlobFileResponse( BlobStorageStatics.GetFirstFileFromCollection( fileCollectionId ).FileId, () => true ),
									false ) ) ) ) ) ).ToCollection();
		}

		/// <summary>
		/// Returns a link to download a file with the given file ID.
		/// If no file is associated with the given file collection ID, returns a literal control with textIfNoFile text.
		/// The file name is used as the label unless labelOverride is specified.
		/// SystemBlobFileManagementProvider must be implemented.
		/// </summary>
		public static IReadOnlyCollection<PhrasingComponent> GetFileButtonFromFileId( int fileId, string labelOverride = null, string textIfNoFile = "" ) {
			var file = BlobStorageStatics.SystemProvider.GetFile( fileId );
			if( file == null )
				return textIfNoFile.ToComponents();
			return new EwfButton(
				new StandardButtonStyle( labelOverride ?? file.FileName, buttonSize: ButtonSize.ShrinkWrap ),
				behavior: new PostBackBehavior(
					postBack: PostBack.CreateFull(
						id: PostBack.GetCompositeId( "ewfFile", file.FileId.ToString() ),
						actionGetter: () => new PostBackAction(
							new PageReloadBehavior( secondaryResponse: new SecondaryResponse( new BlobFileResponse( fileId, () => true ), false ) ) ) ) ) ).ToCollection();
		}
	}
}