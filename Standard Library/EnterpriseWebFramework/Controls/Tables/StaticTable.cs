using System;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// This control is not supported and will be removed. Do not use.
	/// </summary>
	[ ParseChildren( false ) ]
	public class StaticTable: WebControl, ControlTreeDataLoader {
		private bool? isForm;

		/// <summary>
		/// Gets or sets whether or not this table will display as an input form.
		/// </summary>
		public bool IsForm {
			internal get {
				if( !isForm.HasValue )
					throw new ApplicationException( "Please explicitly specify either true or false for the IsForm attribute." );
				return isForm.Value;
			}
			set { isForm = value; }
		}

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			Attributes.Add( "cellspacing", "0" );

			// We use the property here so we get the informative exception if isForm is null.
			if( IsForm )
				CssClass = CssClass.ConcatenateWithSpace( "ewfStandard" );

			var contrast = false;
			foreach( Control child1 in Controls ) {
				// this handles rows directly within the table element
				decorateRow( child1, ref contrast );

				// this handles rows within thead, tbody, or tfoot row groups that are marked runat=server
				foreach( Control child2 in child1.Controls )
					decorateRow( child2, ref contrast );
			}
		}

		private static void decorateRow( Control child, ref bool contrast ) {
			if( child is HtmlTableRow && child.Visible ) {
				if( contrast )
					( child as HtmlTableRow ).Attributes[ "class" ] = ( ( child as HtmlTableRow ).Attributes[ "class" ] ?? "" ).ConcatenateWithSpace( "ewfContrast" );
				contrast = !contrast;
			}
		}

		/// <summary>
		/// Returns the table tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Table; } }
	}
}