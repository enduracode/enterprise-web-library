using System.IO;
using System.Text;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.InputValidation;
using EnterpriseWebLibrary.IO;
using Humanizer;

namespace EnterpriseWebLibrary.WebSite {
	partial class CreateSystem: EwfPage {
		partial class Info {
			public override string ResourceName { get { return "Create a New {0} System".FormatWith( EwlStatics.EwlInitialism ); } }
		}

		private readonly DataValue<string> systemName = new DataValue<string>();
		private readonly DataValue<string> systemShortName = new DataValue<string>();
		private readonly DataValue<string> baseNamespace = new DataValue<string>();

		protected override void loadData() {
			var pb =
				PostBack.CreateFull(
					actionGetter:
						() =>
						new PostBackAction(
							new SecondaryResponse(
							() =>
							new EwfResponse(
								ContentTypes.ApplicationZip,
								new EwfResponseBodyCreator( createAndZipSystem ),
								fileNameCreator: () => "{0}.zip".FormatWith( systemShortName.Value ) ) ) ) );

			ph.AddControlsReturnThis(
				FormItemBlock.CreateFormItemTable(
					formItems: new[]
						{
							FormItem.Create(
								"System name",
								new EwfTextBox( "" ),
								validationGetter: control => new EwfValidation(
									                             ( pbv, validator ) => {
										                             systemName.Value = validator.GetString(
											                             new ValidationErrorHandler( "system name" ),
											                             control.GetPostBackValue( pbv ),
											                             false,
											                             50 );
										                             if( systemName.Value != systemName.Value.RemoveNonAlphanumericCharacters( preserveWhiteSpace: true ) )
											                             validator.NoteErrorAndAddMessage( "The system name must consist of only alphanumeric characters and white space." );
										                             systemShortName.Value = systemName.Value.EnglishToPascal();
									                             },
									                             pb ) ),
							FormItem.Create(
								"Base namespace",
								new EwfTextBox( "" ),
								validationGetter: control => new EwfValidation(
									                             ( pbv, validator ) => {
										                             baseNamespace.Value = validator.GetString(
											                             new ValidationErrorHandler( "base namespace" ),
											                             control.GetPostBackValue( pbv ),
											                             false,
											                             50 );
										                             if( baseNamespace.Value != EwlStatics.GetCSharpIdentifierSimple( baseNamespace.Value ) )
											                             validator.NoteErrorAndAddMessage( "The base namespace must be a valid C# identifier." );
									                             },
									                             pb ) )
						} ) );

			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "Create System", new PostBackButton( pb ) ) );
		}

		private void createAndZipSystem( Stream stream ) {
			IoMethods.ExecuteWithTempFolder(
				folderPath => {
					createSystemFilesInFolder( EwlStatics.CombinePaths( ConfigurationStatics.FilesFolderPath, "System Template" ), folderPath, "" );
					ZipOps.ZipFolderAsStream( folderPath, stream );
				} );
		}

		private void createSystemFilesInFolder( string templateFolderPath, string tempFolderPath, string relativeFolderPath ) {
			var sourceFolderPath = EwlStatics.CombinePaths( templateFolderPath, relativeFolderPath );
			foreach( var fileName in IoMethods.GetFileNamesInFolder( sourceFolderPath ) ) {
				var filePath = EwlStatics.CombinePaths( relativeFolderPath, fileName );
				var destinationFilePath = EwlStatics.CombinePaths( tempFolderPath, filePath == "Solution.sln" ? "{0}.sln".FormatWith( systemName.Value ) : filePath );
				Directory.CreateDirectory( Path.GetDirectoryName( destinationFilePath ) );
				File.WriteAllText(
					destinationFilePath,
					File.ReadAllText( EwlStatics.CombinePaths( templateFolderPath, filePath ), Encoding.UTF8 )
						.Replace( "@@SystemName", systemName.Value )
						.Replace( "@@SystemShortNameLowercase", systemShortName.Value.ToLowerInvariant() )
						.Replace( "@@SystemShortName", systemShortName.Value )
						.Replace( "@@BaseNamespace", baseNamespace.Value ),
					Encoding.UTF8 );
			}
			foreach( var subFolderName in IoMethods.GetFolderNamesInFolder( sourceFolderPath ) )
				createSystemFilesInFolder( templateFolderPath, tempFolderPath, Path.Combine( relativeFolderPath, subFolderName ) );
		}
	}
}