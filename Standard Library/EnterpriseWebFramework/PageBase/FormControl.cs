namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A form control.
	/// </summary>
	internal interface FormControl {
		string DurableValueAsString { get; }
		void AddPostBackValueToDictionary( PostBackValueDictionary postBackValues );
		bool ValueChangedOnPostBack( PostBackValueDictionary postBackValues );
	}

	/// <summary>
	/// A form control with a value of the specified type.
	/// </summary>
	internal interface FormControl<out ValueType>: FormControl {
		ValueType DurableValue { get; }
	}
}