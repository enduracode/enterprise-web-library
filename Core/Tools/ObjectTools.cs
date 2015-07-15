using System;
using System.Linq;

namespace EnterpriseWebLibrary {
	public static class ObjectTools {
		/// <summary>
		/// Returns o.ToString() unless o is null. In this case, returns either null (if nullToEmptyString is false) or the empty string (if nullToEmptyString is true).
		/// </summary>
		public static string ObjectToString( this object o, bool nullToEmptyString ) {
			if( o != null )
				return o.ToString();
			return nullToEmptyString ? String.Empty : null;
		}

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