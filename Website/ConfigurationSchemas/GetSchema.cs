using EnterpriseWebLibrary.Configuration;

// EwlResource
// Parameter: string fileName

namespace EnterpriseWebLibrary.Website.ConfigurationSchemas {
	partial class GetSchema {
		private string filePath;

		protected override void init() {
			filePath = EwlStatics.CombinePaths( ConfigurationStatics.FilesFolderPath, FileName + FileExtensions.Xsd );
			if( !File.Exists( filePath ) )
				throw new ApplicationException( "File does not exist." );
		}

		protected override EwfSafeRequestHandler getOrHead() =>
			new EwfSafeResponseWriter(
				() => EwfResponse.Create( ContentTypes.Xml, new EwfResponseBodyCreator( () => File.ReadAllText( filePath ) ) ),
				EwlStatics.EwlBuildDateTime,
				() => "getSchema" + FileName );
	}
}