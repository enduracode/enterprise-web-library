using System.ComponentModel;

namespace EnterpriseWebLibrary.DataAccess;

/// <summary>
/// EWL use only.
/// </summary>
[ EditorBrowsable( EditorBrowsableState.Never ) ]
public interface TableRetrievalRow<out PkType> {
	/// <summary>
	/// EWL use only.
	/// </summary>
	[ EditorBrowsable( EditorBrowsableState.Never ) ]
	PkType PrimaryKey { get; }
}