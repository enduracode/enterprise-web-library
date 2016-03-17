using System;
using System.IO;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework;

// Parameter: string fileName

namespace EnterpriseWebLibrary.WebSite {
	partial class GetSchema: EwfPage {
		partial class Info {
			internal string FilePath { get; private set; }

			protected override void init() {
				FilePath = EwlStatics.CombinePaths( ConfigurationStatics.FilesFolderPath, FileName + FileExtensions.Xsd );
				if( !File.Exists( FilePath ) )
					throw new ApplicationException( "File does not exist." );
			}

			protected override ConnectionSecurity ConnectionSecurity { get { return ConnectionSecurity.NonSecure; } }
		}

		protected override EwfSafeResponseWriter responseWriter {
			get {
				return new EwfSafeResponseWriter(
					() => EwfResponse.Create( ContentTypes.Xml, new EwfResponseBodyCreator( () => File.ReadAllText( info.FilePath ) ) ),
					EwlStatics.EwlBuildDateTime,
					() => "getSchema" + info.FileName );
			}
		}
	}
}