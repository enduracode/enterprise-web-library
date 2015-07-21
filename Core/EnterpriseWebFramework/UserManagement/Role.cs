namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement {
	/// <summary>
	/// Represents an authorization role in the system.
	/// </summary>
	public class Role {
		/// <summary>
		/// Creates a role object.
		/// </summary>
		/// <param name="roleId"></param>
		/// <param name="name"></param>
		/// <param name="canManageUsers"></param>
		/// <param name="requiresEnhancedSecurity">Enhances security measures such as a shorter session duration.</param>
		public Role( int roleId, string name, bool canManageUsers, bool requiresEnhancedSecurity ) {
			RoleId = roleId;
			Name = name;
			CanManageUsers = canManageUsers;
			RequiresEnhancedSecurity = requiresEnhancedSecurity;
		}

		/// <summary>
		/// Gets the ID of this role.
		/// </summary>
		public int RoleId { get; private set; }

		/// <summary>
		/// Gets the name of this roll.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Returns true if this user can add and remove roles and users.
		/// </summary>
		public bool CanManageUsers { get; private set; }

		/// <summary>
		/// Returns true if the role requires a shorter session duration of 12 minutes.
		/// </summary>
		public bool RequiresEnhancedSecurity { get; private set; }
	}
}