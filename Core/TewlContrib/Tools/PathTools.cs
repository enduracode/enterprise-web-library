namespace EnterpriseWebLibrary.TewlContrib;

public static class PathTools {
	/// <summary>
	/// Normalizes the path to use forward slashes. On Linux, this is a no-op since forward slash is the native separator.
	/// </summary>
	public static string NormalizePath( string path ) => path.Replace( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar );
}