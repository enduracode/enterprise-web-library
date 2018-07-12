using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The behavior for a button.
	/// </summary>
	public interface ButtonBehavior {
		IEnumerable<Tuple<string, string>> GetAttributes();
		bool IncludesIdAttribute();
		IReadOnlyCollection<EtherealComponent> GetEtherealChildren();
		string GetJsInitStatements( string id );
		void AddPostBack();
	}
}