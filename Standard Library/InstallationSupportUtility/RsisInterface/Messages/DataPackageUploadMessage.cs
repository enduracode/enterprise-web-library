using System.IO;
using System.ServiceModel;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.RsisInterface.Messages {
	[ MessageContract ]
	public class DataPackageUploadMessage {
		[ MessageHeader ]
		public string AuthenticationKey;

		[ MessageHeader ]
		public int InstallationId;

		[ MessageBodyMember ]
		public Stream Stream;
	}
}