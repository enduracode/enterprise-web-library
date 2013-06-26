using System;

namespace RedStapler.StandardLibrary.DataAccess {
	[ Obsolete( "Guaranteed through 30 September 2013. Please avoid passing database connections around." ) ]
	public delegate void DbMethod( DBConnection cn );
}