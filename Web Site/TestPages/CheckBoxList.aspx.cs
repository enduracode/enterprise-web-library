using System.Linq;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class CheckBoxList: EwfPage {
		public partial class Info {
			protected override void init( DBConnection cn ) {}
		}

		protected override void LoadData( DBConnection cn ) {
			ph.AddControlsReturnThis( new EwfCheckBoxList<int>( from i in Enumerable.Range( 0, 20 ) select EwfListItem.Create( i, "Item " + i ),
			                                                    new[] { 3, 9, 19 },
			                                                    includeSelectAndDeselectAllButtons: true ) );
		}
	}
}