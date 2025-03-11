using JetBrains.Annotations;

namespace EnterpriseWebLibrary.DataAccess.StandardModification;

/// <summary>
/// A post-delete method that is specified during preDelete and executed after row deletion.
/// </summary>
[ PublicAPI ]
public class PostDeleteExecutor {
	private Action? method;

	/// <summary>
	/// Adds the post-delete method. This can only be called once. The specified data will be passed to the method when it executes; it should include everything the method needs from the rows being deleted and will often simply be a collection of table-retrieval rows. You should retrieve the data in preDelete using the deletion
	/// conditions.
	/// </summary>
	public void AddMethod<T>( T deletedRowData, Action<T> method ) {
		if( this.method is not null )
			throw new Exception( "The method was already added." );
		this.method = () => method( deletedRowData );
	}

	/// <summary>
	/// Generated code use only.
	/// </summary>
	public void Execute() {
		method?.Invoke();
	}
}