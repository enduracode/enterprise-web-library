using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using RedStapler.StandardLibrary.EnterpriseWebFramework.WebService;
using RedStapler.StandardLibrary.WebFileSending;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	public abstract class AutoFillService: EwfPage {
		protected override FileCreator fileCreator {
			get {
				return new FileCreator( stream => {
					using( var writer = new StreamWriter( stream, new UTF8Encoding() ) )
						writer.Write( new JavaScriptSerializer().Serialize( getAutoFillItems() ) );
					return new FileInfoToBeSent( "", "text/plain; charset=utf-8" );
				} );
			}
		}

		protected abstract IEnumerable<AutoFillItem> getAutoFillItems();
	}
}