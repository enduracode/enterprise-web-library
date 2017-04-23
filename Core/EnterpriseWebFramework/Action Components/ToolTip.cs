using System;
using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class ToolTip: EtherealComponent {
		private readonly IReadOnlyCollection<EtherealComponentOrElement> children;

		internal ToolTip( IEnumerable<FlowComponent> content, out Func<string, string> jsInitStatementGetter, string title = "" ) {
			var id = new ElementId();
			children = new DisplayableElement(
				context => {
					id.AddId( context.Id );
					return new DisplayableElementData( null, () => new DisplayableElementLocalData( "div", includeIdAttribute: true ), children: content );
				} ).ToCollection();
			jsInitStatementGetter = targetId => id.Id.Any()
				                                    // If changing this, note that the Basic style sheet may contain qTip2-specific rules.
				                                    ? "$( '#" + targetId + "' ).qtip( { content: { text: $( '#" + id.Id + "' )" +
				                                      ( title.Any() ? ", title: { text: '" + title + "' }" : "" ) + " }, position: { container: $( '#" +
				                                      EwfPage.Instance.Form.ClientID +
				                                      "' ), viewport: $( window ), adjust: { method: 'flipinvert shift' } }, show: { event: 'click', delay: 0 }, hide: { event: 'unfocus' }, style: { classes: 'qtip-bootstrap' } } );"
				                                    : "";
		}

		IEnumerable<EtherealComponentOrElement> EtherealComponent.GetChildren() {
			return children;
		}
	}
}