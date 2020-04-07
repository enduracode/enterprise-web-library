using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a table cell.
	/// </summary>
	public class TableCellSetup {
		internal readonly int FieldSpan;
		internal readonly int ItemSpan;
		internal readonly IEnumerable<string> Classes;
		internal readonly TextAlignment TextAlignment;
		internal readonly ElementActivationBehavior ActivationBehavior;

		/// <summary>
		/// Creates a cell setup object.
		/// </summary>
		/// <param name="fieldSpan">The number of fields this cell will span.
		/// NOTE: Don't allow this to be less than one. Zero is allowed by the HTML spec but is too difficult for us to implement right now.
		/// </param>
		/// <param name="itemSpan">The number of items this cell will span.
		/// NOTE: Don't allow this to be less than one. Zero is allowed by the HTML spec but is too difficult for us to implement right now.
		/// </param>
		/// <param name="classes">The CSS class(es).</param>
		/// <param name="textAlignment">The text alignment of the cell.</param>
		/// <param name="activationBehavior">The activation behavior.</param>
		public TableCellSetup(
			int fieldSpan = 1, int itemSpan = 1, IEnumerable<string> classes = null, TextAlignment textAlignment = TextAlignment.NotSpecified,
			ElementActivationBehavior activationBehavior = null ) {
			FieldSpan = fieldSpan;
			ItemSpan = itemSpan;
			Classes = classes ?? new string[ 0 ];
			TextAlignment = textAlignment;
			ActivationBehavior = activationBehavior;
		}
	}
}