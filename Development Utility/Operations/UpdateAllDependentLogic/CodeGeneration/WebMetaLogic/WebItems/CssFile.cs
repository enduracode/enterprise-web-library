using System;
using System.IO;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic.WebItems {
	internal class CssFile {
		private readonly WebItemGeneralData generalData;

		internal CssFile( WebItemGeneralData generalData ) {
			this.generalData = generalData;
		}

		internal void GenerateCode( TextWriter writer ) {
			writer.WriteLine( "namespace " + generalData.Namespace + " {" );
			writer.WriteLine( "public class " + generalData.ClassName + " {" );
			writer.WriteLine( "public sealed class Info: StaticFileInfo {" );
			writeBuildUrlMethod( writer );

			writer.WriteLine(
				"protected override DateTimeOffset getBuildDateAndTime() { return " + AppStatics.GetLiteralDateTimeExpression( DateTimeOffset.UtcNow ) + "; }" );

			writeAppRelativeFilePathProperty( writer );
			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
		}

		private void writeBuildUrlMethod( TextWriter writer ) {
			writer.WriteLine( "protected override string buildUrl() {" );

			var extensionIndex = generalData.UrlRelativeToProject.LastIndexOf( "." );
			writer.WriteLine(
				"return \"~/" + generalData.UrlRelativeToProject.Remove( extensionIndex ) +
				"\" + StaticFileHandler.GetUrlVersionString( GetResourceLastModificationDateAndTime() ) + \"" + generalData.UrlRelativeToProject.Substring( extensionIndex ) +
				"\";" );

			writer.WriteLine( "}" );
		}

		private void writeAppRelativeFilePathProperty( TextWriter writer ) {
			writer.WriteLine( "protected override string appRelativeFilePath { get { return \"" + generalData.PathRelativeToProject.Replace( "\\", "\\\\" ) + "\"; } }" );
		}
	}
}