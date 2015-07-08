using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	internal interface EtherealControl {
		WebControl Control { get; }
		string GetJsInitStatements();
	}
}