using System.Collections.Generic;
using System.Web.Script.Serialization;
using RedStapler.StandardLibrary.WebFileSending;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	public abstract class AutoCompleteService: EwfPage {
		protected override FileCreator fileCreator {
			get { return new FileCreator( () => new FileToBeSent( "", ContentTypes.PlainText, new JavaScriptSerializer().Serialize( getItems() ) ) ); }
		}

		protected abstract IEnumerable<AutoCompleteItem> getItems();
	}
}