namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// Application-specific logic for the standard functionality in all pages.
/// </summary>
public abstract class AppStandardPageLogicProvider {
	// The warning below also appears on PageBase.getPageViewDataModificationMethod.
	/// <summary>
	/// Returns a method that executes data modifications that happen simply because of a request and require no other action by the user. Returns null if there
	/// are no modifications, which can improve page performance since the data-access cache does not need to be reset.
	/// 
	/// WARNING: Don't ever use this to correct for missing loadData preconditions. For example, do not create a page that requires a user preferences row to
	/// exist and then use a page-view data modification to create the row if it is missing. Page-view data modifications will not execute before the first
	/// loadData call on post-back requests, and we provide no mechanism to do this because it would allow developers to accidentally cause false user
	/// concurrency errors by modifying data that affects the rendering of the page.
	/// </summary>
	protected internal virtual Action? GetPageViewDataModificationMethod() => null;

	/// <summary>
	/// Gets the display name of the application, which will be included in the title of all pages.
	/// </summary>
	protected internal virtual string AppDisplayName => "";

	/// <summary>
	/// Gets the Typekit Kit ID. Never returns null.
	/// </summary>
	protected internal virtual string TypekitId => "";

	/// <summary>
	/// Returns a sequence of Google Fonts family specifications needed by the application. See https://developers.google.com/fonts/docs/css2.
	/// </summary>
	protected internal virtual IEnumerable<string>? GetGoogleFontsFamilySpecifications() => null;

	/// <summary>
	/// Creates and returns a list of custom style sheets that should be used on all EWF pages, including those not using the EWF user interface.
	/// </summary>
	protected internal virtual List<ResourceInfo> GetStyleSheets() => new();

	/// <summary>
	/// Creates and returns a list of custom style sheets that should be used on pages not using the EWF user interface.
	/// </summary>
	protected internal virtual List<ResourceInfo> GetCustomUiStyleSheets() => new();

	/// <summary>
	/// Gets the Google Analytics Measurement ID, which should always start with "G-". Never returns null.
	/// </summary>
	protected internal virtual string GoogleAnalyticsMeasurementId => "";

	/// <summary>
	/// Gets the Google Analytics User ID. Never returns null.
	/// </summary>
	protected internal virtual string GetGoogleAnalyticsUserId() => "";

	/// <summary>
	/// Creates and returns a list of JavaScript files that should be included on all EWF pages, including those not using the EWF user interface.
	/// </summary>
	protected internal virtual List<ResourceInfo> GetJavaScriptFiles() => new();

	/// <summary>
	/// Gets the favicon to be used for Chrome Application shortcuts.
	/// </summary>
	protected internal virtual ResourceInfo? FaviconPng48X48 => null;

	/// <summary>
	/// Gets the favicon. See http://en.wikipedia.org/wiki/Favicon.
	/// </summary>
	protected internal virtual ResourceInfo? Favicon => null;

	/// <summary>
	/// Gets the function call that should be executed when the DOMContentLoaded event is fired for any page in the application.
	/// </summary>
	protected internal virtual string JavaScriptPageInitFunctionCall => "";
}