﻿#nullable disable
using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class LineList: FlowComponent {
		private readonly IReadOnlyCollection<FlowComponentOrNode> children;

		/// <summary>
		/// Creates a line list.
		/// </summary>
		/// <param name="items">The items. Do not pass null.</param>
		/// <param name="generalSetup">The general setup object for the list.</param>
		/// <param name="alignment">The horizontal alignment of the items in the list.</param>
		/// <param name="verticalAlignment">The default vertical alignment of the items in the list.</param>
		public LineList(
			IEnumerable<LineListItem> items, ComponentListSetup generalSetup = null, FlexboxAlignment alignment = FlexboxAlignment.NotSpecified,
			FlexboxVerticalAlignment verticalAlignment = FlexboxVerticalAlignment.NotSpecified ) {
			children =
				( generalSetup ?? new ComponentListSetup() ).GetComponents(
					CssElementCreator.LineListClass.Add( FlexboxAlignmentStatics.Class( alignment ) ).Add( FlexboxVerticalAlignmentStatics.Class( verticalAlignment ) ),
					from i in items select i.ItemAndComponentGetter() );
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}