using System.IO;
using RedStapler.StandardLibrary;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic.WebItems {
	internal class UserControl {
		private readonly WebItemGeneralData generalData;

		internal UserControl( WebItemGeneralData generalData ) {
			this.generalData = generalData;
		}

		internal void GenerateCode( TextWriter writer ) {
			writer.WriteLine( "namespace " + generalData.Namespace + " {" );
			writer.WriteLine( "public partial class " + generalData.ClassName + " {" );
			writeLoadThisMethod( writer );
			generalData.ReadPageStateVariablesFromCodeAndWriteTypedPageStateMethods( writer );
			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
		}

		private void writeLoadThisMethod( TextWriter writer ) {
			writer.WriteLine( "public static " + generalData.ClassName + " LoadThis() {" );
			writer.WriteLine( "return (" + generalData.ClassName + ")( (Page)HttpContext.Current.CurrentHandler ).LoadControl( \"" +
			                  NetTools.CombineUrls( "~", generalData.UrlRelativeToProject ) + "\" );" );
			writer.WriteLine( "}" );
		}
	}
}