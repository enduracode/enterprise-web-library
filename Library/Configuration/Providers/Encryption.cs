using RedStapler.StandardLibrary.Encryption;

namespace RedStapler.StandardLibrary.Configuration.Providers {
	internal class Encryption: SystemEncryptionProvider {
		byte[] SystemEncryptionProvider.Key {
			get {
				return new byte[]
				       	{
				       		17, 222, 224, 85, 183, 92, 155, 130, 182, 123, 40, 219, 161, 200, 237, 120, 82, 210, 77, 71, 246, 76, 54, 220, 77, 30, 100, 115, 247, 41, 86, 177
				       	};
			}
		}
	}
}