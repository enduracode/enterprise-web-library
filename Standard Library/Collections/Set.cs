using System;
using System.Collections;

namespace RedStapler.StandardLibrary.Collections {
	[ Obsolete( "Guaranteed through 31 October 2013. Please use System.Collections.Generic.HashSet instead." ) ]
	public class Set: ICollection {
		private readonly Hashtable hashTable;

		[ Obsolete( "Guaranteed through 31 October 2013. Please use System.Collections.Generic.HashSet instead." ) ]
		public int Count { get { return hashTable.Count; } }

		[ Obsolete( "Guaranteed through 31 October 2013. Please use System.Collections.Generic.HashSet instead." ) ]
		public ICollection Elements { get { return hashTable.Keys; } }

		[ Obsolete( "Guaranteed through 31 October 2013. Please use System.Collections.Generic.HashSet instead." ) ]
		public object SyncRoot { get { return hashTable.SyncRoot; } }

		[ Obsolete( "Guaranteed through 31 October 2013. Please use System.Collections.Generic.HashSet instead." ) ]
		public bool IsSynchronized { get { return false; } }

		[ Obsolete( "Guaranteed through 31 October 2013. Please use System.Collections.Generic.HashSet instead." ) ]
		public Set() {
			hashTable = new Hashtable();
		}

		[ Obsolete( "Guaranteed through 31 October 2013. Please use System.Collections.Generic.HashSet instead." ) ]
		public Set( ICollection collection ) {
			hashTable = new Hashtable();
			AddRange( collection );
		}

		[ Obsolete( "Guaranteed through 31 October 2013. Please use System.Collections.Generic.HashSet instead." ) ]
		public bool IsSubset( Set setToCompare ) {
			foreach( var item in setToCompare.Elements ) {
				if( !Contains( item ) )
					return false;
			}
			return true;
		}

		[ Obsolete( "Guaranteed through 31 October 2013. Please use System.Collections.Generic.HashSet instead." ) ]
		public void Add( object item ) {
			hashTable[ item ] = null;
		}

		[ Obsolete( "Guaranteed through 31 October 2013. Please use System.Collections.Generic.HashSet instead." ) ]
		public void AddComplainAboutDuplicates( object item ) {
			hashTable.Add( item, null );
		}

		[ Obsolete( "Guaranteed through 31 October 2013. Please use System.Collections.Generic.HashSet instead." ) ]
		public void AddRange( ICollection collection ) {
			foreach( var item in collection )
				Add( item );
		}

		[ Obsolete( "Guaranteed through 31 October 2013. Please use System.Collections.Generic.HashSet instead." ) ]
		public void AddRangeComplainAboutDuplicates( ICollection collection ) {
			foreach( var item in collection )
				AddComplainAboutDuplicates( item );
		}

		[ Obsolete( "Guaranteed through 31 October 2013. Please use System.Collections.Generic.HashSet instead." ) ]
		public void Remove( object item ) {
			hashTable.Remove( item );
		}

		[ Obsolete( "Guaranteed through 31 October 2013. Please use System.Collections.Generic.HashSet instead." ) ]
		public bool Contains( object item ) {
			return hashTable.Contains( item );
		}

		[ Obsolete( "Guaranteed through 31 October 2013. Please use System.Collections.Generic.HashSet instead." ) ]
		public IEnumerator GetEnumerator() {
			return hashTable.Keys.GetEnumerator();
		}

		[ Obsolete( "Guaranteed through 31 October 2013. Please use System.Collections.Generic.HashSet instead." ) ]
		public void CopyTo( Array array, int size ) {
			var elements = new ArrayList( hashTable.Keys );

			var cnt = 0;
			while( cnt < size ) {
				array.SetValue( elements[ cnt ], cnt );
				cnt++;
			}
		}
	}
}