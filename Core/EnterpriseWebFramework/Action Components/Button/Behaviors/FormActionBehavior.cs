using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A behavior that performs a form action.
	/// </summary>
	public class FormActionBehavior: ButtonBehavior {
		/// <summary>
		/// UiButtonSetup and private use only.
		/// </summary>
		internal readonly FormAction Action;

		/// <summary>
		/// Creates a form-action behavior.
		/// </summary>
		/// <param name="action">Do not pass null.</param>
		public FormActionBehavior( FormAction action ) {
			Action = action;
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
			return "$( '#{0}' ).click( function() {{ {1} }} );".FormatWith( id, Action.GetJsStatements() );
		}

		void ButtonBehavior.AddPostBack() {
			Action.AddToPageIfNecessary();
		}
	}
}