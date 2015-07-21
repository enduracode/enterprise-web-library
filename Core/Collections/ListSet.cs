using System;
using System.Collections;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.Collections {
	/// <summary>
	/// A collection that does not allow duplicates, and keeps items in the order they are added.
	/// </summary>
	public class ListSet<T>: ICollection<T> {
		private readonly List<T> list = new List<T>();

		/// <summary>
		/// Returns the number of items in the set.
		/// </summary>
		int ICollection<T>.Count { get { return list.Count; } }

		/// <summary>
		/// Returns false.
		/// </summary>
		bool ICollection<T>.IsReadOnly { get { return false; } }

		/// <summary>
		/// Add an item to the set, duplicates ignored.
		/// </summary>
		public void Add( T item ) {
			if( !list.Contains( item ) )
				list.Add( item );
		}

		/// <summary>
		/// Add an item to the set, exception is thrown if the
		/// item already exists.
		/// </summary>
		public void AddComplainAboutDuplicates( T item ) {
			if( list.Contains( item ) )
				throw new ApplicationException( "Item already exists in set." );
			Add( item );
		}

		/// <summary>
		/// Add a collection of items to the set, duplicates ignored.
		/// </summary>
		public void AddRange( IEnumerable<T> collection ) {
			foreach( var item in collection )
				Add( item );
		}

		/// <summary>
		/// Add a collection of items to the set, exception is thrown if
		/// any of the items already exist.
		/// </summary>
		public void AddRangeComplainAboutDuplicates( IEnumerable<T> collection ) {
			foreach( var item in collection )
				AddComplainAboutDuplicates( item );
		}

		/// <summary>
		/// Returns true if item was removed.
		/// </summary>
		public bool Remove( T item ) {
			return list.Remove( item );
		}

		/// <summary>
		/// Removes all items from the set.
		/// </summary>
		void ICollection<T>.Clear() {
			list.Clear();
		}

		/// <summary>
		/// Returns true if the set contains the given item.
		/// </summary>
		bool ICollection<T>.Contains( T item ) {
			return list.Contains( item );
		}

		/// <summary>
		/// Returns the enumerator.
		/// </summary>
		IEnumerator<T> IEnumerable<T>.GetEnumerator() {
			return list.GetEnumerator();
		}

		/// <summary>
		/// Returns the enumerator.
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator() {
			return list.GetEnumerator();
		}

		/// <summary>
		/// Copies items to the given array.
		/// </summary>
		void ICollection<T>.CopyTo( T[] array, int arrayIndex ) {
			list.CopyTo( array, arrayIndex );
		}

		/// <summary>
		/// Returns the set represented as a typed array.
		/// </summary>
		public T[] ToArray() {
			return list.ToArray();
		}

		/// <summary>
		/// Sorts this list set alphabetically (ascending) based on the ToString value for each element.
		/// </summary>
		public void SortAlphabetically() {
			list.SortAlphabetically();
		}
	}
}