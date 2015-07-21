using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary {
	/// <summary>
	/// Provides helpful methods to get random numbers and strings without falling into random pitfalls.
	/// </summary>
	public static class Randomness {
		// We want one instance per thread because Random is not thread safe. 
		// Threadstatic is safe for us to use even in ASP.NET because we don't care if/when the resource is cleaned up and we don't care if multiple requests
		// use the same Random object. Another approach would be to have one Random object and use lock when calling the next method.
		// We cannot do inline initialization or initialize in a static constructor with ThreadStatic because then only the first thread to come along would get a value initialized properly.
		// This also means that it does not make sense to use Lazy<T>.
		[ ThreadStatic ]
		private static Random randomPerThread;

		private static Random random { get { return randomPerThread ?? ( randomPerThread = new Random() ); } }

		// NOTE: Change these all to start with Get.

		/// <summary>
		/// Returns a random string with the given min and max lengths (both inclusive). The default minLength is 0 (empty string can be returned) and
		/// the default max length is 8.
		/// </summary>
		public static string GenerateRandomString( int minLength = 0, int maxLength = 8 ) {
			if( minLength > maxLength )
				throw new ApplicationException( "MinLength cannot be greater than MaxLength." );

			var length = GetRandomInt( minLength, maxLength + 1 );
			var randomString = "";
			for( var i = 0; i < length; i++ )
				randomString += GetRandomLetter();
			return randomString;
		}

		/// <summary>
		/// Returns a random lowercase letter from the 26-letter alphabet.
		/// </summary>
		public static char GetRandomLetter() {
			const string letters = "abcdefghiklmnopqrstuvwxyz";
			return letters[ GetRandomInt( 0, letters.Length ) ];
		}

		/// <summary>
		/// Returns a random email address always for the redstapler.biz domain, in the form: 'RandomCharacters@redstapler.biz'.
		/// </summary>
		public static string GenerateRandomEmailAddress() {
			return GenerateRandomString( 8, 20 ) + "@redstapler.biz";
		}

		/// <summary>
		/// Has a 50% chance of returning true.
		/// </summary>
		public static bool FlipCoin() {
			return random.NextDouble() < .5;
		}

		/// <summary>
		/// Has a percentSuccess% chance of returning true.
		/// </summary>
		public static bool TakeChance( int percentSuccess ) {
			if( percentSuccess < 0 || percentSuccess > 100 )
				throw new ApplicationException( "Percent Success must be between 0 and 100, inclusive." );

			return GetRandomInt( 0, 100 ) < percentSuccess;
		}

		/// <summary>
		/// Returns a random integer with the given number of digits (must be between 1 and 9).
		/// </summary>
		public static int GetRandomInt( int digits ) {
			if( digits < 1 || digits >= 10 )
				throw new ArgumentException( "Number of digits must be between 1 and 9 inclusive." );
			return GetRandomInt( (int)Math.Pow( 10, digits - 1 ), (int)Math.Pow( 10, digits ) );
		}

		/// <summary>
		/// Minvalue is inclusive. MaxValue is exclusive by default (maxValueInclusive = false).
		/// </summary>
		public static int GetRandomInt( int minValue, int maxValue, bool maxValueInclusive = false ) {
			if( maxValueInclusive )
				maxValue++;
			return random.Next( minValue, maxValue );
		}

		/// <summary>
		/// Returns one of the items passed.
		/// </summary>
		public static T Choose<T>( params T[] items ) {
			return items.GetRandomElement();
		}
	}
}