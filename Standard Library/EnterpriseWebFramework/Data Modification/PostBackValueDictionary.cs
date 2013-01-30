using System.Collections.Generic;
using System.Web.UI;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A dictionary of form control values from a post back.
	/// </summary>
	public class PostBackValueDictionary {
		private readonly Dictionary<string, object> dictionary;

		internal PostBackValueDictionary( Dictionary<string, object> dictionary ) {
			this.dictionary = dictionary;
		}

		internal void Add<ValType>( FormControl<ValType> key, ValType value ) {
			dictionary.Add( ( key as Control ).UniqueID, value );
		}

		internal ValType GetValue<ValType>( FormControl<ValType> key ) {
			// We want to ignore all of the problems that could happen, such as the key not existing or the value being the wrong type. Any problem that matters
			// should be caught in a more helpful way by EwfPage when it compares form control hashes.
			//
			// Avoid using exceptions here if possible. This method is sometimes called many times during a request, and we've seen exceptions take as long as 50 ms
			// each when debugging.

			object value;
			if( !dictionary.TryGetValue( ( key as Control ).UniqueID, out value ) )
				return key.DurableValue;

			// It would be nice to figure out a way to do this without using exceptions.
			try {
				return (ValType)value;
			}
			catch {
				return key.DurableValue;
			}
		}

		internal bool ValueChangedOnPostBack<ValType>( FormControl<ValType> key ) {
			return !StandardLibraryMethods.AreEqual( GetValue( key ), key.DurableValue );
		}
	}
}