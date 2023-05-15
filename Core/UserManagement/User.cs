using NodaTime;

namespace EnterpriseWebLibrary.UserManagement;

/// <summary>
/// A user of the system.
/// </summary>
public class User {
	private static Func<User> currentUserGetter;

	internal static void Init( Func<User> currentUserGetter ) {
		User.currentUserGetter = currentUserGetter;
	}

	/// <summary>
	/// Gets the current authenticated user, or null if the user has not been authenticated.
	///
	/// In a web application, do not use from the specifyParameterDefaults or init methods of resource and entity-setup classes because the framework has not yet
	/// been able to correct the connection security of the request, if necessary, and because parent authorization logic has not yet executed. To use from
	/// createParent--which you should only do if, for a given set of parameters, there is no single parent that all users can access--you must explicitly specify
	/// the connection security as SecureIfPossible in the current item. With this use, keep in mind that no parent authorization logic has executed and therefore
	/// you cannot assume anything about the user.
	///
	/// Does not currently work outside of web applications.
	/// </summary>
	public static User Current => currentUserGetter();

	private readonly int userId;
	private readonly string email;
	private readonly Role role;
	private readonly Instant? lastRequestTime;
	private readonly string friendlyName;

	/// <summary>
	/// Creates a user object. FriendlyName defaults to the empty string. Do not pass null.
	/// </summary>
	public User( int userId, string email, Role role, Instant? lastRequestTime, string friendlyName = "" ) {
		this.userId = userId;
		this.email = email;
		this.role = role;
		this.lastRequestTime = lastRequestTime;
		this.friendlyName = friendlyName;
	}

	/// <summary>
	/// The ID of the user.
	/// </summary>
	public int UserId => userId;

	/// <summary>
	/// The email address of the user.
	/// </summary>
	public string Email => email;

	/// <summary>
	/// The role of the user.
	/// </summary>
	public Role Role => role;

	/// <summary>
	/// The last time the user made a request to the system.
	/// </summary>
	public Instant? LastRequestTime => lastRequestTime;

	/// <summary>
	/// The real-world name of the user ("Greg Smalter"). May be the empty string.
	/// </summary>
	public string FriendlyName => friendlyName;
}