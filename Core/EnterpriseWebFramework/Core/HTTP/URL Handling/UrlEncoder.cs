using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public interface UrlEncoder {
		/// <summary>
		/// Framework use only.
		/// </summary>
		IReadOnlyCollection<( string name, string value, bool isSegmentParameter )> GetRemainingParameters();
	}
}