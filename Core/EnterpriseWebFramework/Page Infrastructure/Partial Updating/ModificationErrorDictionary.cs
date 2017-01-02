using System.Collections.Generic;
using System.Collections.Immutable;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class ModificationErrorDictionary {
		private readonly ImmutableDictionary<EwfValidation, IReadOnlyCollection<string>> dictionary;

		internal ModificationErrorDictionary( ImmutableDictionary<EwfValidation, IReadOnlyCollection<string>> dictionary ) {
			this.dictionary = dictionary;
		}

		/// <summary>
		/// Returns the errors from the specified validation.
		/// </summary>
		public IReadOnlyCollection<string> GetErrors( EwfValidation validation ) {
			return dictionary[ validation ];
		}
	}
}