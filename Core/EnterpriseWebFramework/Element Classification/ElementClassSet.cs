using System.Collections.Immutable;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class ElementClassSet {
		public static readonly ElementClassSet Empty = new ElementClassSet( ImmutableHashSet<string>.Empty );

		public static implicit operator ElementClassSet( ElementClass elementClass ) {
			return new ElementClassSet( elementClass.ClassName.ToCollection().ToImmutableHashSet() );
		}

		private readonly IImmutableSet<string> classNames;

		private ElementClassSet( IImmutableSet<string> classNames ) {
			this.classNames = classNames;
		}

		/// <summary>
		/// Adds element classes to this set.
		/// </summary>
		public ElementClassSet Union( ElementClassSet classSet ) {
			return new ElementClassSet( classNames.Union( classSet.classNames ) );
		}

		internal IImmutableSet<string> ClassNames => classNames;
	}

	public static class ElementClassSetExtensionCreators {
		/// <summary>
		/// Adds element classes to this class.
		/// </summary>
		public static ElementClassSet Union( this ElementClass elementClass, ElementClassSet classSet ) {
			return ( (ElementClassSet)elementClass ).Union( classSet );
		}
	}
}