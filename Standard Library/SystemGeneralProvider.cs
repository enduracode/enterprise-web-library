namespace RedStapler.StandardLibrary {
	/// <summary>
	/// General system-specific logic.
	/// </summary>
	public interface SystemGeneralProvider {
		/// <summary>
		/// Gets the Aspose license name. Returns the empty string if the system doesn't have an Aspose license.
		/// </summary>
		string AsposeLicenseName { get; }

		/// <summary>
		/// Password used for intermediate log in and some other things.
		/// </summary>
		string IntermediateLogInPassword { get; }

		/// <summary>
		/// Email address used for sanitized systems that use forms authentication.
		/// </summary>
		string FormsLogInEmail { get; }

		/// <summary>
		/// Password used for sanitized systems that use forms authentication.
		/// </summary>
		string FormsLogInPassword { get; }

		/// <summary>
		/// Not yet documented.
		/// </summary>
		System.Net.Mail.SmtpClient CreateClientSideAppSmtpClient();
	}
}