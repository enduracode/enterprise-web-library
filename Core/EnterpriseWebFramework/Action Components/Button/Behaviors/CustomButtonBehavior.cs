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
		/// <param name="actionStatementGetter">Do not pass or return null.</param>
		public CustomButtonBehavior( Func<string> actionStatementGetter ) {
			this.actionStatementGetter = actionStatementGetter;
		}

		IEnumerable<Tuple<string, string>> ButtonBehavior.GetAttributes() {
			return Enumerable.Empty<Tuple<string, string>>();
		}

		bool ButtonBehavior.IncludesIdAttribute() {
			return true;
		}

		IEnumerable<EtherealComponent> ButtonBehavior.GetEtherealChildren() {
			return Enumerable.Empty<EtherealComponent>();
		}

		string ButtonBehavior.GetJsInitStatements( string id ) {
			return "$( '#{0}' ).click( function() {{ {1} }} );".FormatWith( id, actionStatementGetter() );
		}

		void ButtonBehavior.AddPostBack() {}
	}
}