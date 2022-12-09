using EnterpriseWebLibrary.Website.StaticFiles;

namespace EnterpriseWebLibrary.Website.Providers {
	internal class StandardPageLogic: AppStandardPageLogicProvider {
		protected override List<ResourceInfo> GetStyleSheets() => new() { new TestCss(), new ExternalResource( "//cdn.datatables.net/1.13.1/css/jquery.dataTables.min.css" ) };

		protected override List<ResourceInfo> GetJavaScriptFiles() => new() { new ExternalResource( "//cdn.datatables.net/1.13.1/js/jquery.dataTables.min.js" ) };

		// GMS NOTE: What's the best selector to use here, going forward?
		protected override string JavaScriptDocumentReadyFunctionCall => "$( 'table.responsiveDataTable' ).DataTable( { responsive: true, pageLength: 25 } );";
	}
}