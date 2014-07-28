using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RedStapler.StandardLibrary.Collections {
	/// <summary>
	/// A cache of values.
	/// </summary>
	public class Cache<KeyType, ValType> {
		private IDictionary<KeyType, ValType> dictionary;
		private readonly IEqualityComparer<KeyType> comparer;

		public Cache( bool isThreadSafe, IEqualityComparer<KeyType> comparer = null ) {
			dictionary = isThreadSafe
				             ? (IDictionary<KeyType, ValType>)new ConcurrentDictionary<KeyType, ValType>( comparer )
				             : new Dictionary<KeyType, ValType>( comparer );
			this.comparer = comparer;
		}

		/// <summary>
		/// Attempts to get the value associated with the specified key. Returns true if the key was found.
		/// </summary>
		public bool TryGetValue( KeyType key, out ValType value ) {
			return dictionary.TryGetValue( key, out value );
		}

		/// <summary>
		/// Attempts to add the specified key and value. Returns true if the key/value pair was added.
		/// </summary>
		public bool TryAdd( KeyType key, ValType value ) {
			var concurrentDictionary = dictionary as ConcurrentDictionary<KeyType, ValType>;
			if( concurrentDictionary != null )
				return concurrentDictionary.TryAdd( key, value );

			if( dictionary.ContainsKey( key ) )
				return false;
			dictionary.Add( key, value );
			return true;
		}

		/// <summary>
		/// Returns the value associated with the given key. If there is no value cached for the given key yet, the value is created and added to the cache, then
		/// returned.
		/// </summary>
		public ValType GetOrAdd( KeyType key, Func<ValType> newValueCreator ) {
			var concurrentDictionary = dictionary as ConcurrentDictionary<KeyType, ValType>;
			if( concurrentDictionary != null )
				return concurrentDictionary.GetOrAdd( key, k => newValueCreator() );

			ValType value;
			if( !dictionary.TryGetValue( key, out value ) ) {
				value = newValueCreator();
				dictionary.Add( key, value );
			}
			return value;
		}

		[ Obsolete( "Guaranteed through 31 October 2014. Contact the EWL team if you are using this method." ) ]
		// After removing this, make the dictionary field readonly.
		public void PreFill( IEnumerable<ValType> values, Func<ValType, KeyType> keyCreator ) {
			if( dictionary is ConcurrentDictionary<KeyType, ValType> )
				dictionary = new ConcurrentDictionary<KeyType, ValType>( from i in values select new KeyValuePair<KeyType, ValType>( keyCreator( i ), i ), comparer );
			else
				dictionary = values.ToDictionary( keyCreator, comparer );
		}
	}
}