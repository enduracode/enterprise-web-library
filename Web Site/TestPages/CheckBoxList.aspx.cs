using System.Linq;
using RedStapler.StandardLibrary.EnterpriseWebFramework;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class CheckBoxList: EwfPage {
		protected override void loadData() {
			ph.AddControlsReturnThis(
				new EwfCheckBoxList<int>(
					from i in Enumerable.Range( 0, 20 ) select SelectListItem.Create( i, "Item " + i ),
					new[] { 3, 9, 19 },
					includeSelectAndDeselectAllButtons: true ) );
		}

		public override bool IsAutoDataUpdater { get { return true; } }
	}
}