using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The behavior for a button.
	/// </summary>
	public interface ButtonBehavior {
		IEnumerable<ElementAttribute> GetAttributes();
		bool IncludesIdAttribute();
		IReadOnlyCollection<EtherealComponent> GetEtherealChildren();
		string GetJsInitStatements( string id );
		void AddPostBack();
	}
}