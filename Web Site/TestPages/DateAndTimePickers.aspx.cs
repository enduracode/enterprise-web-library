using System;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class DateAndTimePickers: EwfPage {
		partial class Info {
			public override string ResourceName { get { return "Date/Time Pickers"; } }
		}

		protected override void loadData() {
			var table = FormItemBlock.CreateFormItemTable();
			table.AddFormItems(
				FormItem.Create( "Date Picker".GetLiteralControl(), new DatePicker( null ) ),
				FormItem.Create( "Time Picker".GetLiteralControl(), new TimePicker( null ) ),
				FormItem.Create( "Date/Time Picker".GetLiteralControl(), new DateTimePicker( null ) ),
				FormItem.Create( "Duration Picker".GetLiteralControl(), new DurationPicker( TimeSpan.Zero ) ) );
			ph.AddControlsReturnThis( table );
		}

		public override bool IsAutoDataUpdater { get { return true; } }
	}
}