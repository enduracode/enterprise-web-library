using EnterpriseWebLibrary.DataAccess;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

[ PublicAPI ]
public static class MvcStatics {
	private class EwfActionFilterAttribute: IActionFilter {
		void IActionFilter.OnActionExecuting( ActionExecutingContext filterContext ) {
			DataAccessState.Current.DisableCache();
		}

		void IActionFilter.OnActionExecuted( ActionExecutedContext filterContext ) {
			try {
				if( filterContext.Exception == null )
					AutomaticDatabaseConnectionManager.Current.CommitTransactionsAndExecuteNonTransactionalModificationMethods( true );
			}
			finally {
				DataAccessState.Current.ResetCache();
			}
		}
	}

	// This needs to be updated for ASP.NET Core MVC.
	//public static void ConfigureMvc() {
	//	AreaRegistration.RegisterAllAreas();

	//	GlobalFilters.Filters.Add( new EwfActionFilterAttribute() );

	//	RouteTable.Routes.IgnoreRoute( "{resource}.axd/{*pathInfo}" );
	//	RouteTable.Routes.IgnoreRoute( "Ewf/{*path}" );
	//}
}