using EnterpriseWebLibrary.UserManagement;

namespace EnterpriseWebLibrary;

// Do NOT add anything new to this class; we will soon delete it.
public static class AppTools {
	/// <summary>
	/// Do not use. User.Current replaces this property.
	/// </summary>
	public static User User => User.Current;
}