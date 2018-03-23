using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A phrasing component that can be autofocused.
	/// </summary>
	public class PhrasingAutofocusRegion: PhrasingComponent {
		private readonly IEnumerable<FlowComponentOrNode> children;

		/// <summary>
		/// Creates an autofocus region.
		/// </summary>
		/// <param name="condition">Do not pass null.</param>
		/// <param name="children"></param>
		public PhrasingAutofocusRegion( AutofocusCondition condition, IEnumerable<PhrasingComponent> children ) {
			this.children = new FlowAutofocusRegion( condition, children ).ToCollection();
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}