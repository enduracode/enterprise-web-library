using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Creates Info objects for EWF items.
	/// </summary>
	public interface AppMetaLogicFactory {
		/// <summary>
		/// EWL use only.
		/// </summary>
		PageInfo CreateIntermediateLogInPageInfo( string returnUrl );

		/// <summary>
		/// EWL use only.
		/// </summary>
		PageInfo CreateLogInPageInfo( string returnUrl );

		/// <summary>
		/// EWL use only.
		/// </summary>
		PageInfo CreateSelectUserPageInfo( string returnUrl );

		/// <summary>
		/// EWL use only.
		/// </summary>
		PageInfo CreatePreBuiltResponsePageInfo();

		/// <summary>
		/// EWL use only.
		/// </summary>
		PageInfo CreateAccessDeniedErrorPageInfo( bool showHomeLink );

		/// <summary>
		/// EWL use only.
		/// </summary>
		PageInfo CreatePageDisabledErrorPageInfo( string message );

		/// <summary>
		/// EWL use only.
		/// </summary>
		PageInfo CreatePageNotAvailableErrorPageInfo( bool showHomeLink );

		/// <summary>
		/// EWL use only.
		/// </summary>
		PageInfo CreateUnhandledExceptionErrorPageInfo();

		/// <summary>
		/// EWL use only.
		/// </summary>
		PageInfo CreateBasicTestsPageInfo();

		/// <summary>
		/// EWL use only. Returns applicable CSS info objects, in the correct order.
		/// </summary>
		IEnumerable<ResourceInfo> CreateBasicCssInfos();

		/// <summary>
		/// EWL use only. Returns applicable CSS info objects, in the correct order.
		/// </summary>
		IEnumerable<ResourceInfo> CreateEwfUiCssInfos();

		/// <summary>
		/// EWL use only.
		/// </summary>
		ResourceInfo CreateModernizrJavaScriptInfo();

		/// <summary>
		/// EWL use only. Returns applicable JavaScript info objects, in the correct order.
		/// </summary>
		IEnumerable<ResourceInfo> CreateJavaScriptInfos();
	}
}