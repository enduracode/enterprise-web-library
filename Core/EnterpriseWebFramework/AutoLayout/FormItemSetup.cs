using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a form item.
	/// </summary>
	public class FormItemSetup {
		internal readonly DisplaySetup DisplaySetup;
		internal readonly int? CellSpan;
		internal readonly TextAlignment TextAlignment;

		/// <summary>
		/// Creates a form item setup object.
		/// </summary>
		/// <param name="displaySetup">Not yet implemented. Will implement when EnduraCode goal 790 is complete (i.e. when Web Forms dependency is gone).</param>
		/// <param name="cellSpan">Only applies to adjacent layouts.</param>
		/// <param name="textAlignment"></param>
		public FormItemSetup( DisplaySetup displaySetup = null, int? cellSpan = null, TextAlignment textAlignment = TextAlignment.NotSpecified ) {
			DisplaySetup = displaySetup;
			CellSpan = cellSpan;
			TextAlignment = textAlignment;
		}
	}
}