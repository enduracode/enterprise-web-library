#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Web;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An exception that is thrown during data modification.
	/// </summary>
	public class DataModificationException: ApplicationException {
		/// <summary>
		/// Gets the messages that describe the exception.
		/// </summary>
		public IReadOnlyCollection<TrustedHtmlString> HtmlMessages { get; }

		/// <summary>
		/// Creates a new exception with the specified message.
		/// </summary>
		public DataModificationException( string message ): this( new[] { message } ) {}

		/// <summary>
		/// Creates a new exception with the specified messages.
		/// </summary>
		public DataModificationException( params string[] messages ): this(
			messages.Select( i => new TrustedHtmlString( HttpUtility.HtmlEncode( i ) ) ).ToImmutableArray() ) {}

		internal DataModificationException( IReadOnlyCollection<TrustedHtmlString> messages ): base(
			StringTools.ConcatenateWithDelimiter( Environment.NewLine, messages.Select( i => i.Html ).ToArray() ) ) {
			HtmlMessages = messages;
		}
	}
}