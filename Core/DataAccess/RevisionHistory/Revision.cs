using JetBrains.Annotations;

namespace EnterpriseWebLibrary.DataAccess.RevisionHistory;

/// <summary>
/// A revision of a data entity.
/// </summary>
[ PublicAPI ]
public class Revision {
	private readonly int revisionId;
	private readonly int latestRevisionId;
	private readonly int userTransactionId;

	/// <summary>
	/// Creates a revision object.
	/// </summary>
	public Revision( int revisionId, int latestRevisionId, int userTransactionId ) {
		this.revisionId = revisionId;
		this.latestRevisionId = latestRevisionId;
		this.userTransactionId = userTransactionId;
	}

	/// <summary>
	/// Gets the revision's ID.
	/// </summary>
	public int RevisionId => revisionId;

	/// <summary>
	/// Gets the revision's latest revision ID.
	/// </summary>
	public int LatestRevisionId => latestRevisionId;

	/// <summary>
	/// Gets the revision's user transaction ID.
	/// </summary>
	public int UserTransactionId => userTransactionId;
}