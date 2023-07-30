#nullable disable
using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class ResourceNotAvailableException: Exception {
		public ResourceNotAvailableException( string message, Exception innerException ): base( message, innerException ) {}
	}
}