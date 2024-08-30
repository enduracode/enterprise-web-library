using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework;

// EwlAutoCompleteService
// OptionalParameter: string term

namespace EnterpriseWebLibrary.Website.WebFrameworkDemo {
	partial class TestService {
		protected override IEnumerable<AutoCompleteItem> getItems() {
			var rand = new Random();
			return Enumerable.Range( 0, 10 )
				.Select(
					i => {
						var next = Term + rand.Next( 1000 );
						return new AutoCompleteItem( next, next );
					} );
		}
	}
}

namespace EnterpriseWebLibrary.Website.WebFrameworkDemo {
	partial class TestService {
		protected override UrlHandler getUrlParent() => new LegacyUrlFolderSetup();
	}
}