using System;
using System.Collections.Generic;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A dictionary of form control values from a post back.
	/// </summary>
	public class PostBackValueDictionary {
		private readonly Dictionary<string, object> dictionary;

		internal PostBackValueDictionary( Dictionary<string, object> dictionary ) {
			this.dictionary = dictionary;
		}

		/// <summary>
		/// Returns true if extra post-back values exist.
		/// </summary>
		/// <param name="keys"></param>
		/// <param name="keyPredicate"></param>
		/// <param name="valueSelector">Do not return null from this method.</param>
		internal bool AddFromRequest( IEnumerable<string> keys, Func<string, bool> keyPredicate, Func<string, object> valueSelector ) {
			var extraPostBackValuesExist = false;
			foreach( var key in keys ) {
				if( keyPredicate( key ) && !dictionary.ContainsKey( key ) )
					dictionary.Add( key, valueSelector( key ) );
				else
					extraPostBackValuesExist = true;
			}
			return extraPostBackValuesExist;
		}

		/// <summary>
		/// Returns null if there is no value for the specified key.
		/// </summary>
		internal object GetValue( string key ) {
			object value;
			return dictionary.TryGetValue( key, out value ) ? value : null;
		}
	}
}