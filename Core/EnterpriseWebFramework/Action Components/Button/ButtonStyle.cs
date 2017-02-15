using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public interface ButtonStyle {
		ElementClassSet GetClasses();
		IEnumerable<FlowComponentOrNode> GetChildren();
		string GetJsInitStatements( string id );
	}
}