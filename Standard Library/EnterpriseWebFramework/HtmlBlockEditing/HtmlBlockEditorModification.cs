using System;
using RedStapler.StandardLibrary.DataAccess;

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
		public void Execute( DBConnection cn ) {
			// Do this after all validation so that validation doesn't get confused by our app-relative URL prefix "merge fields". We have seen a system run into
			// problems while doing additional validation to verify that all words preceded by @@ were valid system-specific merge fields; it was mistakenly picking
			// up our app-relative prefixes, thinking that they were merge fields, and complaining that they were not valid.
			var encodedHtml = HtmlBlockStatics.EncodeIntraSiteUris( Html );

			var setup = EwfApp.Instance as HtmlBlockEditingSetup;
			if( htmlBlockId == null )
				htmlBlockId = setup.InsertHtmlBlock( cn, encodedHtml );
			else
				setup.UpdateHtml( cn, htmlBlockId.Value, encodedHtml );

			idSetter( htmlBlockId.Value );
		}
	}
}