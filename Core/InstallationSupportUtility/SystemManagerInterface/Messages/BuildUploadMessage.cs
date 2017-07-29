using System.IO;
using System.ServiceModel;

namespace EnterpriseWebLibrary.InstallationSupportUtility.SystemManagerInterface.Messages {
	[ MessageContract ]
	public class BuildUploadMessage {
		[ MessageHeader ]
		public string AuthenticationKey;

		[ MessageBodyMember ]
		public Stream BuildDocument;
	}
}