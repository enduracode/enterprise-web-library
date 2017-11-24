using System;
using NodaTime;

namespace EnterpriseWebLibrary {
	/// <summary>
	/// A leaky-bucket rate limiter. See https://en.wikipedia.org/wiki/Leaky_bucket.
	/// </summary>
	public class RateLimiter {
		private readonly Duration interval;
		private readonly uint maxBurstSize;

		private uint count;
		private Instant lastDecrementTime;

		public RateLimiter( Duration interval, uint maxBurstSize ) {
			this.interval = interval;
			this.maxBurstSize = maxBurstSize;

			count = 0;
			lastDecrementTime = SystemClock.Instance.GetCurrentInstant();
		}

		public void RequestAction( Action actionMethod, Action atLimitMethod, Action limitExceededMethod ) {
			// Decrement the count as time passes.
			var currentTime = SystemClock.Instance.GetCurrentInstant();
			if( currentTime > lastDecrementTime ) {
				uint intervalsPassed;
				checked {
					intervalsPassed = (uint)Math.Floor( ( currentTime - lastDecrementTime ) / interval );
				}
				count = Math.Max( count - intervalsPassed, 0 );
				lastDecrementTime += interval * intervalsPassed;
			}

			if( count < maxBurstSize ) {
				count += 1;
				if( count < maxBurstSize )
					actionMethod();
				else
					atLimitMethod();
			}
			else
				limitExceededMethod();
		}
	}
}