using System.IO;
using System.ServiceModel;

namespace EnterpriseWebLibrary.InstallationSupportUtility.SystemManagerInterface.Messages {
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