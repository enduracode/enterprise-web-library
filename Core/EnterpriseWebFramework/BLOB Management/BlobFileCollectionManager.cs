using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.DataAccess.BlobStorage;
using EnterpriseWebLibrary.IO;
using EnterpriseWebLibrary.WebSessionState;
using Tewl.InputValidation;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A control for managing a collection of files stored in a database.
	/// </summary>
	public sealed class BlobFileCollectionManager: FlowComponent {
		private readonly IReadOnlyCollection<FlowComponent> children;

		/// <summary>
		/// Creates a file collection manager.
		/// </summary>
		/// <param name="fileCollectionId"></param>
		/// <param name="displaySetup"></param>
		/// <param name="postBackIdBase">Do not pass null.</param>
		/// <param name="sortByName"></param>
		/// <param name="thumbnailResourceGetter">A function that takes a file ID and returns the corresponding thumbnail resource. Do not return null.</param>
		/// <param name="openedFileIds">The file IDs that should not be marked with a UI element drawing the user’s attention to the fact that they haven’t read it.
		/// All other files not in this collection will be marked. The collection can be null, and will result as nothing being shown as new.</param>
		/// <param name="unopenedFileOpenedNotifier">A method that executes when an unopened file is opened. Use to update the app’s database with an indication
		/// that the file has been seen by the user.</param>
		/// <param name="disableModifications">Pass true if there should be no way to upload or delete files.</param>
		/// <param name="uploadValidationMethod"></param>
		/// <param name="fileCreatedOrReplacedNotifier">A method that executes after a file is created or replaced.</param>
		/// <param name="filesDeletedNotifier">A method that executes after one or more files are deleted.</param>
		public BlobFileCollectionManager(
			int fileCollectionId, DisplaySetup displaySetup = null, string postBackIdBase = "", bool sortByName = false,
			Func<int, ResourceInfo> thumbnailResourceGetter = null, IEnumerable<int> openedFileIds = null, MarkFileAsReadMethod unopenedFileOpenedNotifier = null,
			bool disableModifications = false, Action<RsFile, Validator> uploadValidationMethod = null,
			NewFileNotificationMethod fileCreatedOrReplacedNotifier = null, Action filesDeletedNotifier = null ) {
			postBackIdBase = PostBack.GetCompositeId( "ewfFileCollection", postBackIdBase );

			var columnSetups = new List<EwfTableField>();
			if( thumbnailResourceGetter != null )
				columnSetups.Add( new EwfTableField( size: 10.ToPercentage() ) );
			columnSetups.Add( new EwfTableField( classes: new ElementClass( "ewfOverflowedCell" ) ) );
			columnSetups.Add( new EwfTableField( size: 13.ToPercentage() ) );
			columnSetups.Add( new EwfTableField( size: 7.ToPercentage() ) );

			var table = EwfTable.Create(
				postBackIdBase: postBackIdBase,
				caption: "Files",
				selectedItemActions: disableModifications
					                     ? null
					                     : SelectedItemAction.CreateWithFullPostBackBehavior<int>(
							                     "Delete Selected Files",
							                     ids => {
								                     foreach( var i in ids )
									                     BlobStorageStatics.SystemProvider.DeleteFile( i );
								                     filesDeletedNotifier?.Invoke();
								                     EwfPage.AddStatusMessage( StatusMessageType.Info, "Selected files deleted successfully." );
							                     } )
						                     .ToCollection(),
				fields: columnSetups );

			IReadOnlyCollection<BlobFile> files = BlobStorageStatics.SystemProvider.GetFilesLinkedToFileCollection( fileCollectionId );
			files = ( sortByName ? files.OrderByName() : files.OrderByUploadedDateDescending() ).Materialize();

			foreach( var file in files )
				addFileRow( postBackIdBase, thumbnailResourceGetter, openedFileIds, unopenedFileOpenedNotifier, table, file );

			children = files.Any() || !disableModifications
				           ? table.Concat(
						           !disableModifications
							           ? getUploadComponents( fileCollectionId, files, displaySetup, postBackIdBase, uploadValidationMethod, fileCreatedOrReplacedNotifier )
							           : Enumerable.Empty<FlowComponent>() )
					           .Materialize()
				           : Enumerable.Empty<FlowComponent>().Materialize();
		}

		private void addFileRow(
			string postBackIdBase, Func<int, ResourceInfo> thumbnailResourceGetter, IEnumerable<int> openedFileIds, MarkFileAsReadMethod unopenedFileOpenedNotifier,
			EwfTable table, BlobFile file ) {
			var cells = new List<EwfTableCell>();

			var thumbnailControl = BlobManagementStatics.GetThumbnailControl( file, thumbnailResourceGetter );
			if( thumbnailControl.Any() )
				cells.Add( thumbnailControl.ToCell() );

			var fileIsUnopened = openedFileIds != null && !openedFileIds.Contains( file.FileId );

			cells.Add(
				new EwfButton(
						new StandardButtonStyle( file.FileName ),
						behavior: new PostBackBehavior(
							postBack: PostBack.CreateFull(
								id: PostBack.GetCompositeId( postBackIdBase, file.FileId.ToString() ),
								firstModificationMethod: () => {
									if( fileIsUnopened )
										unopenedFileOpenedNotifier?.Invoke( file.FileId );
								},
								actionGetter: () => new PostBackAction(
									new PageReloadBehavior( secondaryResponse: new SecondaryResponse( new BlobFileResponse( file.FileId, () => true ), false ) ) ) ) ) )
					.ToCollection()
					.ToCell() );

			cells.Add( file.UploadedDate.ToDayMonthYearString( false ).ToCell() );
			cells.Add( ( fileIsUnopened ? "New!" : "" ).ToCell() );

			table.AddItem( EwfTableItem.Create( cells, setup: EwfTableItemSetup.Create( id: new SpecifiedValue<int>( file.FileId ) ) ) );
		}

		private IReadOnlyCollection<FlowComponent> getUploadComponents(
			int fileCollectionId, IReadOnlyCollection<BlobFile> files, DisplaySetup displaySetup, string postBackIdBase,
			Action<RsFile, Validator> uploadValidationMethod, NewFileNotificationMethod fileCreatedOrReplacedNotifier ) {
			RsFile file = null;
			var dm = PostBack.CreateFull(
				id: PostBack.GetCompositeId( postBackIdBase, "add" ),
				firstModificationMethod: () => {
					if( file == null )
						return;

					var existingFile = files.SingleOrDefault( i => i.FileName == file.FileName );
					int newFileId;
					if( existingFile != null ) {
						BlobStorageStatics.SystemProvider.UpdateFile(
							existingFile.FileId,
							file.FileName,
							file.Contents,
							BlobStorageStatics.GetContentTypeForPostedFile( file ) );
						newFileId = existingFile.FileId;
					}
					else
						newFileId = BlobStorageStatics.SystemProvider.InsertFile(
							fileCollectionId,
							file.FileName,
							file.Contents,
							BlobStorageStatics.GetContentTypeForPostedFile( file ) );

					fileCreatedOrReplacedNotifier?.Invoke( newFileId );
					EwfPage.AddStatusMessage( StatusMessageType.Info, "File uploaded successfully." );
				} );
			return FormState.ExecuteWithDataModificationsAndDefaultAction(
				dm.ToCollection(),
				() => new StackList(
						new FileUpload(
								validationMethod: ( postBackValue, validator ) => {
									file = postBackValue;
									uploadValidationMethod?.Invoke( postBackValue, validator );
								} ).ToFormItem()
							.ToListItem()
							.Append( new EwfButton( new StandardButtonStyle( "Upload new file" ) ).ToCollection().ToComponentListItem() ) ).ToFormItem(
						setup: new FormItemSetup( displaySetup: displaySetup ),
						label: "Select and upload a new file:".ToComponents() )
					.ToComponentCollection() );
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => children;
	}
}