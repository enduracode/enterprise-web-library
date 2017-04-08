namespace EnterpriseWebLibrary.Email {
	/// <summary>
	/// The name of a file-based email template.
	/// </summary>
	public sealed class EmailTemplateName {
		internal readonly string TemplateName;

		/// <summary>
		/// Development Utility use only.
		/// </summary>
		public EmailTemplateName( string templateName ) {
			TemplateName = templateName;
		}
	}
}