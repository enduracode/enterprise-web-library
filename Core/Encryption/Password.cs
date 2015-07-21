using System;
using System.Security.Cryptography;
using System.Text;

namespace EnterpriseWebLibrary.Encryption {
	/// <summary>
	/// Class for generating and hashing passwords.
	/// Code from http://www.aspheute.com/english/20040105.asp.
	/// </summary>
	public class Password {
		private readonly string password;
		private readonly int salt;

		/// <summary>
		/// Not documented.
		/// </summary>
		public string PasswordText { get { return password; } }

		/// <summary>
		/// Not documented.
		/// </summary>
		public int Salt { get { return salt; } }

		/// <summary>
		/// Generates a new random password with random salt.
		/// </summary>
		public Password(): this( createRandomPassword( 8 ), createRandomSalt() ) {}

		/// <summary>
		/// Create a new password with the given text and randomly generated salt.
		/// </summary>
		public Password( string passwordText ): this( passwordText, createRandomSalt() ) {}

		/// <summary>
		/// Create a password from a stored salt and the given password text.
		/// </summary>
		public Password( string strPassword, int nSalt ) {
			password = strPassword;
			salt = nSalt;
		}

		// It is important that this password not contain white space on the ends since the log in page trims this off.
		private static string createRandomPassword( int passwordLength ) {
			const string allowedChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ23456789";
			var randomBytes = new Byte[passwordLength];
			var rng = new RNGCryptoServiceProvider();
			rng.GetBytes( randomBytes );
			var chars = new char[passwordLength];
			var allowedCharCount = allowedChars.Length;

			for( var i = 0; i < passwordLength; i++ )
				chars[ i ] = allowedChars[ randomBytes[ i ] % allowedCharCount ];

			return new string( chars );
		}

		private static int createRandomSalt() {
			var saltBytes = new Byte[4];
			var rng = new RNGCryptoServiceProvider();
			rng.GetBytes( saltBytes );

			return ( ( saltBytes[ 0 ] << 24 ) + ( saltBytes[ 1 ] << 16 ) + ( saltBytes[ 2 ] << 8 ) + saltBytes[ 3 ] );
		}

		/// <summary>
		/// Not documented.
		/// </summary>
		public byte[] ComputeSaltedHash() {
			// Create a new salt
			var saltBytes = new Byte[ 4 ];
			saltBytes[ 0 ] = (byte)( salt >> 24 );
			saltBytes[ 1 ] = (byte)( salt >> 16 );
			saltBytes[ 2 ] = (byte)( salt >> 8 );
			saltBytes[ 3 ] = (byte)( salt );

			// Create Byte array of password string
			var encoder = new ASCIIEncoding();
			var secretBytes = encoder.GetBytes( password );

			// append the two arrays
			var toHash = new Byte[ secretBytes.Length + saltBytes.Length ];
			Array.Copy( secretBytes, 0, toHash, 0, secretBytes.Length );
			Array.Copy( saltBytes, 0, toHash, secretBytes.Length, saltBytes.Length );

			var sha1 = SHA1.Create();
			return sha1.ComputeHash( toHash );
		}
	}
}