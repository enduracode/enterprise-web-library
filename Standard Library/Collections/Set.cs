using System.Collections;

namespace RedStapler.StandardLibrary.Collections {
	/// <summary>
	/// A set collection.  Does not allow duplicates.
	/// </summary>
	public class Set: ICollection {
		private readonly Hashtable hashTable;

		/// <summary>
		/// Not yet documented.
		/// </summary>
		public int Count { get { return hashTable.Count; } }

		/// <summary>
		/// Not yet documented.
		/// </summary>
		public ICollection Elements { get { return hashTable.Keys; } }

		/// <summary>
		/// Not yet documented.
		/// </summary>
		public object SyncRoot { get { return hashTable.SyncRoot; } }

		/// <summary>
		/// Not yet documented.
		/// </summary>
		public bool IsSynchronized { get { return false; } }

		/// <summary>
		/// Not yet documented.
		/// </summary>
		public Set() {
			hashTable = new Hashtable();
		}

		/// <summary>
		/// Not yet documented.
		/// </summary>
		public Set( ICollection collection ) {
			hashTable = new Hashtable();
			AddRange( collection );
		}

		/// <summary>
		/// Returns true if the given set is a subset
		/// of this set.
		/// </summary>
		public bool IsSubset( Set setToCompare ) {
			foreach( object item in setToCompare.Elements ) {
				if( !Contains( item ) )
					return false;
			}
			return true;
		}

		/// <summary>
		/// Add an item to the set, duplicates ignored.
		/// </summary>
		public void Add( object item ) {
			hashTable[ item ] = null;
		}

		/// <summary>
		/// Add an item to the set, exception is thrown if the
		/// item already exists.
		/// </summary>
		public void AddComplainAboutDuplicates( object item ) {
			hashTable.Add( item, null );
		}

		/// <summary>
		/// Add a collection of items to the set, duplicates ignored.
		/// </summary>
		public void AddRange( ICollection collection ) {
			foreach( object item in collection )
				Add( item );
		}

		/// <summary>
		/// Add a collection of items to the set, exception is thrown if
		/// any of the items already exist.
		/// </summary>
		public void AddRangeComplainAboutDuplicates( ICollection collection ) {
			foreach( object item in collection )
				AddComplainAboutDuplicates( item );
		}

		/// <summary>
		/// Not yet documented.
		/// </summary>
		public void Remove( object item ) {
			hashTable.Remove( item );
		}

		/// <summary>
		/// Not yet documented.
		/// </summary>
		public bool Contains( object item ) {
			return hashTable.Contains( item );
		}

		/// <summary>
		/// Not yet documented.
		/// </summary>
		public IEnumerator GetEnumerator() {
			return hashTable.Keys.GetEnumerator();
		}

		/// <summary>
		/// Not yet documented.
		/// </summary>
		public void CopyTo( System.Array array, int size ) {
			ArrayList elements = new ArrayList( hashTable.Keys );

			int cnt = 0;
			while( cnt < size ) {
				array.SetValue( elements[ cnt ], cnt );
				cnt++;
			}
		}
	}
}