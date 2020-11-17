using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using Tewl.Tools;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class ComponentLists: EwfPage {
		protected override void loadData() {
			ph.AddControlsReturnThis(
				new Section(
						"List of form controls",
						new StackList(
								Enumerable.Range( 1, 3 )
									.Select( i => new TextControl( "", false, maxLength: 10, validationMethod: ( value, validator ) => {} ).ToFormItem().ToListItem() ) )
							.ToCollection() ).ToCollection()
					.GetControls() );
		}
	}
}