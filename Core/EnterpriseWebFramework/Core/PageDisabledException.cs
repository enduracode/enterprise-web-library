using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An exception caused by an attempt to access a disabled page.
	/// </summary>
	public class PageDisabledException: ApplicationException {
		/// <summary>
		/// Creates a page disabled exception.
		/// </summary>
		public PageDisabledException( string message ): base( message ) {}
	}
}