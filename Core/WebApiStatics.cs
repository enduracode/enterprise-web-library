using System.Web.Http;
using System.Web.Http.ExceptionHandling;

namespace EnterpriseWebLibrary {
	public static class WebApiStatics {
		private class TelemetryExceptionLogger: ExceptionLogger {
			public override void Log( ExceptionLoggerContext context ) {
				TelemetryStatics.ReportError( context.Exception );
			}
		}

		public static void ConfigureWebApi( HttpConfiguration config ) {
			config.MapHttpAttributeRoutes();
			config.Services.Add( typeof( IExceptionLogger ), new TelemetryExceptionLogger() );
		}
	}
}