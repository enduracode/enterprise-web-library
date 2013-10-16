using System;
using System.Linq;

namespace RedStapler.StandardLibrary {
	public static class AttributeTools {
		/// <summary>
		/// Returns the attribute for the given object if it's available. Otherwise, returns null.
		/// </summary>
		public static T GetAttribute<T>( this object e ) where T: Attribute {
			var memberInfo = e.GetType().GetMember( e.ToString() );
			if( memberInfo.Any() ) {
				var attributes = memberInfo[ 0 ].GetCustomAttributes( typeof( T ), false );
				if( attributes.Any() )
					return attributes[ 0 ] as T;
			}
			return null;
		}
	}
}