using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public interface HyperlinkStyle {
		ElementClassSet GetClasses();
		IEnumerable<FlowComponentOrNode> GetChildren( string destinationUrl );
		string GetJsInitStatements( string id );
	}
}