using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using Tewl.Tools;

// EwlPage

namespace EnterpriseWebLibrary.Website.WebFrameworkDemo {
	partial class ComponentLists {
		protected override PageContent getContent() =>
			new UiPageContent().Add(
				new Section(
					"List of form controls",
					new StackList(
							Enumerable.Range( 1, 3 )
								.Select( i => new TextControl( "", false, maxLength: 10, validationMethod: ( value, validator ) => {} ).ToFormItem().ToListItem() ) )
						.ToCollection() ) );
	}
}

namespace EnterpriseWebLibrary.Website.WebFrameworkDemo {
partial class ComponentLists {
protected override UrlHandler getUrlParent() => new LegacyUrlFolderSetup();
}
}