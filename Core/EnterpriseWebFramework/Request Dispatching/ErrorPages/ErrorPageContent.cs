using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.ErrorPages {
	public class ErrorPageContent: PageContent {
		private static readonly ElementClass elementClass = new( "ewfError" );

		[ UsedImplicitly ]
		private class CssElementCreator: ControlCssElementCreator {
			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
				new CssElement( "ErrorPageBody", "body.{0}".FormatWith( elementClass.ClassName ) ).ToCollection();
		}

		private readonly BasicPageContent basicContent;

		public ErrorPageContent( IReadOnlyCollection<FlowComponent> content, ElementClassSet bodyClasses = null ) {
			basicContent = new BasicPageContent( bodyClasses: elementClass.Add( bodyClasses ?? ElementClassSet.Empty ) ).Add( content );
		}

		protected internal override PageContent GetContent() => basicContent;
	}
}