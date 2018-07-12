using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An ethereal component that can be autofocused.
	/// </summary>
	public class EtherealAutofocusRegion: EtherealComponent {
		internal readonly AutofocusCondition Condition;
		private readonly IReadOnlyCollection<EtherealComponentOrElement> children;

		/// <summary>
		/// Creates an autofocus region.
		/// </summary>
		/// <param name="condition">Do not pass null.</param>
		/// <param name="children"></param>
		public EtherealAutofocusRegion( AutofocusCondition condition, IReadOnlyCollection<EtherealComponent> children ) {
			Condition = condition;
			this.children = children;
		}

		IReadOnlyCollection<EtherealComponentOrElement> EtherealComponent.GetChildren() {
			return children;
		}
	}
}