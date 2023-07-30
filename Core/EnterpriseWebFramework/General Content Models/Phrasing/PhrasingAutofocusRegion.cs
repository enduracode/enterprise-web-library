#nullable disable
using System.Collections.Generic;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A phrasing component that can be autofocused.
	/// </summary>
	public class PhrasingAutofocusRegion: PhrasingComponent {
		private readonly IReadOnlyCollection<FlowComponentOrNode> children;

		/// <summary>
		/// Creates an autofocus region.
		/// </summary>
		/// <param name="condition">Do not pass null.</param>
		/// <param name="children"></param>
		public PhrasingAutofocusRegion( AutofocusCondition condition, IReadOnlyCollection<PhrasingComponent> children ) {
			this.children = new FlowAutofocusRegion( condition, children ).ToCollection();
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}