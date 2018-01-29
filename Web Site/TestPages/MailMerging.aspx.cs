using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.MailMerging;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class MailMerging: EwfPage {
		protected override void loadData() {
			ph.AddControlsReturnThis(
				MergeStatics.CreateEmptyPseudoTableRowTree().ToFieldTreeDisplay( "Merge Fields" ),
				MergeStatics.CreatePseudoTableRowTree( new PseudoTableRow( 4 ).ToCollection() ).ToRowTreeDisplay( null ) );
		}
	}
}