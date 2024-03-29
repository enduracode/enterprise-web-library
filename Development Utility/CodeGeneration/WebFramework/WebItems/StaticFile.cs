﻿using System;
using System.IO;
using System.Linq;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.WebFramework.WebItems {
	internal class StaticFile {
		private readonly WebItemGeneralData generalData;
		private readonly bool inFramework;
		private readonly bool inVersionedFolder;
		private readonly string folderSetupClassName;

		internal StaticFile( WebItemGeneralData generalData, bool inFramework, bool inVersionedFolder, string folderSetupClassName ) {
			this.generalData = generalData;
			this.inFramework = inFramework;
			this.inVersionedFolder = inVersionedFolder;
			this.folderSetupClassName = folderSetupClassName;
		}

		internal void GenerateCode( TextWriter writer ) {
			writer.WriteLine( "namespace {0} {{".FormatWith( generalData.Namespace ) );
			writer.WriteLine( "public sealed partial class {0}: StaticFile {{".FormatWith( generalData.ClassName ) );

			UrlStatics.GenerateUrlClasses(
				writer,
				generalData.ClassName,
				null,
				Enumerable.Empty<WebItemParameter>().Materialize(),
				Enumerable.Empty<WebItemParameter>().Materialize(),
				!inVersionedFolder );
			writer.WriteLine( "private readonly {0} folderSetup;".FormatWith( folderSetupClassName ) );

			if( inVersionedFolder )
				writer.WriteLine( "public {0}(): base( true ) {{".FormatWith( generalData.ClassName ) );
			else
				writer.WriteLine( "public {0}( bool disableVersioning = false ): base( !disableVersioning ) {{".FormatWith( generalData.ClassName ) );
			writer.WriteLine( "folderSetup = new {0}();".FormatWith( folderSetupClassName ) );
			writer.WriteLine( "}" );

			writer.WriteLine( "public override EntitySetupBase EsAsBaseType => folderSetup;" );
			UrlStatics.GenerateGetEncoderMethod(
				writer,
				"",
				Enumerable.Empty<WebItemParameter>().Materialize(),
				Enumerable.Empty<WebItemParameter>().Materialize(),
				p => "false",
				!inVersionedFolder );
			writer.WriteLine(
				"protected override DateTimeOffset getBuildDateAndTime() => {0};".FormatWith( AppStatics.GetLiteralDateTimeExpression( DateTimeOffset.UtcNow ) ) );
			writer.WriteLine( "protected override bool isFrameworkFile => {0};".FormatWith( inFramework ? "true" : "false" ) );
			writer.WriteLine(
				"protected override string relativeFilePath => @\"{0}\";".FormatWith(
					generalData.PathRelativeToProject.Substring(
						( ( inFramework
							    ? EnterpriseWebFramework.StaticFile.FrameworkStaticFilesSourceFolderPath
							    : EnterpriseWebFramework.StaticFile.AppStaticFilesFolderName ) + Path.DirectorySeparatorChar ).Length ) ) );

			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
		}
	}
}