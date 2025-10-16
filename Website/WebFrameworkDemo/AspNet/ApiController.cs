using Microsoft.AspNetCore.Mvc;

namespace EnterpriseWebLibrary.Website.WebFrameworkDemo.AspNet;

[ ApiController ]
[ Route( "asp-net/api-controller" ) ]
public class ApiController: ControllerBase {
	[ HttpGet ]
	public string Get() {
		ResourceBase.ExecuteDataModificationMethod( () => TelemetryStatics.SendAdministratorNotification( "GET called on API controller." ) );
		return $"This is an API controller within the {EwlStatics.EwlInitialism} web framework.";
	}
}