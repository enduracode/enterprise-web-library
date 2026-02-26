namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class ConfirmationDialogId {
		internal readonly ModalBoxId ModalBoxId;

		/// <summary>
		/// Creates a confirmation-dialog ID.
		/// </summary>
		public ConfirmationDialogId() {
			ModalBoxId = new ModalBoxId();
		}
	}
}