using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Framework use only.
	/// </summary>
	public class UnresolvableUrlException: Exception {
		/// <summary>
		/// Framework use only.
		/// </summary>
		public UnresolvableUrlException( string message, Exception innerException ): base( message, innerException ) {}
	}
}