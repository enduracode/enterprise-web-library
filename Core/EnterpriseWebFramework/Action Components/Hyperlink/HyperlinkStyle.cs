using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public interface HyperlinkStyle {
		ElementClassSet GetClasses();
		IEnumerable<FlowComponent> GetChildren( string destinationUrl );
		string GetJsInitStatements( string id );
	}
}