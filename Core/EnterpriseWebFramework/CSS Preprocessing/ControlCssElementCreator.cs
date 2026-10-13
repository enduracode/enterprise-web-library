using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// An object that defines one or more CSS elements.
/// </summary>
[ UsedImplicitly( ImplicitUseTargetFlags.WithInheritors ) ]
public interface ControlCssElementCreator {
	/// <summary>
	/// Creates an array of CSS elements.
	/// </summary>
	IReadOnlyCollection<CssElement> CreateCssElements();
}