using System;

namespace EnterpriseWebLibrary {
	/// <summary>
	/// EWL and System Manager use only.
	/// </summary>
	public class EwlNuGetPackageSpecificationStatics {
		/// <summary>
		/// Specify the build number only for prereleases.
		/// </summary>
		public static string GetNuGetPackageFileName( string id, int majorVersion, int? buildNumber, DateTime? localExportDateAndTime = null ) =>
			id + "." + GetNuGetPackageVersionString( majorVersion, buildNumber, localExportDateAndTime: localExportDateAndTime ) + ".nupkg";

		/// <summary>
		/// Specify the build number only for prereleases.
		/// </summary>
		public static string GetNuGetPackageVersionString( int majorVersion, int? buildNumber, DateTime? localExportDateAndTime = null ) =>
			majorVersion + ".0.0" + ( buildNumber.HasValue
				                          ? "-pr" + ( localExportDateAndTime.HasValue ? buildNumber.Value - 1 : buildNumber.Value ).ToString( "d5" ) +
				                            ( localExportDateAndTime.HasValue ? "-" + localExportDateAndTime.Value.ToString( "yyMMddHHmm" ) : "" )
				                          : "" );
	}
}