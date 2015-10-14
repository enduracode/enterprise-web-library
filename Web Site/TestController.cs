using System.Net.Http;
using System.Web.Http;

namespace EnterpriseWebLibrary.WebSite {
	[ RoutePrefix( "controller" ) ]
	public class TestController: ApiController {
		[ Route( "" ) ]
		[ HttpGet ]
		public HttpResponseMessage Get() {
			return Request.CreateResponse( "Hello world!" );
		}
	}
}