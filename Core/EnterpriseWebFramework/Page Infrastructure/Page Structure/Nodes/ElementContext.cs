using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class ElementContext {
		public readonly string Id;

		internal ElementContext( string id ) {
			Id = id;
		}

		internal IEnumerable<string> AddModificationErrorDisplayAndGetErrors( string keySuffix, EwfValidation validation ) {
			throw new ApplicationException(
				"not yet implemented; will use same logic as corresponding EwfPage method; can become extension method if this preserves the dependency graph" );
		}
	}
}