using System.Collections.Generic;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Creates Info objects for EWF items.
	/// </summary>
	public interface AppMetaLogicFactory {
		/// <summary>
		/// Standard Library use only.
		/// </summary>
		PageInfo CreateIntermediateLogInPageInfo( string returnUrl );

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		PageInfo CreateLogInPageInfo( string returnUrl );

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		PageInfo CreateSelectUserPageInfo( string returnUrl );

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		PageInfo CreatePreBuiltResponsePageInfo();

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		PageInfo CreateAccessDeniedErrorPageInfo( bool showHomeLink );

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		PageInfo CreatePageDisabledErrorPageInfo( string message );

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		PageInfo CreatePageNotAvailableErrorPageInfo( bool showHomeLink );

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		PageInfo CreateUnhandledExceptionErrorPageInfo();

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		PageInfo CreateBasicTestsPageInfo();

		/// <summary>
		/// Standard Library use only. Returns applicable CSS info objects, in the correct order.
		/// </summary>
		IEnumerable<ResourceInfo> CreateDisplayMediaCssInfos();

		/// <summary>
		/// Standard Library use only. Returns applicable CSS info objects, in the correct order.
		/// </summary>
		IEnumerable<ResourceInfo> CreatePrintMediaCssInfos();

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		ResourceInfo CreateModernizrJavaScriptInfo();

		/// <summary>
		/// Standard Library use only. Returns applicable JavaScript info objects, in the correct order.
		/// </summary>
		IEnumerable<ResourceInfo> CreateJavaScriptInfos();
	}
}