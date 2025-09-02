namespace EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;

public interface WebItem {
	/// <summary>
	/// Gets whether the authenticated user is authorized to access this item.
	/// </summary>
	bool UserCanAccess { get; }

	/// <summary>
	/// Gets the alternative mode for this item, or null if it is in normal mode.
	/// </summary>
	AlternativeResourceMode? AlternativeMode { get; }

	internal EwfUrl GetEwfUrl( bool ensureUserCanAccessItem, bool ensureItemNotDisabled );

	internal IEnumerable<NestedUrl?> GetNestedUrls();
}