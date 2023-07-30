#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A behavior that executes custom JavaScript.
	/// </summary>
	public class CustomButtonBehavior: ButtonBehavior {
		private readonly Func<string> actionStatementGetter;

		/// <summary>
		/// Creates a custom behavior.
		/// </summary>
		/// <param name="actionStatementGetter">A function that gets the JavaScript action statements. The statements can use the variable “e” to access the Event
		/// object. Do not pass or return null.</param>
		public CustomButtonBehavior( Func<string> actionStatementGetter ) {
			this.actionStatementGetter = actionStatementGetter;
		}

		IEnumerable<ElementAttribute> ButtonBehavior.GetAttributes() => Enumerable.Empty<ElementAttribute>();

		bool ButtonBehavior.IncludesIdAttribute() {
			return true;
		}

		IReadOnlyCollection<EtherealComponent> ButtonBehavior.GetEtherealChildren() {
			return null;
		}

		string ButtonBehavior.GetJsInitStatements( string id ) {
			return "$( '#{0}' ).click( function( e ) {{ {1} }} );".FormatWith( id, actionStatementGetter() );
		}

		void ButtonBehavior.AddPostBack() {}
	}
}