using System.ServiceModel;

namespace RedStapler.TestWebSite.Service {
	// NOTE: If you change the interface name "IService" here, you must also update the reference to "IService" in Web.config.
	[ ServiceContract ]
	public interface IService {
		[ OperationContract ]
		string[] GetAutoFillTextBoxChoices( string prefixText, int count );
	}
}