using System;
using System.IO;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.TewlContrib;
using Tewl;

// EwlResource
// Parameter: string fileName

namespace EnterpriseWebLibrary.WebSite.ConfigurationSchemas {
	partial class GetSchema {
		internal string FilePath { get; private set; }

		protected override void init() {
			FilePath = EwlStatics.CombinePaths( ConfigurationStatics.FilesFolderPath, FileName + FileExtensions.Xsd );
			if( !File.Exists( FilePath ) )
				throw new ApplicationException( "File does not exist." );
		}

		protected override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.NonSecure;

		protected override EwfSafeRequestHandler getOrHead() =>
			new EwfSafeResponseWriter(
				() => EwfResponse.Create( ContentTypes.Xml, new EwfResponseBodyCreator( () => File.ReadAllText( FilePath ) ) ),
				EwlStatics.EwlBuildDateTime,
				() => "getSchema" + FileName );
	}
}