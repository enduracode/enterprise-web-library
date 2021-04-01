using System;
using System.IO;
using Humanizer;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic.WebItems {
	internal class StaticFile {
		private readonly WebItemGeneralData generalData;
		private readonly bool isFrameworkFile;
		private readonly bool isVersioned;
		private readonly string folderSetupClassName;

		internal StaticFile( WebItemGeneralData generalData, bool isFrameworkFile, bool isVersioned, string folderSetupClassName ) {
			this.generalData = generalData;
			this.isFrameworkFile = isFrameworkFile;
			this.isVersioned = isVersioned;
			this.folderSetupClassName = folderSetupClassName;
		}

		internal void GenerateCode( TextWriter writer ) {
			writer.WriteLine( "namespace {0} {{".FormatWith( generalData.Namespace ) );
			writer.WriteLine( "public sealed partial class {0}: StaticFileBase {{".FormatWith( generalData.ClassName ) );

			if( isVersioned )
				writer.WriteLine( "public {0}(): base( true ) {{}}".FormatWith( generalData.ClassName ) );
			else
				writer.WriteLine( "public {0}( bool disableVersioning = false ): base( !disableVersioning ) {{}}".FormatWith( generalData.ClassName ) );

			writer.WriteLine( "public override EntitySetupBase EsAsBaseType => new {0}();".FormatWith( folderSetupClassName ) );
			writer.WriteLine( "protected override UrlEncoder getUrlEncoder() => null;" );
			writer.WriteLine(
				"protected override DateTimeOffset getBuildDateAndTime() => {0};".FormatWith( AppStatics.GetLiteralDateTimeExpression( DateTimeOffset.UtcNow ) ) );
			writer.WriteLine( "protected override bool isFrameworkFile => {0};".FormatWith( isFrameworkFile ? "true" : "false" ) );
			writer.WriteLine( "protected override string relativeFilePath => @\"{0}\";".FormatWith( generalData.PathRelativeToProject ) );

			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
		}
	}
}