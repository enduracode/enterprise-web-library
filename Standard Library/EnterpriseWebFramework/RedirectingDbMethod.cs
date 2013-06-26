using System;
using RedStapler.StandardLibrary.DataAccess;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	[ Obsolete( "Guaranteed through 30 September 2013. Please avoid passing database connections around." ) ]
	public delegate string RedirectingDbMethod( DBConnection cn );
}