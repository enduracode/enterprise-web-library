// EwlPage

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Admin {
	partial class CssElements {
		public override string ResourceName => "CSS Elements";

		protected override PageContent getContent() =>
			new UiPageContent().Add( new StackList( CssPreprocessingStatics.Elements.OrderBy( i => i.Name ).Select( i => i.Name.ToComponentListItem() ) ) );
	}
}