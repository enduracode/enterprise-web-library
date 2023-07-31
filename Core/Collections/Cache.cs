using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace EnterpriseWebLibrary.Collections;

/// <summary>
/// A cache of values.
/// </summary>
public class Cache<KeyType, ValType> where KeyType: notnull {
	private readonly IDictionary<KeyType, ValType> dictionary;

	public Cache( bool isThreadSafe, IEqualityComparer<KeyType>? comparer = null ) {
		dictionary = isThreadSafe
			             ? comparer != null
				               ? (IDictionary<KeyType, ValType>)new ConcurrentDictionary<KeyType, ValType>( comparer )
				               : new ConcurrentDictionary<KeyType, ValType>()
			             : new Dictionary<KeyType, ValType>( comparer );
	}

	/// <summary>
	/// Gets a collection containing the keys in the cache.
	/// </summary>
	public ICollection<KeyType> Keys => dictionary.Keys;

	/// <summary>
	/// Returns true if the key was found.
	/// </summary>
	public bool ContainsKey( KeyType key ) => dictionary.ContainsKey( key );

	/// <summary>
	/// Gets the value associated with the specified key.
	/// </summary>
	public ValType this[ KeyType key ] => dictionary[ key ];

	/// <summary>
	/// Attempts to get the value associated with the specified key. Returns true if the key was found.
	/// </summary>
	public bool TryGetValue( KeyType key, [ MaybeNullWhen( false ) ] out ValType value ) => dictionary.TryGetValue( key, out value );

	/// <summary>
	/// Attempts to add the specified key and value. Returns true if the key/value pair was added.
	/// </summary>
	public bool TryAdd( KeyType key, ValType value ) {
		if( dictionary is ConcurrentDictionary<KeyType, ValType> concurrentDictionary )
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
	public ValType GetOrAdd( KeyType key, Func<ValType> newValueCreator, bool disableNewCaching = false ) {
		if( !disableNewCaching )
			if( dictionary is ConcurrentDictionary<KeyType, ValType> concurrentDictionary )
				return concurrentDictionary.GetOrAdd( key, _ => newValueCreator() );

		if( !dictionary.TryGetValue( key, out var value ) ) {
			value = newValueCreator();
			if( !disableNewCaching )
				dictionary.Add( key, value );
		}
		return value;
	}

	/// <summary>
	/// Attempts to remove the key/value pair with the specified key. Returns true if the key was found and the key/value pair was removed.
	/// </summary>
	public bool Remove( KeyType key ) => dictionary.Remove( key );
}