using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public interface ButtonStyle {
		ElementClassSet GetClasses();
		IEnumerable<FlowComponent> GetChildren();
		string GetJsInitStatements( string id );
	}
}