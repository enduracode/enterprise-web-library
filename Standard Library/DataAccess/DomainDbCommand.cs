using System;

namespace RedStapler.StandardLibrary.DataAccess {
	[ Obsolete( "Guaranteed through 30 September 2013. Please use a generic delegate for the Execute method if you need this functionality." ) ]
	public interface DomainDbCommand {
		[ Obsolete( "Guaranteed through 30 September 2013. Please use a generic delegate for the Execute method if you need this functionality." ) ]
		void Execute();
	}
}