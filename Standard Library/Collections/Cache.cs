using System;
using System.Collections.Generic;
using System.Linq;

namespace RedStapler.StandardLibrary.Collections {
	/// <summary>
	/// Provides a convenient way to maintain a cache of values.
	/// </summary>
	public class Cache<Key, Value> {
		private Dictionary<Key, Value> keysToValues = new Dictionary<Key, Value>();

		/// <summary>
		/// Returns the value associated with the given key. If there is no value cached for the given key yet, the value is created and added to the cache, then returned.
		/// </summary>
		public Value GetOrAddValue( Key key, Func<Value> newValueCreator ) {
			Value value;
			if( !keysToValues.TryGetValue( key, out value ) ) {
				value = newValueCreator();
				keysToValues.Add( key, value );
			}
			return value;
		}

		/// <summary>
		/// Destroys any existing cache and prefills the cache with the given values.
		/// </summary>
		public void PreFill( IEnumerable<Value> values, Func<Value, Key> keyCreator ) {
			keysToValues = values.ToDictionary( keyCreator );
		}
	}
}