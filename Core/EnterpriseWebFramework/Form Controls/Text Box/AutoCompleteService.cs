using System.Collections.Generic;
using System.Web.Script.Serialization;
using EnterpriseWebLibrary.TewlContrib;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public abstract class AutoCompleteService: ResourceBase {
		protected sealed override bool disablesUrlNormalization => base.disablesUrlNormalization;

		protected override EwfSafeRequestHandler getOrHead() =>
			new EwfSafeResponseWriter(
				EwfResponse.Create( ContentTypes.PlainText, new EwfResponseBodyCreator( () => new JavaScriptSerializer().Serialize( getItems() ) ) ) );

		protected abstract IEnumerable<AutoCompleteItem> getItems();
	}
}