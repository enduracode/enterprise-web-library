namespace RedStapler.StandardLibrary.DataAccess {
	/// <summary>
	/// Implemented by wrappers used to update or insert groups of related data.
	/// </summary>
	public interface DomainDbCommand {
		/// <summary>
		/// Executes the database command.
		/// </summary>
		void Execute();
	}
}