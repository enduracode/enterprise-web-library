using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace RedStapler.StandardLibrary.WebFileSending {
	/// <summary>
	/// An image file for sending. Capable of resizing.
	/// </summary>
	public class ImageToBeSent: FileToBeSent {
		/// <summary>
		/// Creates a new image to be sent. Do not pass null for the content type if you don't have it; instead pass the empty string.
		/// </summary>
		public ImageToBeSent( string fileName, string contentType, byte[] contents, int? forcedWidth ): base( fileName, contentType, contents ) {
			if( forcedWidth.HasValue ) {
				// Transform image to correct size
				// NOTE: Investigate using this: http://www.hanselman.com/blog/NuGetPackageOfWeek11ImageResizerEnablesCleanClearImageResizingInASPNET.aspx.
				using( var fromStream = new MemoryStream( contents ) ) {
					using( var imageSource = Image.FromStream( fromStream ) ) {
						var width = forcedWidth.Value;
						var height = getHeightFromImageAndNewWidth( imageSource, width );

						using( var resizedImage = new Bitmap( width, height ) ) {
							using( var gr = Graphics.FromImage( resizedImage ) ) {
								gr.SmoothingMode = SmoothingMode.AntiAlias;
								gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
								gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
								gr.DrawImage( imageSource, 0, 0, width, height );
							}

							using( var toStream = new MemoryStream() ) {
								resizedImage.Save( toStream, ImageFormat.Jpeg );
								binaryContents = toStream.ToArray();
							}
						}
					}
				}
			}
		}

		private static int getHeightFromImageAndNewWidth( Image image, int width ) {
			return width * image.Height / image.Width;
		}
	}
}