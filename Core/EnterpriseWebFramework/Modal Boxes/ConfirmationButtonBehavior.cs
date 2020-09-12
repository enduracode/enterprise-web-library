using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A behavior that opens a confirmation dialog box that contains a post-back button.
	/// </summary>
	public class ConfirmationButtonBehavior: ButtonBehavior {
		private readonly ConfirmationDialog dialog;
		private readonly ConfirmationFormAction confirmationAction;

		/// <summary>
		/// Creates a confirmation behavior. If you have form controls that should open the same dialog via implicit submission, use
		/// <see cref="ConfirmationFormAction"/> instead.
		/// </summary>
		/// <param name="dialogContent"></param>
		/// <param name="postBack">Pass null to use the post-back corresponding to the first of the current data modifications.</param>
		public ConfirmationButtonBehavior( IReadOnlyCollection<FlowComponent> dialogContent, PostBack postBack = null ) {
			var id = new ConfirmationDialogId();
			dialog = new ConfirmationDialog( id, dialogContent, postBack: postBack );
			confirmationAction = new ConfirmationFormAction( id );
		}

		IEnumerable<Tuple<string, string>> ButtonBehavior.GetAttributes() {
			return Enumerable.Empty<Tuple<string, string>>();
		}

		bool ButtonBehavior.IncludesIdAttribute() {
			return true;
		}

		IReadOnlyCollection<EtherealComponent> ButtonBehavior.GetEtherealChildren() {
			return dialog.ToCollection();
		}

		string ButtonBehavior.GetJsInitStatements( string id ) {
			FormAction action = confirmationAction;
			return "$( '#{0}' ).click( function() {{ {1} }} );".FormatWith( id, action.GetJsStatements() );
		}

		void ButtonBehavior.AddPostBack() {}
	}
}