using System;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The modification class used by the HTML block editor control.
	/// </summary>
	public class HtmlBlockEditorModification {
		private int? htmlBlockId;

		/// <summary>
		/// Gets or sets the HTML.
		/// </summary>
		public string Html { get; set; }

		private readonly Action<int> idSetter;

		internal HtmlBlockEditorModification( int? htmlBlockId, string html, Action<int> idSetter ) {
			this.htmlBlockId = htmlBlockId;
			Html = html;
			this.idSetter = idSetter;
		}

		/// <summary>
		/// Executes the modification and calls the ID setter with the ID of the created or modified HTML block.
		/// </summary>
		public void Execute() {
			if( htmlBlockId == null )
				htmlBlockId = HtmlBlockStatics.CreateHtmlBlock( Html );
			else
				HtmlBlockStatics.UpdateHtmlBlock( htmlBlockId.Value, Html );

			idSetter( htmlBlockId.Value );
		}
	}
}