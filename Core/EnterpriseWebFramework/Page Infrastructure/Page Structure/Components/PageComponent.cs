#nullable disable
using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public interface PageComponent {}

	public static class PageComponentExtensionCreators {
		/// <summary>
		/// Concatenates page components.
		/// </summary>
		public static IEnumerable<ComponentType> Concat<ComponentType>( this ComponentType first, IEnumerable<ComponentType> second )
			where ComponentType: PageComponent =>
			second.Prepend( first );

		/// <summary>
		/// Returns a sequence of two page components.
		/// </summary>
		public static IEnumerable<ComponentType> Append<ComponentType>( this ComponentType first, ComponentType second ) where ComponentType: PageComponent =>
			Enumerable.Empty<ComponentType>().Append( first ).Append( second );
	}
}