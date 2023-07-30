using EnterpriseWebLibrary.DataAccess.BlobStorage;
using EnterpriseWebLibrary.IO;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A control for managing a single file stored in a database.
/// </summary>
public sealed class BlobFileManager: FlowComponent {
	private readonly IReadOnlyCollection<FlowComponent> children;

	public BlobFileManager(
		int? fileCollectionId, bool requireUploadIfNoFile, Action<int> idSetter, out Action modificationMethod, BlobFileManagerSetup? setup = null ) {
		setup ??= BlobFileManagerSetup.Create();
		var file = fileCollectionId != null ? BlobStorageStatics.GetFirstFileFromCollection( fileCollectionId.Value ) : null;

		var components = new List<FlowComponent>();
		if( file != null ) {
			var download = new EwfButton(
				new StandardButtonStyle( Translation.DownloadExisting + " (" + file.FileName + ")", buttonSize: ButtonSize.ShrinkWrap ),
				behavior: new PostBackBehavior(
					postBack: PostBack.CreateFull(
						id: PostBack.GetCompositeId( "ewfFile", file.FileId.ToString() ),
						actionGetter: () => {
							// Refresh the file here in case a new one was uploaded on the same post-back.
							return new PostBackAction(
								new PageReloadBehavior(
									secondaryResponse: new SecondaryResponse(
										new BlobFileResponse( BlobStorageStatics.GetFirstFileFromCollection( fileCollectionId.Value ).FileId, () => true ),
										false ) ) );
						} ) ) );
			components.Add( download );
		}
		else if( !setup.OmitNoExistingFileMessage )
			components.Add( new GenericPhrasingContainer( Translation.NoExistingFile.ToComponents() ) );

		RsFile uploadedFile = null;
		var fileUploadDisplayedPmv = new PageModificationValue<string>();
		components.AddRange(
			new FileUpload(
					displaySetup: fileUploadDisplayedPmv.ToCondition( bool.TrueString.ToCollection() ).ToDisplaySetup(),
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
					} ).ToFormItem()
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

		modificationMethod = () => {
			if( fileCollectionId == null )
				fileCollectionId = BlobStorageStatics.SystemProvider.InsertFileCollection();

			if( uploadedFile != null ) {
				BlobStorageStatics.SystemProvider.DeleteFilesLinkedToFileCollection( fileCollectionId.Value );
				BlobStorageStatics.SystemProvider.InsertFile(
					fileCollectionId.Value,
					uploadedFile.FileName,
					uploadedFile.Contents,
					BlobStorageStatics.GetContentTypeForPostedFile( uploadedFile ) );
			}

			idSetter( fileCollectionId.Value );
		};
	}

	IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => children;
}