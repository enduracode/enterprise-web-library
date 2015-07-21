using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement {
	/// <summary>
	/// Represents a user of the system who must be authenticated externally.
	/// </summary>
	public class ExternalAuthUser: User {
		private readonly int userId;
		private readonly string email;
		private readonly DateTime? lastRequestDateTime;
		private readonly Role role;
		private readonly string friendlyName;

		/// <summary>
		/// Creates a user object. FriendlyName defaults to the empty string. Do not pass null.
		/// </summary>
		public ExternalAuthUser( int userId, string email, Role role, DateTime? lastRequestDateTime, string friendlyName = "" ) {
			this.userId = userId;
			this.email = email;
			this.role = role;
			this.lastRequestDateTime = lastRequestDateTime;
			this.friendlyName = friendlyName;
		}

		/// <summary>
		/// The ID of the user.
		/// </summary>
		public int UserId { get { return userId; } }

		/// <summary>
		/// The email address of the user.
		/// </summary>
		string User.Email { get { return email; } }

		/// <summary>
		/// The role of the user.
		/// </summary>
		public Role Role { get { return role; } }

		/// <summary>
		/// The last time the user made a request to the system.
		/// </summary>
		public DateTime? LastRequestDateTime { get { return lastRequestDateTime; } }

		/// <summary>
		/// The real-world name of the user ("Greg Smalter"). May be the empty string.
		/// </summary>
		public string FriendlyName { get { return friendlyName; } }
	}
}