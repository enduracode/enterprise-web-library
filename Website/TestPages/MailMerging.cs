using System;
using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.MailMerging;
using Tewl.Tools;

// EwlPage

namespace EnterpriseWebLibrary.Website.TestPages {
	partial class MailMerging {
		protected override PageContent getContent() =>
			new UiPageContent().Add(
				MergeStatics.CreateEmptyPseudoTableRowTree()
					.ToFieldTreeDisplay( "Merge Fields" )
					.Append(
						new Section(
							"A Pseudo Row",
							MergeStatics.CreatePseudoTableRowTree( new PseudoTableRow( 4 ).ToCollection() )
								.ToRowTreeDisplay(
									new MergeFieldNameTree(
										new[] { "Test", "FullName" },
										childNamesAndChildren: Tuple.Create( "Things", new MergeFieldNameTree( "Another".ToCollection() ) ).ToCollection() ),
									omitListIfSingleRow: true )
								.ToCollection() ) )
					.Materialize() );
	}
}

namespace EnterpriseWebLibrary.Website.TestPages {
partial class MailMerging {
protected override UrlHandler getUrlParent() => new LegacyUrlFolderSetup();
}
}