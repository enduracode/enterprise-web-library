using RedStapler.StandardLibrary.DataAccess;
using System;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {

	/// <summary>
	/// A method that takes a database connection and returns a URL.
	/// </summary>
	public delegate string RedirectingDbMethod( DBConnection cn );

}