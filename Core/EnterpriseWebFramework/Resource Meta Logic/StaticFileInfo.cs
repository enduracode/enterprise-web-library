using System;
using System.IO;
using System.Web;
using EnterpriseWebLibrary.Configuration;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A base set of functionality that can be used to discover information about a static file before actually requesting it.
	/// </summary>
	public abstract class StaticFileInfo: ResourceInfo {
		/// <summary>
		/// EWL use only.
		/// </summary>
		public override EntitySetupInfo EsInfoAsBaseType { get { return null; } }

		/// <summary>
		/// EWL use only.
		/// </summary>
		public override string ResourceName { get { return ""; } }

		protected override bool isIdenticalTo( ResourceInfo infoAsBaseType ) {
			var info = infoAsBaseType as StaticFileInfo;
			return info != null && info.appRelativeFilePath == appRelativeFilePath;
		}

		protected internal override ResourceInfo CloneAndReplaceDefaultsIfPossible( bool disableReplacementOfDefaults ) {
			return this;
		}

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
		internal string FilePath { get { return EwlStatics.CombinePaths( HttpRuntime.AppDomainAppPath, appRelativeFilePath ); } }

		/// <summary>
		/// Gets the app relative path of the file.
		/// </summary>
		protected abstract string appRelativeFilePath { get; }
	}
}