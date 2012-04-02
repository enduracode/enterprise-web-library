using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.StandardLibrary.WebFileSending {
	/// <summary>
	/// A special FileCreator that is capable of scaling an image on the fly.
	/// </summary>
	public class ImageFileCreator: FileCreator {
		/// <summary>
		/// Pass null for forcedWidth to keep the existing width.  If you pass a non-null value, the image will be scaled to the given width and maintain aspect ratio.
		/// </summary>
		public ImageFileCreator( int fileId, int? forcedWidth ) {
			// NOTE: I do not like how I have to duplicate this from FileCreator or how FileCreator has to have a blank constructor to let me do this.
			method = delegate( DBConnection cn ) {
				var file = BlobFileOps.SystemProvider.GetFile( cn, fileId );
				var contents = BlobFileOps.SystemProvider.GetFileContents( cn, fileId );
				return new ImageToBeSent( file.FileName, file.ContentType, contents, forcedWidth );
			};
		}
	}
}