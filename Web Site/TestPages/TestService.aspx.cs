using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework;

// OptionalParameter: string term

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class TestService: AutoCompleteService {
		protected override IEnumerable<AutoCompleteItem> getItems() {
			var rand = new Random();
			return Enumerable.Range( 0, 10 ).Select( i => {
				var next = info.Term + rand.Next( 1000 );
				return new AutoCompleteItem( next, next );
			} );
		}
	}
}