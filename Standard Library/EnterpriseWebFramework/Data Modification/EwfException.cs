using System;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An exception that is thrown during data modification.
	/// </summary>
	public class DataModificationException: MultiMessageApplicationException {
		/// <summary>
		/// Creates a new exception with the specified message.
		/// </summary>
		public DataModificationException( string message ): base( new[] { message } ) {}

		/// <summary>
		/// Creates a new exception with the specified messages.
		/// </summary>
		public DataModificationException( params string[] messages ): base( messages ) {}
	}

	[ Obsolete( "Guaranteed through 28 February 2014. Please use DataModificationException instead." ) ]
	public class EwfException: DataModificationException {
		[ Obsolete( "Guaranteed through 28 February 2014. Please use DataModificationException instead." ) ]
		public EwfException( string message ): base( message ) {}

		[ Obsolete( "Guaranteed through 28 February 2014. Please use DataModificationException instead." ) ]
		public EwfException( params string[] messages ): base( messages ) {}
	}
}