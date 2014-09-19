using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI;
using Aspose.Pdf.Facades;
using RedStapler.StandardLibrary.IO;
using RedStapler.StandardLibrary.Validation;
using RedStapler.StandardLibrary.WebFileSending;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Contains methods for working with BLOB files.
	/// </summary>
	public static class BlobFileOps {
		private const string providerName = "BlobFileManagement";
		private static SystemBlobFileManagementProvider provider;

		internal static void Init( Type systemLogicType ) {
			provider = StandardLibraryMethods.GetSystemLibraryProvider( systemLogicType, providerName ) as SystemBlobFileManagementProvider;
		}

		internal static SystemBlobFileManagementProvider SystemProvider {
			get {
				if( provider == null )
					throw StandardLibraryMethods.CreateProviderNotFoundException( providerName );
				return provider;
			}
		}

		/// <summary>
		/// Returns the first file in the specified file collection, or null if the collection is empty.
		/// </summary>
		public static BlobFile GetFirstFileFromCollection( int fileCollectionId ) {
			return SystemProvider.GetFilesLinkedToFileCollection( fileCollectionId ).FirstOrDefault();
		}

		/// <summary>
		/// Copies the specified file collection and returns the ID of the copy.
		/// </summary>
		public static int CopyFileCollection( int fileCollectionId ) {
			var newFileCollectionId = SystemProvider.InsertFileCollection();
			foreach( var file in SystemProvider.GetFilesLinkedToFileCollection( fileCollectionId ) )
				SystemProvider.InsertFile( newFileCollectionId, file.FileName, SystemProvider.GetFileContents( file.FileId ), file.ContentType );
			return newFileCollectionId;
		}

		/// <summary>
		/// Uploaded file cannot be null. But if uploadedFile.HasFile is false, this will be a no-op.
		/// Pass null for acceptableFileExtensions if there is no restriction on file extension.
		/// PerformAdditionalImageValidation cannot be null but may be an empty delegate.
		/// </summary>
		public static void ValidateUploadedFile(
			Validator validator, EwfFileUpload uploadedFile, string[] acceptableFileExtensions, Action<Validator, System.Drawing.Image> performAdditionalImageValidation,
			bool mustBeRenderableImage ) {
			var file = uploadedFile.GetPostBackValue( AppRequestState.Instance.EwfPageRequestState.PostBackValues );
			if( file == null )
				return;

			// Perform generic file validation.
			if( acceptableFileExtensions != null && !FileExtensions.MatchesAGivenExtension( file.FileName, acceptableFileExtensions ) ) {
				validator.NoteErrorAndAddMessage( Translation.UnacceptableFileExtension + " " + acceptableFileExtensions.GetCommaDelimitedStringFromCollection() );
				// Don't bother trying to see if it's an image and parse the image. The file extension message be more detailed than the messages those errors produce.
				return;
			}

			// Perform image-specific validation if necessary.
			if( mustBeRenderableImage ) {
				// Make sure it is an image according to its content type.
				if( !ContentTypes.IsImageType( GetContentTypeForPostedFile( file ) ) )
					validator.NoteErrorAndAddMessage( "Please upload a valid image file." );
				else {
					// Make sure it is an image type that we understand. Also perform optional custom validation.
					try {
						using( var stream = new MemoryStream( file.Contents ) ) {
							var image = System.Drawing.Image.FromStream( stream );
							performAdditionalImageValidation( validator, image );
						}
					}
					catch( ArgumentException ) {
						// If we end up in this catch block, it means that System.Drawing.Image does not understand our image. Since we already know that our content type
						// is image at this point, this usually means that the file is some sort of unsupported image format, like NEF.
						validator.NoteErrorAndAddMessage( "The uploaded image file is in an unsupported format." );
					}
				}
			}
		}

		/// <summary>
		/// Provides a height/width image validation method without you having to create a custom validation method.
		/// </summary>
		public static Action<Validator, System.Drawing.Image> GetWidthAndHeightImageValidationMethod( string subject, int width, int height ) {
			return ( validator2, image ) => {
				if( image.Height != height || image.Width != width )
					validator2.NoteErrorAndAddMessage( subject + " must be " + width + "x" + height + " pixels." );
			};
		}

		/// <summary>
		/// Returns null if the file is null, the file is not an image, or there is no thumbnail page info creator.
		/// </summary>
		internal static Control GetThumbnailControl( BlobFile file, Func<decimal, PageInfo> thumbnailPageInfoCreator ) {
			// NOTE: We'd like to check here whether the file is a renderable image or not. But we can't because we don't have the file contents.
			// So, we'll have to make sure that all ThumbnailPageInfoCreators provide a page that knows how to handle NEF files (ideally we'd want
			// it to behave as if there was no thumbnail at all if there is an unrenderable image file).
			// The only alternative to this that I can think of is creating a new file table field called "IsRenderable" that we store when
			// we first save the image.
			if( file == null || !ContentTypes.IsImageType( file.ContentType ) || thumbnailPageInfoCreator == null )
				return null;
			return new EwfImage( thumbnailPageInfoCreator( file.FileId ).GetUrl() ) { SizesToAvailableWidth = true };
		}

		// NOTE: Use this from blob file manager, etc.
		/// <summary>
		/// Returns a link to download a file with the given file collection ID.
		/// If no file is associated with the given file collection ID, returns a literal control with textIfNoFile text.
		/// The file name is used as the label unless labelOverride is specified.
		/// SystemBlobFileManagementProvider must be implemented.
		/// </summary>
		public static Control GetFileLink( int fileCollectionId, string labelOverride = null, string textIfNoFile = "" ) {
			var file = GetFirstFileFromCollection( fileCollectionId );
			if( file == null )
				return textIfNoFile.GetLiteralControl();
			return
				new PostBackButton(
					PostBack.CreateFull(
						id: PostBack.GetCompositeId( "ewfFile", file.FileId.ToString() ),
						actionGetter: () => new PostBackAction( FileCreator.CreateFromFileCollection( fileCollectionId ) ) ),
					new TextActionControlStyle( labelOverride ?? file.FileName ),
					false );
		}

		/// <summary>
		/// Returns a link to download a file with the given file ID.
		/// If no file is associated with the given file collection ID, returns a literal control with textIfNoFile text.
		/// The file name is used as the label unless labelOverride is specified.
		/// SystemBlobFileManagementProvider must be implemented.
		/// </summary>
		public static Control GetFileLinkFromFileId( int fileId, string labelOverride = null, string textIfNoFile = "" ) {
			var file = SystemProvider.GetFile( fileId );
			if( file == null )
				return textIfNoFile.GetLiteralControl();
			return
				new PostBackButton(
					PostBack.CreateFull(
						id: PostBack.GetCompositeId( "ewfFile", file.FileId.ToString() ),
						actionGetter: () => new PostBackAction( new FileCreator( fileId ) ) ),
					new TextActionControlStyle( labelOverride ?? file.FileName ),
					false );
		}

		/// <summary>
		/// SystemBlobFileManagementProvider must be implemented.
		/// You should check other meta information about the file (such as the extension) before calling this expensive method.
		/// </summary>
		public static bool IsValidPdfFile( int fileId ) {
			var contents = SystemProvider.GetFileContents( fileId );
			return IsValidPdfFile( contents );
		}

		/// <summary>
		/// Returns true if the file is a valid PDF file.
		/// You should check other meta information about the file (such as the extension) before calling this expensive method.
		/// </summary>
		public static bool IsValidPdfFile( byte[] contents ) {
			using( var memoryStream = new MemoryStream( contents ) )
				return IsValidPdfFile( memoryStream );
		}

		/// <summary>
		/// Returns true if the file is a valid PDF file. Caller is responsible for opening and cleaning up the stream.
		/// You should check other meta information about the file (such as the extension) before calling this expensive method.
		/// </summary>
		public static bool IsValidPdfFile( Stream sourceStream ) {
			try {
				return new PdfFileInfo( sourceStream ).IsPdfFile;
			}
			catch {
				// We catch all exceptions here because we don't trust Aspose to consistently throw a particular type of exception when the PDF is invalid.
				return false;
			}
		}

		/// <summary>
		/// Returns the content type of the given HttpPostedFile.
		/// </summary>
		/// There is no such thing as an official mapping of file extentions to content types or vice versa.
		/// Windows has a one-one mapping in the registry, which is how the client's (Windows) computer determines the file type.
		/// Windows determines the content type through no other means than the file extension. This also means that
		/// different clients can have different content types and are even able to spoof the content-type. HttpPostedFile does nothing more
		/// than determine the content-type given by the client headers. Because of this, I think that would should
		/// first be consulting our official mappings of file extensions to content types, and then fall back on the .NET provided method.
		/// Maybe we should never be trusting the client's content-type, since it's conceivable it could lead to a buffer-overflow attack.
		/// We could fall back to consulting the server's content-type mappings instead of trusting the client at all, but this is flawed too
		/// since all it takes to make us determine the file to be another content-type is to change the extension.
		internal static string GetContentTypeForPostedFile( RsFile file ) {
			var type = ContentTypes.GetContentType( file.FileName );
			return type != String.Empty ? type : file.ContentType;
		}

		internal static IEnumerable<BlobFile> OrderByName( this IEnumerable<BlobFile> rows ) {
			return rows.OrderBy( i => i.FileName ).ThenBy( i => i.FileId );
		}

		internal static IEnumerable<BlobFile> OrderByUploadedDateDescending( this IEnumerable<BlobFile> rows ) {
			return rows.OrderByDescending( i => i.UploadedDate ).ThenByDescending( i => i.FileId );
		}
	}
}