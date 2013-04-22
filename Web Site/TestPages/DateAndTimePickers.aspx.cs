using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class DateAndTimePickers: EwfPage {
		public partial class Info {
			protected override void init( DBConnection cn ) {}
			public override string PageName { get { return "Date/Time Pickers"; } }
		}

		protected override void LoadData( DBConnection cn ) {
			var table = FormItemBlock.CreateFormItemTable();
			table.AddFormItems( FormItem.Create( "Date Picker", new DatePicker( null ) ),
			                    FormItem.Create( "Time Picker", new TimePicker( null ) ),
			                    FormItem.Create( "Date/Time Picker", new DateTimePicker( null ) ) );
			ph.AddControlsReturnThis( table );
		}
	}
}