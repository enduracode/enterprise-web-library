using System.Collections.Generic;
using Humanizer;
using JetBrains.Annotations;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.ErrorPages {
	public class ErrorPageContent: PageContent {
		private static readonly ElementClass elementClass = new ElementClass( "ewfError" );

		[ UsedImplicitly ]
		private class CssElementCreator: ControlCssElementCreator {
			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
				new CssElement( "ErrorPageBody", "body.{0}".FormatWith( elementClass.ClassName ) ).ToCollection();
		}

		private readonly BasicPageContent basicContent;

		public ErrorPageContent( IReadOnlyCollection<FlowComponent> content ) {
			basicContent = new BasicPageContent( bodyClasses: elementClass ).Add( content );
		}

		protected internal override PageContent GetContent() => basicContent;
	}
}