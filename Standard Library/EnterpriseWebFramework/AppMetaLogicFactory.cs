using System.Collections.Generic;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Creates Info objects for EWF items.
	/// </summary>
	public interface AppMetaLogicFactory {
		/// <summary>
		/// Standard Library use only.
		/// </summary>
		PageInfo GetIntermediateLogInPageInfo( string returnUrl );

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		PageInfo GetLogInPageInfo( string returnUrl );

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
		IEnumerable<CssInfo> GetDisplayMediaCssInfos();

		/// <summary>
		/// Standard Library use only. Returns applicable CSS info objects, in the correct order.
		/// </summary>
		IEnumerable<CssInfo> GetPrintMediaCssInfos();
	}
}