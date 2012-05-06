using System;
using System.IO;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic.WebItems {
	internal class CssFile {
		private readonly WebItemGeneralData generalData;

		internal CssFile( WebItemGeneralData generalData ) {
			this.generalData = generalData;
		}

		internal void GenerateCode( TextWriter writer ) {
			writer.WriteLine( "namespace " + generalData.Namespace + " {" );
			writer.WriteLine( "public class " + generalData.ClassName + " {" );
			writer.WriteLine( "public sealed class Info: CssInfo {" );
			writeGetUrlMethod( writer );
			writeAppRelativeFilePathProperty( writer );
			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
		}

		private void writeGetUrlMethod( TextWriter writer ) {
			writer.WriteLine( "public override string GetUrl() {" );

			var extensionIndex = generalData.UrlRelativeToProject.LastIndexOf( "." );

			// We could use the last write time of the file for the version string instead of DateTime.Now, but that would prevent re-downloading when we change the
			// expansion of a CSS element without changing the source file.
			writer.WriteLine( "return \"" +
			                  NetTools.CombineUrls( "~",
			                                        generalData.UrlRelativeToProject.Remove( extensionIndex ) + CssHandler.GetFileVersionString( DateTime.Now ) +
			                                        generalData.UrlRelativeToProject.Substring( extensionIndex ) ) + "\";" );

			writer.WriteLine( "}" );
		}

		private void writeAppRelativeFilePathProperty( TextWriter writer ) {
			writer.WriteLine( "protected override string appRelativeFilePath { get { return \"" + generalData.PathRelativeToProject.Replace( "\\", "\\\\" ) + "\"; } }" );
		}
	}
}