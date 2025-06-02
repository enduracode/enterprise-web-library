using EnterpriseWebLibrary.UserManagement;

namespace EnterpriseWebLibrary;

// Do NOT add anything new to this class; we will soon delete it.
public static class AppTools {
	[ Obsolete( "Guaranteed through 30 June 2025. Use SystemUser.Current instead." ) ]
	public static SystemUser? User => SystemUser.Current;
}