#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class ToolTip: EtherealComponent {
		private readonly IReadOnlyCollection<EtherealComponent> children;

		internal ToolTip( IReadOnlyCollection<FlowComponent> content, out Func<string, string> jsInitStatementGetter, string title = "" ) {
			var id = new ElementId();
			children = new DisplayableElement(
				context => new DisplayableElementData(
					new DisplaySetup( false ),
					() => new DisplayableElementLocalData( "div", focusDependentData: new DisplayableElementFocusDependentData( includeIdAttribute: true ) ),
					clientSideIdReferences: id.ToCollection(),
					children: content ) ).ToCollection();
			jsInitStatementGetter = targetId => id.Id.Any()
				                                    // If changing this, note that the Basic style sheet may contain qTip2-specific rules.
				                                    ? "$( '#" + targetId + "' ).qtip( { content: { text: $( '#" + id.Id + "' )" +
				                                      ( title.Any() ? ", title: { text: '" + title + "' }" : "" ) + " }, position: { container: $( '#" +
				                                      PageBase.FormId +
				                                      "' ), viewport: $( window ), adjust: { method: 'flipinvert shift' } }, show: { event: 'click', delay: 0 }, hide: { event: 'unfocus' }, style: { classes: 'qtip-bootstrap' } } );"
				                                    : "";
		}

		IReadOnlyCollection<EtherealComponentOrElement> EtherealComponent.GetChildren() {
			return children;
		}
	}
}