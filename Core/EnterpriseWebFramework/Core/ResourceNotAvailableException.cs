using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An exception caused by an invalid URL query string.
	/// </summary>
	public class ResourceNotAvailableException: ApplicationException {
		/// <summary>
		/// Creates a new ResourceNotAvailableException with the specified message and inner exception.
		/// </summary>
		public ResourceNotAvailableException( string message, Exception innerException ): base( message, innerException ) {}
	}
}