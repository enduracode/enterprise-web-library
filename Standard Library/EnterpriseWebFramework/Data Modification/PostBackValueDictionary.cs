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
			// We want to suppress all of the exceptions that could happen, such as the key not existing or the value being the wrong type. If a problem occurs here
			// that matters, it's ok to suppress it because it should be caught in a more helpful way by EwfPage when it compares form control hashes.
			try {
				return (ValType)dictionary[ ( key as Control ).UniqueID ];
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