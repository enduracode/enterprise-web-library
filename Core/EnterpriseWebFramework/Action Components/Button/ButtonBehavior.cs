using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The behavior for a button.
	/// </summary>
	public interface ButtonBehavior {
		IReadOnlyCollection<Tuple<string, string>> GetAttributes();
		bool IncludesIdAttribute();
		IEnumerable<EtherealComponent> GetEtherealChildren();
		string GetJsInitStatements( string id );
		void AddPostBack();
	}
}