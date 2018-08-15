using System;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class DateAndTimePickers: EwfPage {
		partial class Info {
			public override string ResourceName => "Date/Time Controls";
		}

		protected override void loadData() {
			var table = FormItemBlock.CreateFormItemTable();
			table.AddFormItems(
				new DateControl( null, true ).ToFormItem( label: "Date control".ToComponents() ),
				FormItem.Create( "Time Picker", new TimePicker( null ) ),
				FormItem.Create( "Date/Time Picker", new DateTimePicker( null ) ),
				FormItem.Create( "Duration Picker", new DurationPicker( TimeSpan.Zero ) ) );
			ph.AddControlsReturnThis( table );
		}

		public override bool IsAutoDataUpdater => true;
	}
}