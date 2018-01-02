namespace EnterpriseWebLibrary {
	/// <summary>
	/// General system-specific logic.
	/// </summary>
	public interface SystemGeneralProvider {
		/// <summary>
		/// Gets the password used for intermediate log-in.
		/// </summary>
		string IntermediateLogInPassword { get; }

		/// <summary>
		/// Gets the email default from name.
		/// </summary>
		string EmailDefaultFromName { get; }

		/// <summary>
		/// Gets the email default from address.
		/// </summary>
		string EmailDefaultFromAddress { get; }
	}
}