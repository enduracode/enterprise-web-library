namespace EnterpriseWebLibrary {
	/// <summary>
	/// General system-specific logic.
	/// </summary>
	public abstract class SystemGeneralProvider {
		/// <summary>
		/// Gets the display name of the system.
		/// </summary>
		protected internal virtual string SystemDisplayName => "";

		/// <summary>
		/// Gets the password used for intermediate log-in.
		/// </summary>
		protected internal abstract string IntermediateLogInPassword { get; }

		/// <summary>
		/// Gets the email default from name.
		/// </summary>
		protected internal abstract string EmailDefaultFromName { get; }

		/// <summary>
		/// Gets the email default from address.
		/// </summary>
		protected internal abstract string EmailDefaultFromAddress { get; }
	}
}