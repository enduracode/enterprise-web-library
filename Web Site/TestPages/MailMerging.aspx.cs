using System;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.MailMerging;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class MailMerging: EwfPage {
		protected override void loadData() {
			ph.AddControlsReturnThis(
				MergeStatics.CreateEmptyPseudoTableRowTree().ToFieldTreeDisplay( "Merge Fields" ),
				new LegacySection(
					"A Pseudo Row",
					MergeStatics.CreatePseudoTableRowTree( new PseudoTableRow( 4 ).ToCollection() )
						.ToRowTreeDisplay(
							new MergeFieldNameTree(
								new[] { "Test", "FullName" },
								childNamesAndChildren: Tuple.Create( "Things", new MergeFieldNameTree( "Another".ToCollection() ) ).ToCollection() ),
							omitListIfSingleRow: true )
						.ToCollection() ) );
		}
	}
}