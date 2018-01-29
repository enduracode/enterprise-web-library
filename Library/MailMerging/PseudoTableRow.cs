namespace EnterpriseWebLibrary.MailMerging {
	/// <summary>
	/// Has fields in it like a table row would.
	/// </summary>
	public class PseudoTableRow {
		private readonly int num;

		public PseudoTableRow( int num ) {
			this.num = num;
		}

		internal string FullName => getString( "First Middle Last" );

		private string getString( string s ) {
			return s + num;
		}
	}
}