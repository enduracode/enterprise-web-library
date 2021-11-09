using NodaTime;

namespace EnterpriseWebLibrary.UserManagement {
	/// <summary>
	/// A user of the system.
	/// </summary>
	public class User {
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
}