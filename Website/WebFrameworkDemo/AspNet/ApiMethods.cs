namespace EnterpriseWebLibrary.Website.WebFrameworkDemo.AspNet;

public static class ApiMethods {
	public static string Get() {
		ResourceBase.ExecuteDataModificationMethod( () => TelemetryStatics.SendAdministratorNotification( "GET called in minimal API." ) );
		return $"This is a minimal-API method within the {EwlStatics.EwlInitialism} web framework.";
	}
}