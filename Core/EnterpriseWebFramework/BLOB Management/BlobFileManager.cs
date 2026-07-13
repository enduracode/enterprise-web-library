using EnterpriseWebLibrary.DataAccess.BlobStorage;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ComponentDisplay;
using EnterpriseWebLibrary.IO;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A control for managing a single file stored in a database.
/// </summary>
public sealed class BlobFileManager: FlowComponent {
	private readonly IReadOnlyCollection<FlowComponent> children;

	public BlobFileManager( int? fileId, bool requireUploadIfNoFile, Action<int?> idSetter, out Action modificationMethod, BlobFileManagerSetup? setup = null ) {
		setup ??= BlobFileManagerSetup.Create();
		var file = fileId.HasValue ? BlobStorageStatics.SystemProvider.GetFile( fileId.Value ) : null;

		var components = new List<FlowComponent>();
		if( fileId.HasValue )
			components.AddRange( BlobManagementStatics.GetFileButton( fileId.Value, labelOverride: Translation.DownloadExisting + " (" + file!.FileName + ")" ) );
		else if( !setup.OmitNoExistingFileMessage )
			components.Add( new GenericPhrasingContainer( Translation.NoExistingFile.ToComponents() ) );

		RsFile? uploadedFile = null;
		var fileUploadDisplayedPmv = new PageModificationValue<string>();
		components.AddRange(
			new FileUpload(
					validationPredicate: setup.UploadValidationPredicate,
					validationErrorNotifier: setup.UploadValidationErrorNotifier,
					validationMethod: ( postBackValue, validator ) => {
						if( requireUploadIfNoFile && file == null && postBackValue == null ) {
							validator.NoteErrorAndAddMessage( Translation.PleaseUploadAFile );
							setup.UploadValidationErrorNotifier?.Invoke();
							return;
						}

						uploadedFile = postBackValue;
						setup.UploadValidationMethod?.Invoke( postBackValue, validator );
					} ).ToFormItem(
					setup: new FormItemSetup( displaySetup: fileUploadDisplayedPmv.ToCondition( bool.TrueString.ToCollection() ).ToDisplaySetup() ),
					label: "Select a new file:".ToComponents() )
				.ToComponentCollection() );
		var fileUploadDisplayedHiddenFieldId = new HiddenFieldId();
		if( file != null )
			components.Add(
				new EwfButton(
					new StandardButtonStyle( Translation.ClickHereToReplaceExistingFile, buttonSize: ButtonSize.ShrinkWrap ),
					displaySetup: fileUploadDisplayedPmv.ToCondition( bool.FalseString.ToCollection() ).ToDisplaySetup(),
					behavior: new ChangeValueBehavior( fileUploadDisplayedHiddenFieldId, bool.TrueString ) ) );

		children = new GenericFlowContainer(
			BlobManagementStatics.GetThumbnailControl( file, setup.ThumbnailResourceGetter )
				.Append<FlowComponent>( new StackList( from i in components select i.ToCollection().ToComponentListItem() ) )
				.Materialize(),
			displaySetup: setup.DisplaySetup,
			classes: setup.Classes,
			etherealContent: new EwfHiddenField( ( file == null ).ToString(), id: fileUploadDisplayedHiddenFieldId, pageModificationValue: fileUploadDisplayedPmv )
				.PageComponent.ToCollection() ).ToCollection();

		modificationMethod = () => idSetter(
			uploadedFile is null
				? fileId
				: BlobStorageStatics.InsertFile( uploadedFile.FileName, BlobStorageStatics.GetContentTypeForPostedFile( uploadedFile ), uploadedFile.Contents ) );
	}

	IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => children;
}