using EnterpriseWebLibrary.TewlContrib;
using Newtonsoft.Json;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public abstract class AutoCompleteService: ResourceBase {
		protected sealed override bool disablesUrlNormalization => base.disablesUrlNormalization;

		protected override EwfSafeRequestHandler getOrHead() =>
			new EwfSafeResponseWriter(
				EwfResponse.Create( ContentTypes.PlainText, new EwfResponseBodyCreator( () => JsonConvert.SerializeObject( getItems(), Formatting.None ) ) ) );

		protected abstract IEnumerable<AutoCompleteItem> getItems();
	}
}