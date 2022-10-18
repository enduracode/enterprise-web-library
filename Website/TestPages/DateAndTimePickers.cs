using EnterpriseWebLibrary.EnterpriseWebFramework;

// EwlPage

namespace EnterpriseWebLibrary.Website.TestPages {
	partial class DateAndTimePickers {
		public override string ResourceName => "Date/Time Controls";

		protected override PageContent getContent() {
			var list = FormItemList.CreateStack();
			list.AddFormItems(
				new DateControl( null, true ).ToFormItem( label: "Date control".ToComponents() ),
				new TimeControl( null, true ).ToFormItem( label: "Time control".ToComponents() ),
				new TimeControl( null, true, minuteInterval: 30 ).ToFormItem( label: "Drop-down time control".ToComponents() ),
				new DateAndTimeControl( null, true ).ToFormItem( label: "Date and time control".ToComponents() ),
				new DurationControl( null, true ).ToFormItem( label: "Duration control".ToComponents() ) );
			return new UiPageContent( isAutoDataUpdater: true ).Add( list );
		}
	}
}

namespace EnterpriseWebLibrary.Website.TestPages {
	partial class DateAndTimePickers {
		protected override UrlHandler getUrlParent() => new LegacyUrlFolderSetup();
	}
}