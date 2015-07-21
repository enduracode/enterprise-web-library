using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public abstract class AutoCompleteService: EwfPage {
		protected override EwfSafeResponseWriter responseWriter {
			get {
				return
					new EwfSafeResponseWriter(
						new EwfResponse( ContentTypes.PlainText, new EwfResponseBodyCreator( () => new JavaScriptSerializer().Serialize( getItems() ) ) ) );
			}
		}

		protected abstract IEnumerable<AutoCompleteItem> getItems();
	}
}