#nullable disable
using System;
using System.Linq;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An action that opens a confirmation dialog box.
	/// </summary>
	public class ConfirmationFormAction: NonPostBackFormAction {
		private readonly ConfirmationDialogId dialogId;

		public ConfirmationFormAction( ConfirmationDialogId dialogId ) {
			this.dialogId = dialogId;
		}

		void FormAction.AddToPageIfNecessary() {}

		string FormAction.GetJsStatements() {
			if( !dialogId.ModalBoxId.ElementId.Id.Any() )
				throw new ApplicationException( "The confirmation dialog box must be on the page." );
			return "document.getElementById( '{0}' ).showModal();".FormatWith( dialogId.ModalBoxId.ElementId.Id );
		}
	}
}