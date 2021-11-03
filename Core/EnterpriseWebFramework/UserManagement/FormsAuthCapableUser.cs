using EnterpriseWebLibrary.UserManagement;
using NodaTime;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement {
	/// <summary>
	/// Represents a user of the system who can be authenticated using ASP.NET Forms Authentication.
	/// </summary>
	public class FormsAuthCapableUser: User {
		private readonly int userId;
		private readonly string email;
		private readonly Role role;
		private readonly Instant? lastRequestTime;
		private readonly int salt;
		private readonly byte[] saltedPassword;
		private readonly bool mustChangePassword;
		private readonly string friendlyName;

		/// <summary>
		/// Creates a user object. FriendlyName defaults to the empty string. Do not pass null.
		/// </summary>
		public FormsAuthCapableUser(
			int userId, string email, Role role, Instant? lastRequestTime, int salt, byte[] saltedPassword, bool mustChangePassword, string friendlyName = "" ) {
			this.userId = userId;
			this.email = email;
			this.role = role;
			this.lastRequestTime = lastRequestTime;
			this.salt = salt;
			this.saltedPassword = saltedPassword;
			this.mustChangePassword = mustChangePassword;
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
		/// The salt for this user.
		/// </summary>
		public int Salt => salt;

		/// <summary>
		/// The salted password for this user.
		/// </summary>
		public byte[] SaltedPassword => saltedPassword;

		/// <summary>
		/// Returns true if this user must change their password at next logon.
		/// </summary>
		public bool MustChangePassword => mustChangePassword;

		/// <summary>
		/// The real-world name of the user ("Greg Smalter"). May be the empty string.
		/// </summary>
		public string FriendlyName => friendlyName;
	}
}