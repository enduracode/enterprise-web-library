using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A behavior that changes a hidden-field value.
	/// </summary>
	public class ChangeValueBehavior: ButtonBehavior {
		private readonly HiddenFieldId hiddenFieldId;
		private readonly string value;

		/// <summary>
		/// Creates a change-value behavior.
		/// </summary>
		/// <param name="hiddenFieldId">Do not pass null.</param>
		/// <param name="value">Do not pass null.</param>
		public ChangeValueBehavior( HiddenFieldId hiddenFieldId, string value ) {
			this.hiddenFieldId = hiddenFieldId;
			this.value = value;
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
			return "$( '#{0}' ).click( function() {{ {1} }} );".FormatWith( id, hiddenFieldId.GetJsValueModificationStatements( "'{0}'".FormatWith( value ) ) );
		}

		void ButtonBehavior.AddPostBack() {}
	}
}