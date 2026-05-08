using JetBrains.Annotations;

namespace EnterpriseWebLibrary.TewlContrib;

public static class StringToolsLegacy {
	/// <summary>
	/// Do not use. Migrate to string interpolation instead.
	/// </summary>
	[ StringFormatMethod( "s" ) ]
	public static string FormatWith( this string s, params object?[] objects ) => string.Format( s, objects );
}