using System;
using System.Web;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A base set of functionality that can be used to discover information about a static CSS file before actually requesting it.
	/// </summary>
	public abstract class StaticCssInfo: ResourceInfo {
		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public override EntitySetupInfo EsInfoAsBaseType { get { return null; } }

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public override string ResourceName { get { return ""; } }

		protected override bool isIdenticalTo( ResourceInfo infoAsBaseType ) {
			var info = infoAsBaseType as StaticCssInfo;
			return info != null && info.appRelativeFilePath == appRelativeFilePath;
		}

		protected internal override ResourceInfo CloneAndReplaceDefaultsIfPossible( bool disableReplacementOfDefaults ) {
			return this;
		}

		/// <summary>
		/// Gets the last-modification date/time of the resource.
		/// </summary>
		public abstract DateTimeOffset GetResourceLastModificationDateAndTime();

		/// <summary>
		/// Gets the path of the CSS file.
		/// </summary>
		internal string FilePath { get { return StandardLibraryMethods.CombinePaths( HttpRuntime.AppDomainAppPath, appRelativeFilePath ); } }

		/// <summary>
		/// Gets the app relative path of the CSS file.
		/// </summary>
		protected abstract string appRelativeFilePath { get; }
	}
}