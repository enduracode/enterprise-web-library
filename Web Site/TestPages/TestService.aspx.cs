using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.WebFileSending;

// OptionalParameter: string term

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class TestService: EwfPage {
		protected override void loadData() {}

		protected override bool sendsFileInline { get { return true; } }

		protected override FileCreator fileCreator {
			get {
				return new FileCreator( stream => {
					using( var writer = new StreamWriter( stream, new UTF8Encoding() ) ) {
						var rand = new Random();
						writer.Write( new JavaScriptSerializer().Serialize( Enumerable.Range( 0, 10 ).Select( i => {
							var next = info.Term + rand.Next( 1000 );
							return new { label = next, value = next };
						} ).ToArray() ) );
					}
					return new FileInfoToBeSent( "", "text/plain; charset=utf-8" );
				} );
			}
		}
	}
}