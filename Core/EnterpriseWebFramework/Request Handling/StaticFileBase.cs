using System;
using System.IO;
using System.Web;
using EnterpriseWebLibrary.Configuration;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A static file in a web application.
	/// </summary>
	public abstract class StaticFileBase: ResourceBase {
		/// <summary>
		/// Development Utility and private use only.
		/// </summary>
		public const string FrameworkStaticFilesSourceFolderPath = @"EnterpriseWebFramework\Static Files";

		/// <summary>
		/// Development Utility and private use only.
		/// </summary>
		public const string AppStaticFilesFolderName = "Static Files";

		/// <summary>
		/// EWL use only.
		/// </summary>
		public override EntitySetupBase EsAsBaseType => null;

		/// <summary>
		/// EWL use only.
		/// </summary>
		public override string ResourceName => "";

		protected internal override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;

		/// <summary>
		/// Gets the last-modification date/time of the resource.
		/// </summary>
		public DateTimeOffset GetResourceLastModificationDateAndTime() {
			// The build date/time is an important factor here. Exclusively using the last write time of the file would prevent re-downloading when we change the
			// expansion of a CSS element without changing the source file. And for non-development installations, we don't use the last write time at all because
			// it's probably much slower (the build date/time is just a literal) and also because we don't expect files to be modified on servers.
			if( ConfigurationStatics.IsDevelopmentInstallation ) {
				var lastWriteTime = File.GetLastWriteTimeUtc( filePath );
				if( lastWriteTime > getBuildDateAndTime() )
					return lastWriteTime;
			}
			return getBuildDateAndTime();
		}

		protected abstract DateTimeOffset getBuildDateAndTime();

		/// <summary>
		/// Gets the path of the file.
		/// </summary>
		private string filePath =>
			isFrameworkFile
				? ConfigurationStatics.InstallationConfiguration.SystemIsEwl && ConfigurationStatics.IsDevelopmentInstallation
					  ?
					  EwlStatics.CombinePaths(
						  ConfigurationStatics.InstallationConfiguration.InstallationPath,
						  EwlStatics.CoreProjectName,
						  FrameworkStaticFilesSourceFolderPath )
					  : EwlStatics.CombinePaths(
						  ConfigurationStatics.InstallationConfiguration.InstallationPath,
						  InstallationFileStatics.WebFrameworkStaticFilesFolderName )
				: EwlStatics.CombinePaths( HttpRuntime.AppDomainAppPath, AppStaticFilesFolderName, relativeFilePath );

		/// <summary>
		/// Gets whether the file is part of the framework.
		/// </summary>
		protected abstract bool isFrameworkFile { get; }

		/// <summary>
		/// Gets the relative path of the file.
		/// </summary>
		protected abstract string relativeFilePath { get; }

		protected override bool isIdenticalTo( ResourceBase resourceAsBaseType ) =>
			resourceAsBaseType is StaticFileBase staticFile && staticFile.isFrameworkFile == isFrameworkFile && staticFile.relativeFilePath == relativeFilePath;

		public override ResourceBase CloneAndReplaceDefaultsIfPossible( bool disableReplacementOfDefaults ) => this;
	}
}