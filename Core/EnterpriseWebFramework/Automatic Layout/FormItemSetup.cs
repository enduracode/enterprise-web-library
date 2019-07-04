using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a form item.
	/// </summary>
	public class FormItemSetup {
		internal readonly DisplaySetup DisplaySetup;
		internal readonly int? ColumnSpan;
		internal readonly TextAlignment TextAlignment;

		/// <summary>
		/// Creates a form item setup object.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="columnSpan">Only applies to <see cref="FormItemList.CreateGrid"/>.</param>
		/// <param name="textAlignment"></param>
		public FormItemSetup( DisplaySetup displaySetup = null, int? columnSpan = null, TextAlignment textAlignment = TextAlignment.NotSpecified ) {
			DisplaySetup = displaySetup;
			ColumnSpan = columnSpan;
			TextAlignment = textAlignment;
		}
	}
}