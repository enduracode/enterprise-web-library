using Microsoft.AspNetCore.Http;

namespace EnterpriseWebLibrary.UserManagement.IdentityProviders;

public class CustomIdentityProvider: IdentityProvider {
	public delegate string LogInPageGetterMethod( string returnUrl );

	public delegate SystemUser? LogInUserGetterMethod( IQueryCollection query );

	internal readonly string Identifier;
	private readonly LogInPageGetterMethod logInPageGetter;
	private readonly LogInUserGetterMethod logInUserGetter;

	/// <summary>
	/// Creates a custom identity provider.
	/// </summary>
	/// <param name="identifier">Do not pass the empty string.</param>
	/// <param name="logInPageGetter">A function that takes a return URL and returns the log-in page.</param>
	/// <param name="logInUserGetter">A function that takes the query parameters from the authentication page and returns the corresponding user object, or null
	/// if a user does not exist. This function may also update the user if necessary, or even create a new user. Do not pass null.</param>
	public CustomIdentityProvider( string identifier, LogInPageGetterMethod logInPageGetter, LogInUserGetterMethod logInUserGetter ) {
		Identifier = identifier;
		this.logInPageGetter = logInPageGetter;
		this.logInUserGetter = logInUserGetter;
	}

	internal string GetLogInPage( string returnUrl ) => logInPageGetter( returnUrl );

	internal SystemUser? LogInUser( IQueryCollection query ) => logInUserGetter( query );
}