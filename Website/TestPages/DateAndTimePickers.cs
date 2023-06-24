// EwlPage

namespace EnterpriseWebLibrary.Website.TestPages {
	partial class DateAndTimePickers {
		protected override string getResourceName() => "Date/Time Controls";

		protected override PageContent getContent() {
			var list = FormItemList.CreateStack();
			list.AddFormItems(
				new DateControl( null, true, validationMethod: ( _, _ ) => {} ).ToFormItem( label: "Date control".ToComponents() ),
				new TimeControl( null, true, validationMethod: ( _, _ ) => {} ).ToFormItem( label: "Time control".ToComponents() ),
				new TimeControl( null, true, minuteInterval: 30, validationMethod: ( _, _ ) => {} ).ToFormItem( label: "Drop-down time control".ToComponents() ),
				new DurationControl( null, true, validationMethod: ( _, _ ) => {} ).ToFormItem( label: "Duration control".ToComponents() ) );
			return new UiPageContent( isAutoDataUpdater: true ).Add( list );
		}
	}
}

namespace EnterpriseWebLibrary.Website.TestPages {
	partial class DateAndTimePickers {
		protected override UrlHandler getUrlParent() => new LegacyUrlFolderSetup();
	}
}