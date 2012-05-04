namespace RedStapler.StandardLibrary {
	/// <summary>
	/// Standard Library and RSIS use only.
	/// </summary>
	public class EwlNuGetPackageSpecificationStatics {
		/// <summary>
		/// Specify the build number only for prereleases.
		/// </summary>
		public static string GetNuGetPackageFileName( string systemShortName, int majorVersion, int? buildNumber ) {
			return GetNuGetPackageId( systemShortName ) + "." + GetNuGetPackageVersionString( majorVersion, buildNumber ) + ".nupkg";
		}

		public static string GetNuGetPackageId( string systemShortName ) {
			return systemShortName;
		}

		/// <summary>
		/// Specify the build number only for prereleases.
		/// </summary>
		public static string GetNuGetPackageVersionString( int majorVersion, int? buildNumber ) {
			return majorVersion + ".0.0" + ( buildNumber.HasValue ? "-prerelease" + buildNumber.Value.ToString( "d5" ) : "" );
		}
	}
}