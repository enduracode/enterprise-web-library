using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The state for a page in an EWF application.
	/// </summary>
	public class PageState {
		// We use hash table because it is one of the view state optimized types.
		private readonly Hashtable customState;

		// These should be scoped within EwfPage but are here for convenience.
		private int uniqueControlIdForPageStateCounter;
		private readonly Dictionary<Control, string> controlsToUniqueControlIdentifiersForPageState = new Dictionary<Control, string>();

		internal static PageState CreateForNewPage() {
			return new PageState( new Hashtable() );
		}

		private PageState( Hashtable customState ) {
			this.customState = customState;
		}


		// view state serialization

		internal static Tuple<PageState, object[]> CreateFromViewState( object[] viewStateArray ) {
			return Tuple.Create( new PageState( (Hashtable)viewStateArray[ 0 ] ), viewStateArray.Skip( 1 ).ToArray() );
		}

		internal object[] GetViewStateArray( object[] additionalElements ) {
			return new object[] { customState }.Concat( additionalElements ).ToArray();
		}


		internal void ClearCustomStateControlKeys() {
			uniqueControlIdForPageStateCounter = 0;
			controlsToUniqueControlIdentifiersForPageState.Clear();
		}

		/// <summary>
		/// Framework use only. Returns the value corresponding to the specified key if it has been set. Otherwise, returns the specified default value.
		/// </summary>
		public TValue GetValue<TValue>( Control control, string key, TValue defaultValue ) {
			var uniquePageStateIdentifier = getPageStateKey( control, key );
			if( customState.ContainsKey( uniquePageStateIdentifier ) )
				return (TValue)customState[ uniquePageStateIdentifier ];
			return defaultValue;
		}

		/// <summary>
		/// Framework use only. Sets the value corresponding to the specified key to the specified value.
		/// </summary>
		public void SetValue( Control control, string key, object value ) {
			customState[ getPageStateKey( control, key ) ] = value;
		}

		/// <summary>
		/// Framework use only. Clears the value corresponding to the specified key.
		/// </summary>
		public void ClearValue( Control control, string key ) {
			customState.Remove( getPageStateKey( control, key ) );
		}

		private string getPageStateKey( Control control, string key ) {
			// If this control does not already have a unique control ID for page state, create one
			string uniqueControlIdForPageState;
			if( !controlsToUniqueControlIdentifiersForPageState.TryGetValue( control, out uniqueControlIdForPageState ) ) {
				uniqueControlIdForPageState = uniqueControlIdForPageStateCounter++.ToString();
				controlsToUniqueControlIdentifiersForPageState.Add( control, uniqueControlIdForPageState );
			}

			// Return the unique page state ID
			return uniqueControlIdForPageState + "-" + key;
		}
	}
}