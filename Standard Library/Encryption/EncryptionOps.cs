using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace RedStapler.StandardLibrary.Encryption {
	/// <summary>
	/// Provides a suite of static methods to simplify symmetric encryption. Designed for systems that use a single, global encryption key.
	/// </summary>
	public static class EncryptionOps {
		private const string providerName = "Encryption";
		private static SystemEncryptionProvider provider;

		internal static void Init( Type systemLogicType ) {
			provider = StandardLibraryMethods.GetSystemLibraryProvider( systemLogicType, providerName ) as SystemEncryptionProvider;
		}

		internal static SystemEncryptionProvider SystemProvider {
			get {
				if( provider == null )
					throw StandardLibraryMethods.CreateProviderNotFoundException( providerName );
				return provider;
			}
		}

		/// <summary>
		/// Generates a random initialization vector, which should be 16 bytes long since that is the block size of the encryption algorithm.
		/// </summary>
		public static byte[] GenerateInitVector() {
			SymmetricAlgorithm algorithm = Rijndael.Create();
			return algorithm.IV;
		}

		/// <summary>
		/// Encrypts the specified string and returns a byte array representing the encrypted value. The length of this array should be a multiple of 16 since the
		/// encryption algorithm's block size is 16 bytes.
		/// </summary>
		public static byte[] EncryptString( byte[] initVector, string value ) {
			SymmetricAlgorithm algorithm = Rijndael.Create();
			algorithm.Key = SystemProvider.Key;
			algorithm.IV = initVector;
			byte[] encryptedValue;
			using( var ms = new MemoryStream() ) {
				using( var cs = new CryptoStream( ms, algorithm.CreateEncryptor(), CryptoStreamMode.Write ) ) {
					using( TextWriter tw = new StreamWriter( cs ) )
						tw.Write( value );
				}
				encryptedValue = ms.ToArray();
			}
			return encryptedValue;
		}

		/// <summary>
		/// Decrypts the specified byte array and returns a string representing the decrypted value.
		/// </summary>
		public static string DecryptString( byte[] initVector, byte[] value ) {
			SymmetricAlgorithm algorithm = Rijndael.Create();
			algorithm.Key = SystemProvider.Key;
			algorithm.IV = initVector;
			string decryptedValue;
			using( var ms = new MemoryStream( value ) ) {
				using( var cs = new CryptoStream( ms, algorithm.CreateDecryptor(), CryptoStreamMode.Read ) ) {
					using( TextReader tr = new StreamReader( cs ) )
						decryptedValue = tr.ReadToEnd();
				}
			}
			return decryptedValue;
		}

		/// <summary>
		/// Encrypts the specified string and returns a base64 string that contains both the init vector as well as the encrypted data.
		/// This can be used to encrypt query parameters or to store the value in a page or cookie.
		/// It's okay to visibly store the init vector (though, it's not obvious what is the init vector and what is the encrypted data in the
		/// resulting string) because what keeps the data truly secure is the key used to encrypt. Init vectors are used to insure variety when encrypting
		/// the same data with the same key (providing you pass a different init vector each time), thus preventing rainbow table attacks.
		/// </summary>
		public static string GetEncryptedString( byte[] initVector, string value ) {
			return ( BitConverter.ToString( initVector ) + BitConverter.ToString( EncryptString( initVector, value ) ) ).Replace( "-", "" );
		}

		/// <summary>
		/// This returns the encrypted string that was encrypted via the GetEncryptedString method.
		/// </summary>
		public static string GetDecryptedString( string encryptedString ) {
			// IV is the first part of the parmater, which is 16 bytes/32 hex characters
			var pairs = new List<string>();
			const int numberofIvHexCharacters = 16 * 2; // 16 byte IVs * # of hex characters to represent a byte
			for( var i = 0; i < numberofIvHexCharacters; i += 2 )
				pairs.Add( encryptedString.Substring( i, 2 ) );
			// The length of the value will always be a multiple of 16
			var message = new List<string>();
			for( var i = numberofIvHexCharacters; i < encryptedString.Length; i += 2 )
				message.Add( encryptedString.Substring( i, 2 ) );
			var iV = pairs.Select( h => Convert.ToByte( h, 16 ) ).ToArray();
			return DecryptString( iV, message.Select( h => Convert.ToByte( h, 16 ) ).ToArray() );
		}
	}
}