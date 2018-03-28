using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A display style for modification errors.
	/// </summary>
	public interface ErrorDisplayStyle<out ComponentType> where ComponentType: PageComponent {
		/// <summary>
		/// EWL use only.
		/// </summary>
		IReadOnlyCollection<ComponentType> GetComponents( IEnumerable<string> errors );
	}
}