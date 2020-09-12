using EnterpriseWebLibrary.EnterpriseWebFramework;
using Tewl.Tools;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class DateAndTimePickers: EwfPage {
		partial class Info {
			public override string ResourceName => "Date/Time Controls";
		}

		protected override void loadData() {
			var list = FormItemList.CreateStack();
			list.AddFormItems(
				new DateControl( null, true ).ToFormItem( label: "Date control".ToComponents() ),
				new TimeControl( null, true ).ToFormItem( label: "Time control".ToComponents() ),
				new TimeControl( null, true, minuteInterval: 30 ).ToFormItem( label: "Drop-down time control".ToComponents() ),
				new DateAndTimeControl( null, true ).ToFormItem( label: "Date and time control".ToComponents() ),
				new DurationControl( null, true ).ToFormItem( label: "Duration control".ToComponents() ) );
			ph.AddControlsReturnThis( list.ToCollection().GetControls() );
		}

		public override bool IsAutoDataUpdater => true;
	}
}