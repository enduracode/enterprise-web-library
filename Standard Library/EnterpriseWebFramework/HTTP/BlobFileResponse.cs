using System;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An HTTP response based on a BLOB file.
	/// </summary>
	public class BlobFileResponse {
		private readonly BlobFile file;
		private readonly Func<bool> processAsAttachmentGetter;
		private readonly Lazy<int?> forcedImageWidth;

		/// <summary>
		/// Creates a BLOB-file response.
		/// </summary>
		/// <param name="fileId">The ID of the BLOB file.</param>
		/// <param name="processAsAttachmentGetter">A function that gets whether you want the response to be processed as an attachment. Return true if you want
		/// this behavior; otherwise return false or do not pass a function.</param>
		/// <param name="forcedImageWidthGetter">A function that gets the width in pixels to which the specified file should be scaled, while maintaining its aspect
		/// ratio. The file must be an image. Pass null or return null if the file is not an image or you do not want scaling.</param>
		public BlobFileResponse( int fileId, Func<bool> processAsAttachmentGetter, Func<int?> forcedImageWidthGetter = null ) {
			file = BlobFileOps.SystemProvider.GetFile( fileId );
			this.processAsAttachmentGetter = processAsAttachmentGetter ?? ( () => false );
			forcedImageWidth = new Lazy<int?>( forcedImageWidthGetter ?? ( () => null ) );
		}

		internal DateTime FileLastModificationDateAndTime { get { return file.UploadedDate; } }
		internal string MemoryCacheKey { get { return "blobFile-" + file.FileId; } }

		internal EwfResponse GetResponse() {
			return new EwfResponse(
				file.ContentType,
				new EwfResponseBodyCreator(
					() => {
						var contents = BlobFileOps.SystemProvider.GetFileContents( file.FileId );
						if( forcedImageWidth.Value.HasValue )
							contents = StandardLibraryMethods.ResizeImage( contents, forcedImageWidth.Value.Value );
						return contents;
					} ),
				fileNameCreator: () => processAsAttachmentGetter() ? file.FileName : "" );
		}
	}
}