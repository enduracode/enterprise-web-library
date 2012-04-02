namespace RedStapler.StandardLibrary.DataAccess.CommandWriting {
	/// <summary>
	/// A value used in a database command parameter.
	/// </summary>
	public class DbParameterValue {
		private readonly object value;
		private readonly string dbTypeString;

		/// <summary>
		/// Creates a value with an unspecified type. This is not recommended since it forces the database type to be inferred from the .NET type of the value, and
		/// this process is imperfect and has lead to problems in the past with blobs.
		/// </summary>
		public DbParameterValue( object value ) {
			this.value = value;
		}

		/// <summary>
		/// Creates a value with the specified type.
		/// </summary>
		public DbParameterValue( object value, string dbTypeString ) {
			this.value = value;
			this.dbTypeString = dbTypeString;
		}

		internal object Value { get { return value; } }

		internal string DbTypeString { get { return dbTypeString; } }
	}
}