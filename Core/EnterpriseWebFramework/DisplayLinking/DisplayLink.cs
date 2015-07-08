namespace RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayLinking {
	/// <summary>
	/// A mapping that uses JavaScript and CSS to allow one control to affect the display of other controls.
	/// </summary>
	internal interface DisplayLink {
		void AddJavaScript();
		void SetInitialDisplay( PostBackValueDictionary formControlValues );
	}
}