// EwlPage

namespace EnterpriseWebLibrary.Website.WebFrameworkDemo {
	partial class CheckboxListDemo {
		protected override string getResourceName() => "Checkbox List";

		protected override PageContent getContent() =>
			new UiPageContent( isAutoDataUpdater: true ).Add(
				new CheckboxList<int>(
						CheckboxListSetup.Create(
							from i in Enumerable.Range( 1, 20 ) select SelectListItem.Create( i, "Item " + i ),
							includeSelectAndDeselectAllButtons: true,
							minColumnWidth: 20.ToEm() ),
						new[] { 3, 9, 19 } ).ToFormItem()
					.ToComponentCollection() );
	}
}

namespace EnterpriseWebLibrary.Website.WebFrameworkDemo {
	partial class CheckboxListDemo {
		protected override UrlHandler getUrlParent() => new LegacyUrlFolderSetup();
	}
}