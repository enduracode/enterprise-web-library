namespace RedStapler.StandardLibrary.DataAccess.CommandWriting {
	/// <summary>
	/// A column name and a value for use by an inline database command.
	/// </summary>
	public class InlineDbCommandColumnValue {
		private readonly string columnName;
		private readonly DbCommandParameter parameter;

		/// <summary>
		/// Creates an inline database command column value.
		/// </summary>
		public InlineDbCommandColumnValue( string columnName, DbParameterValue value ) {
			this.columnName = columnName;
			parameter = new DbCommandParameter( columnName, value );
		}

		internal string ColumnName { get { return columnName; } }

		internal DbCommandParameter Parameter { get { return parameter; } }
	}
}