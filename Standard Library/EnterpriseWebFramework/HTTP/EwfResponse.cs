using System;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An HTTP response, minus any caching information.
	/// </summary>
	public class EwfResponse {
		internal readonly string ContentType;
		internal readonly Func<string> FileNameCreator;
		internal readonly EwfResponseBodyCreator BodyCreator;

		/// <summary>
		/// Creates a response. 
		/// </summary>
		/// <param name="contentType">The media type of the response. We recommend that you always specify this, but pass the empty string if you don't have it. Do
		/// not pass null.</param>
		/// <param name="bodyCreator">The response body creator.</param>
		/// <param name="fileNameCreator">A function that creates the file name for saving the response. If you return a nonempty string, the response will be
		/// processed as an attachment with the specified file name. Do not return null from the function.</param>
		public EwfResponse( string contentType, EwfResponseBodyCreator bodyCreator, Func<string> fileNameCreator = null ) {
			ContentType = contentType;
			FileNameCreator = fileNameCreator ?? ( () => "" );
			BodyCreator = bodyCreator;
		}

		/// <summary>
		/// EWF use only.
		/// </summary>
		public EwfResponse( FullResponse fullResponse ) {
			ContentType = fullResponse.ContentType;
			FileNameCreator = () => fullResponse.FileName;
			BodyCreator = fullResponse.TextBody != null
				              ? new EwfResponseBodyCreator( () => fullResponse.TextBody )
				              : new EwfResponseBodyCreator( () => fullResponse.BinaryBody );
		}

		internal FullResponse CreateFullResponse() {
			return BodyCreator.BodyIsText
				       ? new FullResponse( ContentType, FileNameCreator(), BodyCreator.TextBodyCreator() )
				       : new FullResponse( ContentType, FileNameCreator(), BodyCreator.BinaryBodyCreator() );
		}
	}
}