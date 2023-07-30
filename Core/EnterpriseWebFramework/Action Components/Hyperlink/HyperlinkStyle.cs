#nullable disable
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public interface HyperlinkStyle {
		ElementClassSet GetClasses();
		IReadOnlyCollection<FlowComponent> GetChildren( string destinationUrl );
		string GetJsInitStatements( string id );
	}
}