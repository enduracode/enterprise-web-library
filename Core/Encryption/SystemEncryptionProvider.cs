namespace RedStapler.StandardLibrary.Encryption {
	/// <summary>
	/// System-specific encryption logic.
	/// </summary>
	public interface SystemEncryptionProvider {
		/// <summary>
		/// To generate a key for a new system, use Rijndael.Create() and then retrieve the key from the resulting object. It should be 32 bytes (256 bits) long
		/// since that is the default key length for the encryption algorithm.
		/// </summary>
		byte[] Key { get; }
	}
}