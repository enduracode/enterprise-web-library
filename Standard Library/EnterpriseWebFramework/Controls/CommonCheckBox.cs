namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Used to define common attributes and methods among check boxes.
	/// </summary>
	public interface CommonCheckBox {
		bool IsChecked { get; }

		/// <summary>
		/// Gets the name of the group that this check box belongs to.
		/// </summary>
		string GroupName { get; }

		/// <summary>
		/// Method to insert a Javascript onclick event.
		/// </summary>
		void AddOnClickJsMethod( string s );

		/// <summary>
		/// Gets whether the box is checked in the post back.
		/// </summary>
		bool IsCheckedInPostBack( PostBackValueDictionary postBackValues );

		/// <summary>
		/// Returns true if the value changed on this post back.
		/// </summary>
		bool ValueChangedOnPostBack( PostBackValueDictionary postBackValues );
	}
}