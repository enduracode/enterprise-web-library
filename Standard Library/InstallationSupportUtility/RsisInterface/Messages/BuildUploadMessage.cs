using System.IO;
using System.ServiceModel;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.RsisInterface.Messages {
	[ MessageContract ]
	public class BuildUploadMessage {
		[ MessageHeader ]
		public string AuthenticationKey;

		[ MessageBodyMember ]
		public Stream BuildDocument;
	}
}