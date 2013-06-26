using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.IO;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A file upload control.
	/// </summary>
	public class EwfFileUpload: WebControl, ControlTreeDataLoader, FormControl<RsFile> {
		private FileUpload fileUpload;

		RsFile FormControl<RsFile>.DurableValue { get { return null; } }
		string FormControl.DurableValueAsString { get { return ""; } }

		void ControlTreeDataLoader.LoadData() {
			this.AddControlsReturnThis( fileUpload = new FileUpload() );
		}

		void FormControl.AddPostBackValueToDictionary( PostBackValueDictionary postBackValues ) {
			postBackValues.Add( this, fileUpload.HasFile ? new RsFile( fileUpload.FileBytes, fileUpload.FileName, fileUpload.PostedFile.ContentType ) : null );
		}

		/// <summary>
		/// Gets the post back value.
		/// </summary>
		public RsFile GetPostBackValue( PostBackValueDictionary postBackValues ) {
			return postBackValues.GetValue( this );
		}

		/// <summary>
		/// Returns true if the value changed on this post back.
		/// </summary>
		public bool ValueChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return postBackValues.ValueChangedOnPostBack( this );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Span; } }
	}
}