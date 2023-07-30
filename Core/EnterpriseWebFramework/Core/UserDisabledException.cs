#nullable disable
using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An exception caused by an attempt to access the authenticated user when it has been disabled.
	/// </summary>
	public class UserDisabledException: Exception {
		internal UserDisabledException( string message ): base( message ) {}
	}
}