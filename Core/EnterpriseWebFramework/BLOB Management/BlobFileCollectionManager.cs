using EnterpriseWebLibrary.DataAccess.BlobStorage;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ComponentDisplay;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ElementBase.Classification;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;
using EnterpriseWebLibrary.IO;
using NodaTime;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A control for managing a collection of files stored in a database.
/// </summary>
public sealed class BlobFileCollectionManager: FlowComponent {
	private readonly IReadOnlyCollection<FlowComponent> children;

	/// <summary>
	/// Creates a file collection manager.
	/// </summary>
	/// <param name="fileCollectionId"></param>
	/// <param name="timeZone"></param>
	/// <param name="displaySetup"></param>
	/// <param name="postBackIdBase">Do not pass null.</param>
	/// <param name="sortByName"></param>
	/// <param name="thumbnailResourceGetter">A function that takes a file ID and returns the corresponding thumbnail resource. Do not return null.</param>
	/// <param name="openedFileIds">The file-collection file IDs that should not be marked with a UI element drawing the user’s attention to the fact that they
	/// haven’t read it. All other files not in this collection will be marked. The collection can be null, and will result as nothing being shown as new.</param>
	/// <param name="unopenedFileOpenedNotifier">A method that executes when an unopened file is opened. Use to update the app’s database with an indication
	/// that the file has been seen by the user.</param>
	/// <param name="disableModifications">Pass true if there should be no way to upload or delete files.</param>
	/// <param name="uploadValidationMethod"></param>
	/// <param name="fileCreatedOrReplacedNotifier">A method that executes after a file is created or replaced.</param>
	/// <param name="filesDeletedNotifier">A method that executes after one or more files are deleted.</param>
	public BlobFileCollectionManager(
		int fileCollectionId, DateTimeZone timeZone, DisplaySetup? displaySetup = null, string postBackIdBase = "", bool sortByName = false,
		Func<int, ResourceInfo>? thumbnailResourceGetter = null, IEnumerable<int>? openedFileIds = null, MarkFileAsReadMethod? unopenedFileOpenedNotifier = null,
		bool disableModifications = false, Action<RsFile?, Validator>? uploadValidationMethod = null,
		NewFileNotificationMethod? fileCreatedOrReplacedNotifier = null, Action? filesDeletedNotifier = null ) {
		postBackIdBase = PostBack.GetCompositeId( "ewfFileCollection", postBackIdBase );

		var columnSetups = new List<EwfTableField>();
		if( thumbnailResourceGetter != null )
			columnSetups.Add( new EwfTableField( size: 10.ToPercentage() ) );
		columnSetups.Add( new EwfTableField( classes: new ElementClass( "ewfOverflowedCell" ) ) );
		columnSetups.Add( new EwfTableField( size: 13.ToPercentage() ) );
		columnSetups.Add( new EwfTableField( size: 7.ToPercentage() ) );

		var table = EwfTable.Create(
			idBase: postBackIdBase,
			caption: "Files",
			selectedItemActions: disableModifications
				                     ? null
				                     : SelectedItemAction.CreateWithFullPostBackBehavior<int>(
						                     "Delete Selected Files",
						                     ids => {
							                     foreach( var i in ids )
								                     BlobStorageStatics.SystemProvider.DeleteFileCollectionFile( i );
							                     filesDeletedNotifier?.Invoke();
							                     PageBase.AddStatusMessage( StatusMessageType.Info, "Selected files deleted successfully." );
						                     } )
					                     .ToCollection(),
			fields: columnSetups );

		var unorderedFiles = BlobStorageStatics.SystemProvider.GetFilesLinkedToFileCollection( fileCollectionId );
		var files = ( sortByName ? unorderedFiles.OrderByName() : unorderedFiles.OrderByUploadTimeDescending() ).Materialize();

		table.AddData( files, file => getFileItem( file, postBackIdBase, thumbnailResourceGetter, timeZone, openedFileIds, unopenedFileOpenedNotifier ) );

		children = files.Any() || !disableModifications
			           ? table.Concat(
					           !disableModifications
						           ? getUploadComponents( fileCollectionId, files, displaySetup, postBackIdBase, uploadValidationMethod, fileCreatedOrReplacedNotifier )
						           : Enumerable.Empty<FlowComponent>() )
				           .Materialize()
			           : Enumerable.Empty<FlowComponent>().Materialize();
	}

	private EwfTableItem getFileItem(
		BlobFileCollectionFile collectionFile, string postBackIdBase, Func<int, ResourceInfo>? thumbnailResourceGetter, DateTimeZone timeZone,
		IEnumerable<int>? openedFileIds, MarkFileAsReadMethod? unopenedFileOpenedNotifier ) {
		var cells = new List<EwfTableCell>();

		var file = BlobStorageStatics.SystemProvider.GetFile( collectionFile.FileId );
		var thumbnailControl = BlobManagementStatics.GetThumbnailControl( file, thumbnailResourceGetter );
		if( thumbnailControl.Any() )
			cells.Add( thumbnailControl.ToCell() );

		var fileIsUnopened = openedFileIds != null && !openedFileIds.Contains( collectionFile.FileCollectionFileId );

		cells.Add(
			new EwfButton(
					new StandardButtonStyle( file.FileName ),
					behavior: new PostBackBehavior(
						postBack: PostBack.CreateFull(
							id: PostBack.GetCompositeId( postBackIdBase, collectionFile.FileCollectionFileId.ToString() ),
							modificationMethod: () => {
								if( fileIsUnopened )
									unopenedFileOpenedNotifier?.Invoke( collectionFile.FileCollectionFileId );
							},
							actionGetter: () => new PostBackAction(
								new PageReloadBehavior( secondaryResponse: new SecondaryResponse( new BlobFileResponse( file.FileId, () => true ), false ) ) ) ) ) )
				.ToCollection()
				.ToCell() );

		cells.Add( file.UploadTime.InZone( timeZone ).Date.ToDayMonthYearString( false ).ToCell() );
		cells.Add( ( fileIsUnopened ? "New!" : "" ).ToCell() );

		return EwfTableItem.Create( cells, setup: EwfTableItemSetup.Create( id: new SpecifiedValue<int>( collectionFile.FileCollectionFileId ) ) );
	}

	private IReadOnlyCollection<FlowComponent> getUploadComponents(
		int fileCollectionId, IReadOnlyCollection<BlobFileCollectionFile> files, DisplaySetup? displaySetup, string postBackIdBase,
		Action<RsFile?, Validator>? uploadValidationMethod, NewFileNotificationMethod? fileCreatedOrReplacedNotifier ) {
		BlobFileCollectionFile? existingFile = null;
		RsFile? newFile = null;
		return FormState.ExecuteWithActions(
			PostBack.CreateFull(
				id: PostBack.GetCompositeId( postBackIdBase, "add" ),
				modificationMethod: () => {
					if( newFile is null )
						return;

					var newFileId = BlobStorageStatics.InsertFile( newFile.FileName, BlobStorageStatics.GetContentTypeForPostedFile( newFile ), newFile.Contents );

					int collectionFileId;
					if( existingFile is null )
						collectionFileId = BlobStorageStatics.SystemProvider.InsertFileCollectionFile( fileCollectionId, newFileId );
					else {
						BlobStorageStatics.SystemProvider.UpdateFileCollectionFile( existingFile.FileCollectionFileId, newFileId );
						collectionFileId = existingFile.FileCollectionFileId;
					}

					fileCreatedOrReplacedNotifier?.Invoke( collectionFileId );
					PageBase.AddStatusMessage( StatusMessageType.Info, "File uploaded successfully." );
				} ),
			() => FormItemList.CreateWrapping( setup: new FormItemListSetup( displaySetup: displaySetup, buttonSetup: new ButtonSetup( "Upload new file" ) ) )
				.AddItem(
					new FileUpload(
						validationMethod: ( postBackValue, validator ) => {
							if( postBackValue is not null )
								existingFile = files.SingleOrDefault( i => BlobStorageStatics.SystemProvider.GetFile( i.FileId ).FileName == postBackValue.FileName );
							newFile = postBackValue;
							uploadValidationMethod?.Invoke( postBackValue, validator );
						} ).ToFormItem( label: "Select a new file:".ToComponents() ) )
				.ToCollection() );
	}

	IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => children;
}