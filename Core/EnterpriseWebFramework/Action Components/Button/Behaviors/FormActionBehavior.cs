using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A behavior that performs a form action.
	/// </summary>
	public class FormActionBehavior: ButtonBehavior {
		private readonly FormAction action;

		/// <summary>
		/// Creates a form-action behavior.
		/// </summary>
		/// <param name="action">Do not pass null.</param>
		public FormActionBehavior( FormAction action ) {
			this.action = action;
		}

		IEnumerable<Tuple<string, string>> ButtonBehavior.GetAttributes() {
			return Enumerable.Empty<Tuple<string, string>>();
		}

		bool ButtonBehavior.IncludesIdAttribute() {
			return true;
		}

		IReadOnlyCollection<EtherealComponent> ButtonBehavior.GetEtherealChildren() {
			return null;
		}

		string ButtonBehavior.GetJsInitStatements( string id ) {
			return "$( '#{0}' ).click( function() {{ {1} }} );".FormatWith( id, action.GetJsStatements() );
		}

		void ButtonBehavior.AddPostBack() {
			action.AddToPageIfNecessary();
		}
	}
}