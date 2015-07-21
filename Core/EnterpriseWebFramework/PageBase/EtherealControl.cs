using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal interface EtherealControl {
		WebControl Control { get; }
		string GetJsInitStatements();
	}
}