using System.Web.Http;

namespace EnterpriseWebLibrary {
	public static class WebApiStatics {
		public static void ConfigureWebApi( HttpConfiguration config ) {
			config.MapHttpAttributeRoutes();
		}
	}
}