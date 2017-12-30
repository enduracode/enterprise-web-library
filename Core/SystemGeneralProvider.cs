namespace EnterpriseWebLibrary {
	/// <summary>
	/// General system-specific logic.
	/// </summary>
	public interface SystemGeneralProvider {
		/// <summary>
		/// Gets the Aspose.Pdf license name. Returns the empty string if the system doesn't have an Aspose.Pdf license.
		/// </summary>
		string AsposePdfLicenseName { get; }

		/// <summary>
		/// Gets the Aspose.Words license name. Returns the empty string if the system doesn't have an Aspose.Words license.
		/// </summary>
		string AsposeWordsLicenseName { get; }

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