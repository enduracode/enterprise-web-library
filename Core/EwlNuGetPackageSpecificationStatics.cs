using System;

namespace EnterpriseWebLibrary {
	/// <summary>
	/// EWL and System Manager use only.
	/// </summary>
	public class EwlNuGetPackageSpecificationStatics {
		/// <summary>
		/// Specify the build number only for prereleases.
		/// </summary>
		public static string GetNuGetPackageFileName( string systemShortName, int majorVersion, int? buildNumber, DateTime? localExportDateAndTime = null ) {
			return GetNuGetPackageId( systemShortName ) + "." + GetNuGetPackageVersionString( majorVersion, buildNumber, localExportDateAndTime: localExportDateAndTime ) +
			       ".nupkg";
		}

		public static string GetNuGetPackageId( string systemShortName ) {
			return systemShortName;
		}

		/// <summary>
		/// Specify the build number only for prereleases.
		/// </summary>
		public static string GetNuGetPackageVersionString( int majorVersion, int? buildNumber, DateTime? localExportDateAndTime = null ) {
			return majorVersion + ".0.0" +
			       ( buildNumber.HasValue
			         	? "-pr" + ( localExportDateAndTime.HasValue ? buildNumber.Value - 1 : buildNumber.Value ).ToString( "d5" ) +
			         	  ( localExportDateAndTime.HasValue ? "-" + localExportDateAndTime.Value.ToString( "yyMMddHHmm" ) : "" )
			         	: "" );
		}
	}
}