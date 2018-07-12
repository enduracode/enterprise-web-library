using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public interface ButtonStyle {
		ElementClassSet GetClasses();
		IReadOnlyCollection<FlowComponent> GetChildren();
		string GetJsInitStatements( string id );
	}
}