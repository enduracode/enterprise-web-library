using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A drop down list or radio button list that allows exactly one item to be selected. Do not use this control in markup.
	/// NOTE: This control is experimental. It will eventually replace EwfListControl.
	/// </summary>
	public class SelectList<ValType>: WebControl, ControlTreeDataLoader, FormControl<ValType> {
		private readonly ValType durableValue;
		//private bool valueSet;

		// NOTE: Enforce in constructor that there is at least one item in the list?

		// NOTE: Should we support having at least one item in the list *and* not having a value? Customers have sometimes asked for radio button lists with no default selection.

		// NOTE: If a value is passed to the constructor, it must correspond to an item in the list. We don't want to continue the EwfListControl practice of ignorning an invalid selected value, because it leads to confusion about when ValueChangedOnPostBack should return true. If the user doesn't touch the control, ValueChangedOnPostBack should always return false.

		// NOTE: Should it be possible to *not* pass an initial selected value to the constructor?
		// NOTE: This might help with "add new entity" page drop downs that don't have a blank item at the top and instead start out with the first real item selected; it would be a pain to have to pass a selected value in this case.
		// NOTE: Another thing we could do is force there to always be a blank (null) item at the top of the list; "add new entity" page client code could then just pass null as the selected value. How would this work with radio button lists?

		// NOTE: Should we support multiple items with the same value? This might be useful for dividing lines in long drop down lists, or for listing the same logical item multiple times, e.g. listing "The Beatles" twice in an alphabetical list, with the Bs and also with the Ts.
		// NOTE: One problem with this is what to do when the initial selected value corresponds to multiple items. Which one gets selected?

		// NOTE: Use a separate control (EwfTextBox?) to support custom text scenarios. One essential part of this control is that each item has both a name and a value, and when supporting custom text there is really no such thing as a separate value corresponding to each name.

		// NOTE: Don't support change events. Instead, allow a post back event on this control that forces the control into auto post back mode.

		// NOTE: Consider using something like http://harvesthq.github.com/chosen/ or http://jamielottering.github.com/DropKick/ to make drop down lists better.

		// NOTE: Remember to use the EwfListItem class for item adding. Also, would it ever make sense to use ChangeBasedListItem?

		/// <summary>
		/// Creates a select list.
		/// </summary>
		public SelectList( ValType value ) {
			durableValue = value;
		}

		ValType FormControl<ValType>.DurableValue { get { return durableValue; } }
		string FormControl.DurableValueAsString { get { return durableValue.ToString(); } }

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {}

		void FormControl.AddPostBackValueToDictionary( PostBackValueDictionary postBackValues ) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the post back value.
		/// </summary>
		public ValType GetPostBackValue( PostBackValueDictionary postBackValues ) {
			return postBackValues.GetValue( this );
		}

		/// <summary>
		/// Returns true if the value changed on this post back.
		/// </summary>
		public bool ValueChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return postBackValues.ValueChangedOnPostBack( this );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Select; } }
	}
}