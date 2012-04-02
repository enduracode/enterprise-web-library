using System;

namespace RedStapler.StandardLibrary.Encryption {
	/// <summary>
	/// NOTE: Use the query parameter encrypting method used by M+Vision. Remove usages of these methods.
	/// </summary>
	public static class MakeshiftEncryptionOps {
		/// <summary>
		/// A makeshift encryption method used by Todd.
		/// Do not use.
		/// </summary>
		public static string EncryptNumber( int num ) {
			return num + "-" + makeLetter( num % 17 ) + makeLetter( num % 9 ) + makeLetter( num % 24 ) + makeLetter( num % 23 );
		}

		/// <summary>
		/// A makeshift decryption method used by Todd. Returns true if the given encrypted key is a valid encryption of the given number, according to EncryptNumber.
		/// Do not use.
		/// </summary>
		public static bool DecryptNumber( string key, out int num ) {
			num = 0;
			if( key == null )
				return false;

			try {
				num = Convert.ToInt32( key.Split( '-' )[ 0 ] );
			}
			catch( FormatException ) {
				return false;
			}
			catch( OverflowException ) {
				return false;
			}

			return ( key == EncryptNumber( num ) );
		}

		private static char makeLetter( int num ) {
			return (char)( num + 97 ); // 97 is ascii 'a'
		}
	}
}