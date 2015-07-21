using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement {
	/// <summary>
	/// Represents a user of the system.
	/// </summary>
	public interface User {
		/// <summary>
		/// The ID of the user.
		/// </summary>
		int UserId { get; }

		/// <summary>
		/// The email address of the user.
		/// </summary>
		string Email { get; }

		/// <summary>
		/// The role of the user.
		/// </summary>
		Role Role { get; }

		/// <summary>
		/// The last time the user made a request to the system.
		/// </summary>
		DateTime? LastRequestDateTime { get; }

		/// <summary>
		/// The real-world name of the user ("Greg Smalter"). May be the empty string.
		/// </summary>
		string FriendlyName { get; }
	}
}