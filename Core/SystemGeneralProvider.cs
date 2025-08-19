using System.Security.Cryptography;
using NodaTime;

namespace EnterpriseWebLibrary;

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

	/// <summary>
	/// Returns the best-effort data hasher for the specified date, or null for the library’s built-in hasher.
	/// </summary>
	protected internal virtual HMAC? GetBestEffortDataHasher( LocalDate date ) => null;

	/// <summary>
	/// Gets whether Imageflow, used for image resizing, is licensed.
	/// </summary>
	protected internal virtual bool ImageflowLicensed => false;
}