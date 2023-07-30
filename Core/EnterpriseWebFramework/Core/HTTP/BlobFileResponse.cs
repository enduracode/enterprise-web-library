﻿#nullable disable
using EnterpriseWebLibrary.DataAccess.BlobStorage;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An HTTP response based on a BLOB file.
	/// </summary>
	public class BlobFileResponse {
		private readonly BlobFile file;
		private readonly Lazy<bool> processAsAttachment;
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
			file = BlobStorageStatics.SystemProvider.GetFile( fileId );
			processAsAttachment = new Lazy<bool>( processAsAttachmentGetter ?? ( () => false ) );
			forcedImageWidth = new Lazy<int?>( forcedImageWidthGetter ?? ( () => null ) );
		}

		internal DateTime FileLastModificationDateAndTime => file.UploadedDate;

		internal string ETagBase => identifier;
		internal string MemoryCacheKey => identifier;

		private string identifier =>
			"blobFile-{0}-{1}-{2}".FormatWith(
				file.FileId,
				processAsAttachment.Value ? "a" : "i",
				forcedImageWidth.Value.HasValue ? forcedImageWidth.Value.Value.ToString() : "nonscaled" );

		internal EwfResponse GetResponse() {
			return EwfResponse.Create(
				file.ContentType,
				new EwfResponseBodyCreator(
					() => {
						var contents = BlobStorageStatics.SystemProvider.GetFileContents( file.FileId );
						if( forcedImageWidth.Value.HasValue )
							contents = EwlStatics.ResizeImage( contents, forcedImageWidth.Value.Value ).ToArray();
						return contents;
					} ),
				fileNameCreator: () => processAsAttachment.Value ? file.FileName : "" );
		}
	}
}