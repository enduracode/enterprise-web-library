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
				var lastWriteTime = File.GetLastWriteTimeUtc( FilePath );
				if( lastWriteTime > getBuildDateAndTime() )
					return lastWriteTime;
			}
			return getBuildDateAndTime();
		}

		protected abstract DateTimeOffset getBuildDateAndTime();

		/// <summary>
		/// Gets the path of the file.
		/// </summary>
		internal string FilePath => EwlStatics.CombinePaths( HttpRuntime.AppDomainAppPath, appRelativeFilePath );

		/// <summary>
		/// Gets the app relative path of the file.
		/// </summary>
		protected abstract string appRelativeFilePath { get; }

		protected override bool isIdenticalTo( ResourceBase resourceAsBaseType ) =>
			resourceAsBaseType is StaticFileBase staticFile && staticFile.appRelativeFilePath == appRelativeFilePath;

		public override ResourceBase CloneAndReplaceDefaultsIfPossible( bool disableReplacementOfDefaults ) => this;
	}
}