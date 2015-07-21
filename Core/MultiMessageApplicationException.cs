using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace EnterpriseWebLibrary {
	/// <summary>
	/// An application exception with multiple messages.
	/// </summary>
	public class MultiMessageApplicationException: ApplicationException {
		private readonly ImmutableArray<string> messages;

		/// <summary>
		/// Creates an exception with the specified messages.
		/// </summary>
		public MultiMessageApplicationException( params string[] messages ): base( StringTools.ConcatenateWithDelimiter( Environment.NewLine, messages ) ) {
			this.messages = ImmutableArray.Create( messages );
		}

		/// <summary>
		/// Gets the messages that describe the exception.
		/// </summary>
		public IReadOnlyCollection<string> Messages { get { return messages; } }
	}
}