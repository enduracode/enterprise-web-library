using System;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement {
	/// <summary>
	/// Represents a user of the system who can be authenticated using ASP.NET Forms Authentication.
	/// </summary>
	public class FormsAuthCapableUser: User {
		private readonly int userId;
		private readonly string email;
		private readonly Role role;
		private readonly DateTime? lastRequestDateTime;
		private readonly int salt;
		private readonly string saltedPassword;
		private readonly bool mustChangePassword;
		private readonly string friendlyName;

		/// <summary>
		/// Creates a user object. FriendlyName defaults to the empty string. Do not pass null.
		/// </summary>
		public FormsAuthCapableUser( int userId, string email, Role role, DateTime? lastRequestDateTime, int salt, string saltedPassword, bool mustChangePassword,
		                             string friendlyName = "" ) {
			this.userId = userId;
			this.email = email;
			this.role = role;
			this.lastRequestDateTime = lastRequestDateTime;
			this.salt = salt;
			this.saltedPassword = saltedPassword;
			this.mustChangePassword = mustChangePassword;
			this.friendlyName = friendlyName;
		}

		/// <summary>
		/// The ID of the user.
		/// </summary>
		public int UserId { get { return userId; } }

		/// <summary>
		/// The email address of the user.
		/// </summary>
		public string Email { get { return email; } }

		/// <summary>
		/// The role of the user.
		/// </summary>
		public Role Role { get { return role; } }

		/// <summary>
		/// The last time the user made a request to the system.
		/// </summary>
		public DateTime? LastRequestDateTime { get { return lastRequestDateTime; } }

		/// <summary>
		/// The salt for this user.
		/// </summary>
		public int Salt { get { return salt; } }

		/// <summary>
		/// The salted password for this user.
		/// </summary>
		public string SaltedPassword { get { return saltedPassword; } }

		/// <summary>
		/// Returns true if this user must change their password at next logon.
		/// </summary>
		public bool MustChangePassword { get { return mustChangePassword; } }

		/// <summary>
		/// The real-world name of the user ("Greg Smalter"). May be the empty string.
		/// </summary>
		public string FriendlyName { get { return friendlyName; } }
	}
}