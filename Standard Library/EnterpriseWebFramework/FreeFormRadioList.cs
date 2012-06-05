using System.Collections.Generic;
using System.Linq;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Declare and initialize this in your code-behind and use it to create individual radio buttons that are tied together in a group. You can arrange them on
	/// your page however you wish.
	/// </summary>
	public class FreeFormRadioList {
		private readonly Dictionary<string, CommonCheckBox> dictionary;
		private readonly string groupName;

		/// <summary>
		/// Creates a new free-form list of radio buttons.
		/// </summary>
		public FreeFormRadioList( string groupName ) {
			dictionary = new Dictionary<string, CommonCheckBox>();
			this.groupName = groupName;
		}

		/// <summary>
		/// Creates a new radio button, adds it to the list, and returns it.
		/// </summary>
		public T CreateRadioButton<T>( string value ) where T: CommonCheckBox, new() {
			var r = new T();
			AddCheckBox( value, r );
			return r;
		}

		/// <summary>
		/// Adds an existing check box, which will be converted into a radio button, to the list.
		/// </summary>
		public void AddCheckBox( string value, CommonCheckBox r ) {
			dictionary.Add( value, r );
			r.GroupName = groupName;
		}

		/// <summary>
		/// Gets the value of the selected item or selects the item that contains the specified value.
		/// </summary>
		public string SelectedValue {
			get { return ( from pair in dictionary where pair.Value.Checked select pair.Key ).FirstOrDefault(); }
			set {
				// We need to support setting the value to something that isn't present, just like every other list control.
				// Uncheck all checkboxes so that only one checkbox remains checked.
				foreach( var pair in dictionary )
					pair.Value.Checked = false;
				if( dictionary.ContainsKey( value ) )
					dictionary[ value ].Checked = true;
			}
		}

		/// <summary>
		/// Returns true if the value changed on this post back.
		/// </summary>
		public bool ValueChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return dictionary.Any( i => i.Value.ValueChangedOnPostBack( AppRequestState.Instance.EwfPageRequestState.PostBackValues ) );
		}

		/// <summary>
		/// Returns the key-value pairs in the form of &lt;string value, CommonCheckBox&gt;
		/// </summary>
		internal IEnumerable<KeyValuePair<string, CommonCheckBox>> ValuesAndRadioButtons { get { return dictionary.ToList(); } }
	}
}