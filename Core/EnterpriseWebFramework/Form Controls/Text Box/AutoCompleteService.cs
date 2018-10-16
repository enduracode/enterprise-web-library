using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public abstract class AutoCompleteService: EwfPage {
		protected override EwfSafeRequestHandler requestHandler =>
			new EwfSafeResponseWriter(
				EwfResponse.Create( ContentTypes.PlainText, new EwfResponseBodyCreator( () => new JavaScriptSerializer().Serialize( getItems() ) ) ) );

		protected abstract IEnumerable<AutoCompleteItem> getItems();
	}
}