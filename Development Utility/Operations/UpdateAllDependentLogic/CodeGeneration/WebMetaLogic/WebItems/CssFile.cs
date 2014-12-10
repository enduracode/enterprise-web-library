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
			writer.WriteLine( "public sealed class Info: StaticCssInfo {" );
			writeBuildUrlMethod( writer );

			// We could use the last write time of the file instead of the current date/time, but that would prevent re-downloading when we change the expansion of a
			// CSS element without changing the source file.
			writer.WriteLine(
				"public override DateTimeOffset GetResourceLastModificationDateAndTime() { return " + AppStatics.GetLiteralDateTimeExpression( DateTimeOffset.UtcNow ) +
				"; }" );

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
				"\" + StaticCssHandler.GetUrlVersionString( GetResourceLastModificationDateAndTime() ) + \"" + generalData.UrlRelativeToProject.Substring( extensionIndex ) +
				"\";" );

			writer.WriteLine( "}" );
		}

		private void writeAppRelativeFilePathProperty( TextWriter writer ) {
			writer.WriteLine( "protected override string appRelativeFilePath { get { return \"" + generalData.PathRelativeToProject.Replace( "\\", "\\\\" ) + "\"; } }" );
		}
	}
}