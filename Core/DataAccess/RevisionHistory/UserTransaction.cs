using JetBrains.Annotations;
using NodaTime;

namespace EnterpriseWebLibrary.DataAccess.RevisionHistory;

/// <summary>
/// A transaction performed by a user.
/// </summary>
[ PublicAPI ]
public class UserTransaction {
	private readonly int userTransactionId;
	private readonly int? userId;
	private readonly Instant transactionTime;

	/// <summary>
	/// Creates a user transaction.
	/// </summary>
	public UserTransaction( int userTransactionId, int? userId, Instant transactionTime ) {
		this.userTransactionId = userTransactionId;
		this.userId = userId;
		this.transactionTime = transactionTime;
	}

	/// <summary>
	/// Gets the transaction’s ID.
	/// </summary>
	public int UserTransactionId => userTransactionId;

	/// <summary>
	/// Gets the transaction’s user ID.
	/// </summary>
	public int? UserId => userId;

	/// <summary>
	/// Gets the transaction’s time.
	/// </summary>
	public Instant TransactionTime => transactionTime;

	public string LocalTransactionDateAndTimeString {
		get {
			var localDateAndTime = transactionTime.InZone( DateTimeZoneProviders.Tzdb.GetSystemDefault() ).ToDateTimeUnspecified();
			return "{0}, {1}".FormatWith( localDateAndTime.ToDayMonthYearString( false ), localDateAndTime.ToHourAndMinuteString() );
		}
	}
}