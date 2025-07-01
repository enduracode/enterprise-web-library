using NodaTime;

namespace EnterpriseWebLibrary;

/// <summary>
/// A leaky-bucket rate limiter. See https://en.wikipedia.org/wiki/Leaky_bucket.
/// </summary>
public class RateLimiter {
	/// <summary>
	/// A method executed if the rate limit has been exceeded.
	/// </summary>
	/// <param name="waitDuration">The time remaining until an action can execute within the rate limit.</param>
	public delegate void LimitExceededMethod( Duration waitDuration );

	private readonly Duration interval;
	private readonly uint maxBurstSize;
	private readonly Func<Instant> timeGetter;
	private readonly object actionLock = new();

	private uint count;
	private Instant lastDecrementTime;

	/// <summary>
	/// Creates a rate limiter.
	/// </summary>
	/// <param name="interval"></param>
	/// <param name="maxBurstSize"></param>
	/// <param name="timeGetter">A function that gets the time instant for an action.</param>
	public RateLimiter( Duration interval, uint maxBurstSize, Func<Instant> timeGetter ) {
		this.interval = interval;
		this.maxBurstSize = maxBurstSize;
		this.timeGetter = timeGetter;

		count = 0;
		lastDecrementTime = timeGetter();
	}

	/// <summary>
	/// Executes one of the specified actions based on the state of this rate limiter. This method is thread safe, but the action executes after the lock is
	/// released.
	/// </summary>
	/// <param name="actionMethod"></param>
	/// <param name="atLimitMethod"></param>
	/// <param name="limitExceededMethod">The method executed if the rate limit has been exceeded.</param>
	public void RequestAction( Action actionMethod, Action atLimitMethod, LimitExceededMethod limitExceededMethod ) {
		Action method;
		lock( actionLock ) {
			// Decrement the count as time passes.
			var currentTime = timeGetter();
			if( currentTime > lastDecrementTime ) {
				uint intervalsPassed;
				checked {
					intervalsPassed = (uint)Math.Floor( ( currentTime - lastDecrementTime ) / interval );
				}
				count = intervalsPassed < count ? count - intervalsPassed : 0;
				lastDecrementTime += interval * intervalsPassed;
			}

			if( count < maxBurstSize ) {
				count += 1;
				method = count < maxBurstSize ? actionMethod : atLimitMethod;
			}
			else {
				var remainingTime = lastDecrementTime + interval - currentTime;
				method = () => limitExceededMethod( remainingTime );
			}
		}

		method();
	}
}