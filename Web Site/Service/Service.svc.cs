namespace EnterpriseWebLibrary.WebSite.Service {
	// NOTE: If you change the class name "Service" here, you must also update the reference to "Service" in Web.config.
	public class Service: IService {
		public string[] GetAutoFillTextBoxChoices( string prefixText, int count ) {
			return new[] { "one", "two", "three" };
		}
	}
}