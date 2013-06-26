using System;
using System.IO;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.WebFileSending;

// Parameter: string fileName

namespace EnterpriseWebLibrary.WebSite {
	public partial class GetSchema: EwfPage {
		partial class Info {
			internal string FilePath { get; private set; }

			protected override void init() {
				FilePath = StandardLibraryMethods.CombinePaths( AppTools.FilesFolderPath, FileName + FileExtensions.Xsd );
				if( !File.Exists( FilePath ) )
					throw new ApplicationException( "File does not exist." );
			}

			protected override ConnectionSecurity ConnectionSecurity { get { return ConnectionSecurity.NonSecure; } }
		}

		protected override FileCreator fileCreator { get { return new FileCreator( () => new FileToBeSent( info.FileName + FileExtensions.Xsd, ContentTypes.Xml, File.ReadAllText( info.FilePath ) ) ); } }
		protected override bool sendsFileInline { get { return true; } }
	}
}