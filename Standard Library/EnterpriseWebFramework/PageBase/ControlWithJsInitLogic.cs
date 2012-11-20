namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A control with JavaScript that should execute when the jQuery document ready event is fired.
	/// </summary>
	public interface ControlWithJsInitLogic {
		/// <summary>
		/// Gets the JavaScript statements that should be executed when the jQuery document ready event is fired.
		/// </summary>
		string GetJsInitStatements();
	}
}