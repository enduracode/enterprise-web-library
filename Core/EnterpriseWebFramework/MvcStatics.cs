using System.Web.Mvc;
using System.Web.Routing;
using EnterpriseWebLibrary.DataAccess;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public static class MvcStatics {
		private class EwfActionFilterAttribute: ActionFilterAttribute {
			public override void OnActionExecuting( ActionExecutingContext filterContext ) {
				DataAccessState.Current.DisableCache();
			}

			public override void OnActionExecuted( ActionExecutedContext filterContext ) {
				try {
					if( filterContext.Exception == null )
						AppRequestState.Instance.CommitDatabaseTransactionsAndExecuteNonTransactionalModificationMethods();
				}
				finally {
					DataAccessState.Current.ResetCache();
				}
			}
		}

		public static void ConfigureMvc() {
			AreaRegistration.RegisterAllAreas();

			GlobalFilters.Filters.Add( new EwfActionFilterAttribute() );

			RouteTable.Routes.IgnoreRoute( "{resource}.axd/{*pathInfo}" );
			RouteTable.Routes.IgnoreRoute( "Ewf/{*path}" );
		}
	}
}