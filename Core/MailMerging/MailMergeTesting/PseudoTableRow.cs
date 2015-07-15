namespace EnterpriseWebLibrary.MailMerging.MailMergeTesting {
	/// <summary>
	/// Has fields in it like a table row would.
	/// </summary>
	internal class PseudoTableRow {
		private readonly int num;

		public PseudoTableRow( int num ) {
			this.num = num;
		}

		internal string FullName { get { return getString( "First Middle Last" ); } }

		private string getString( string s ) {
			return s + num;
		}
	}
}