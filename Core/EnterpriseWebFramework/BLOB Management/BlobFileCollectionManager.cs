using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.DataAccess.BlobStorage;
using EnterpriseWebLibrary.IO;
using EnterpriseWebLibrary.WebSessionState;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A control for managing a collection of files stored in a database.
	/// </summary>
	public class BlobFileCollectionManager: WebControl, INamingContainer, ControlTreeDataLoader {
		private int fileCollectionId;
		private MarkFileAsReadMethod markFileAsReadMethod;
		private IEnumerable<int> fileIdsMarkedAsRead;
		private string[] acceptableFileExtensions;
		private readonly bool sortByName;
		private readonly string postBackIdBase;
		private readonly NewFileNotificationMethod fileCreatedOrReplacedNotifier;
		private readonly Action filesDeletedNotifier;
		private IReadOnlyCollection<BlobFile> files;
		private readonly IReadOnlyCollection<DataModification> dataModifications;

		/// <summary>
		/// Sets the caption on the file table. Do not set this to null.
		/// </summary>
		public string Caption { private get; set; }

		/// <summary>
		/// True if there should be no way to upload or delete files.
		/// </summary>
		public bool ReadOnly { private get; set; }

		/// <summary>
		/// True if this file collection manager can only accept images (of any renderable type - jpgs, pngs, but not nefs) for its files.
		/// </summary>
		public bool AcceptOnlyImages { get; set; }

		/// <summary>
		/// Sets the method used to get thumbnail URLs for files with the image content type. The method takes a file ID and returns a resource info object.
		/// </summary>
		public Func<int, ResourceInfo> ThumbnailResourceInfoCreator { private get; set; }

		/// <summary>
		/// Creates a file collection manager.
		/// </summary>
		/// <param name="sortByName"></param>
		/// <param name="postBackIdBase">Do not pass null.</param>
		/// <param name="fileCreatedOrReplacedNotifier">A method that executes after a file is created or replaced.</param>
		/// <param name="filesDeletedNotifier">A method that executes after one or more files are deleted.</param>
		public BlobFileCollectionManager(
			bool sortByName = false, string postBackIdBase = "", NewFileNotificationMethod fileCreatedOrReplacedNotifier = null,
			Action filesDeletedNotifier = null ) {
			Caption = "";
			this.sortByName = sortByName;
			this.postBackIdBase = PostBack.GetCompositeId( "ewfFileCollection", postBackIdBase );
			this.fileCreatedOrReplacedNotifier = fileCreatedOrReplacedNotifier;
			this.filesDeletedNotifier = filesDeletedNotifier;

			dataModifications = FormState.Current.DataModifications;
		}

		/// <summary>
		/// Call this during LoadData.
		/// </summary>
		public void LoadData( int fileCollectionId ) {
			LoadData( fileCollectionId, null, null );
		}

		/// <summary>
		/// Call this during LoadData.  The markFileAsReadMethod can be used to update the app's database with an indication that the file has been seen by the user.
		/// The fileIdsMarkedAsRead collection indicates which files should be not marked with a UI element drawing the user's attention to the fact that they haven't read it.
		/// All other files not in this collection will be marked. FileIdsMarkedAsRead can be null, and will result as nothing being shown as new.
		/// </summary>
		public void LoadData( int fileCollectionId, MarkFileAsReadMethod markFileAsReadMethod, IEnumerable<int> fileIdsMarkedAsRead ) {
			this.fileCollectionId = fileCollectionId;
			this.markFileAsReadMethod = markFileAsReadMethod;
			this.fileIdsMarkedAsRead = fileIdsMarkedAsRead;
		}

		void ControlTreeDataLoader.LoadData() {
			FormState.ExecuteWithDataModificationsAndDefaultAction(
				dataModifications,
				() => {
					CssClass = CssClass.ConcatenateWithSpace( "ewfStandardFileCollectionManager" );

					if( AppRequestState.Instance.Browser.IsInternetExplorer() )
						Controls.Add(
							new HtmlGenericControl( "p" )
								{
									InnerText =
										"Because you are using Internet Explorer, clicking on a file below will result in a yellow warning bar appearing near the top of the browser.  You will need to then click the warning bar and tell Internet Explorer you are sure you want to download the file."
								} );

					var columnSetups = new List<EwfTableField>();
					if( ThumbnailResourceInfoCreator != null )
						columnSetups.Add( new EwfTableField( size: 10.ToPercentage() ) );
					columnSetups.Add( new EwfTableField( classes: new ElementClass( "ewfOverflowedCell" ) ) );
					columnSetups.Add( new EwfTableField( size: 13.ToPercentage() ) );
					columnSetups.Add( new EwfTableField( size: 7.ToPercentage() ) );

					var table = EwfTable.Create(
						postBackIdBase: postBackIdBase,
						caption: Caption,
						selectedItemActions: ReadOnly
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

					files = BlobStorageStatics.SystemProvider.GetFilesLinkedToFileCollection( fileCollectionId );
					files = ( sortByName ? files.OrderByName() : files.OrderByUploadedDateDescending() ).Materialize();

					foreach( var file in files )
						addFileRow( table, file );
					if( !ReadOnly )
						table.AddItem(
							EwfTableItem.Create( getUploadComponents().ToCell( new TableCellSetup( fieldSpan: ThumbnailResourceInfoCreator != null ? 4 : 3 ) ) ) );

					this.AddControlsReturnThis( table.ToCollection().GetControls() );

					if( ReadOnly && !files.Any() )
						Visible = false;
				} );
		}

		private void addFileRow( EwfTable table, BlobFile file ) {
			var cells = new List<EwfTableCell>();

			var thumbnailControl = BlobManagementStatics.GetThumbnailControl( file, ThumbnailResourceInfoCreator );
			if( thumbnailControl.Any() )
				cells.Add( thumbnailControl.ToCell() );

			var fileIsUnread = fileIdsMarkedAsRead != null && !fileIdsMarkedAsRead.Contains( file.FileId );

			cells.Add(
				new EwfButton(
						new StandardButtonStyle( file.FileName ),
						behavior: new PostBackBehavior(
							postBack: PostBack.CreateFull(
								id: PostBack.GetCompositeId( postBackIdBase, file.FileId.ToString() ),
								firstModificationMethod: () => {
									if( fileIsUnread )
										markFileAsReadMethod?.Invoke( file.FileId );
								},
								actionGetter: () => new PostBackAction(
									new PageReloadBehavior( secondaryResponse: new SecondaryResponse( new BlobFileResponse( file.FileId, () => true ), false ) ) ) ) ) )
					.ToCollection()
					.ToCell() );

			cells.Add( file.UploadedDate.ToDayMonthYearString( false ).ToCell() );
			cells.Add( ( fileIsUnread ? "New!" : "" ).ToCell( new TableCellSetup( classes: "ewfNewness".ToCollection() ) ) );

			table.AddItem( EwfTableItem.Create( cells, setup: EwfTableItemSetup.Create( id: new SpecifiedValue<int>( file.FileId ) ) ) );
		}

		private IReadOnlyCollection<FlowComponent> getUploadComponents() {
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
					"Select and upload a new file:".ToComponents()
						.ToComponentListItem()
						.ToCollection()
						.Append(
							new FileUpload(
									validationMethod: ( postBackValue, validator ) => {
										BlobManagementStatics.ValidateUploadedFile( validator, postBackValue, acceptableFileExtensions, AcceptOnlyImages );
										file = postBackValue;
									} ).ToFormItem()
								.ToListItem() )
						.Append( new EwfButton( new StandardButtonStyle( "Upload new file" ) ).ToCollection().ToComponentListItem() ) ).ToCollection() );
		}

		/// <summary>
		/// Prevents the user from uploading a file of a type other than those provided. File type constants found in EnterpriseWebLibrary.FileExtensions.
		/// Do not use this to force the file to be a specific type of file, such as an image (which consists of several file extensions).
		/// Instead, use AcceptOnlyImages.
		/// </summary>
		public void SetFileTypeFilter( params string[] acceptableFileTypes ) {
			acceptableFileExtensions = acceptableFileTypes;
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey => HtmlTextWriterTag.Div;
	}
}