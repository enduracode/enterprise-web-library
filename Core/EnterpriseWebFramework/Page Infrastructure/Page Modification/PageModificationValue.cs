using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A form-control-dependent value used to modify the page consistently on both the server and the client.
	/// </summary>
	public class PageModificationValue<T> {
		private readonly InitializationAwareValue<T> value = new InitializationAwareValue<T>();
		private readonly List<Func<string, string>> jsModificationStatements = new List<Func<string, string>>();

		/// <summary>
		/// Creates a page-modification value.
		/// </summary>
		public PageModificationValue() {}

		/// <summary>
		/// Adds a JavaScript statement that should be executed when the value changes.
		/// </summary>
		/// <param name="statementGetter">A function that takes the value expression and returns a complete statement. Do not pass null.</param>
		public void AddJsModificationStatement( Func<string, string> statementGetter ) {
			// This dependency on EwfPage should not exist.
			EwfPage.AssertPageTreeNotBuilt();

			jsModificationStatements.Add( statementGetter );
		}

		internal void AddValue( T value ) {
			if( this.value.Initialized )
				throw new ApplicationException( "The value was already added." );
			this.value.Value = value;
		}

		/// <summary>
		/// Gets the value. Not available until after the page tree has been built.
		/// </summary>
		public T Value {
			get {
				// This dependency on EwfPage should not exist.
				EwfPage.AssertPageTreeBuilt();

				return value.Value;
			}
		}

		/// <summary>
		/// Returns the JavaScript statements that should be executed when the value changes. Not available until after the page tree has been built.
		/// </summary>
		public string GetJsModificationStatements( string valueExpression ) {
			// This dependency on EwfPage should not exist.
			EwfPage.AssertPageTreeBuilt();

			return jsModificationStatements.Aggregate(
				new StringBuilder(),
				( builder, statementGetter ) => builder.Append( statementGetter( valueExpression ) ),
				i => i.ToString() );
		}
	}
}