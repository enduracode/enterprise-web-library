namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Used to define common attributes and methods among check boxes. 
	/// </summary>
	public interface CommonCheckBox {
		/// <summary>
		/// Gets or sets the name of the group that this check box belongs to.
		/// </summary>
		string GroupName { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the check box is checked.
		/// </summary>
		bool Checked { get; set; }

		/// <summary>
		/// Gets or sets the label associated with the check box.
		/// </summary>
		string Text { get; set; }

		/// <summary>
		/// Method to insert a Javascript onclick event.
		/// </summary>
		void AddOnClickJsMethod( string s );

		/// <summary>
		/// Returns true if the value changed on this post back.
		/// </summary>
		bool ValueChangedOnPostBack( PostBackValueDictionary postBackValues );
	}
}