using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.DataAccess.BlobStorage;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.InputValidation;
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
		private Action<Validator, System.Drawing.Image> validateImage = delegate {};
		private IEnumerable<BlobFile> files;
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
		/// Set this if you want to perform custom validation on any (renderable) images added to this manager.
		/// </summary>
		public Action<Validator, System.Drawing.Image> ValidateImage { private get { return validateImage; } set { validateImage = value; } }

		/// <summary>
		/// Set the method to execute when a new file is uploaded.
		/// </summary>
		public NewFileNotificationMethod NewFileNotificationMethod { private get; set; }

		/// <summary>
		/// Sets the method used to get thumbnail URLs for files with the image content type. The method takes a file ID and returns a resource info object.
		/// </summary>
		public Func<decimal, ResourceInfo> ThumbnailResourceInfoCreator { private get; set; }

		/// <summary>
		/// Creates a file collection manager.
		/// </summary>
		/// <param name="sortByName"></param>
		/// <param name="postBackIdBase">Do not pass null.</param>
		public BlobFileCollectionManager( bool sortByName = false, string postBackIdBase = "" ) {
			Caption = "";
			this.sortByName = sortByName;
			this.postBackIdBase = PostBack.GetCompositeId( "ewfFileCollection", postBackIdBase );

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

					var columnSetups = new List<ColumnSetup>();
					if( ThumbnailResourceInfoCreator != null )
						columnSetups.Add( new ColumnSetup { Width = Unit.Percentage( 10 ) } );
					columnSetups.Add( new ColumnSetup { CssClassOnAllCells = "ewfOverflowedCell" } );
					columnSetups.Add( new ColumnSetup { Width = Unit.Percentage( 13 ) } );
					columnSetups.Add( new ColumnSetup { Width = Unit.Percentage( 7 ) } );
					columnSetups.Add( new ColumnSetup { Width = Unit.Percentage( 23 ), CssClassOnAllCells = "ewfRightAlignCell" } );

					var table = new DynamicTable( columnSetups.ToArray() ) { Caption = Caption };

					files = BlobStorageStatics.SystemProvider.GetFilesLinkedToFileCollection( fileCollectionId );
					files = ( sortByName ? files.OrderByName() : files.OrderByUploadedDateDescending() ).ToArray();

					var deleteModMethods = new List<Func<bool>>();
					var deletePb = PostBack.CreateFull(
						id: PostBack.GetCompositeId( postBackIdBase, "delete" ),
						firstModificationMethod: () => {
							if( deleteModMethods.Aggregate( false, ( deletesOccurred, method ) => method() || deletesOccurred ) )
								EwfPage.AddStatusMessage( StatusMessageType.Info, "Selected files deleted successfully." );
						} );
					FormState.ExecuteWithDataModificationsAndDefaultAction(
						deletePb.ToCollection(),
						() => {
							foreach( var file in files )
								addFileRow( table, file, deleteModMethods );
							if( !ReadOnly )
								table.AddRow(
									getUploadControlList().ToCell( new TableCellSetup( fieldSpan: ThumbnailResourceInfoCreator != null ? 3 : 2 ) ),
									( files.Any() ? new PostBackButton( new ButtonActionControlStyle( "Delete Selected Files" ), usesSubmitBehavior: false ) : null ).ToCell(
										new TableCellSetup( fieldSpan: 2, classes: "ewfRightAlignCell".ToCollection() ) ) );
						} );

					Controls.Add( table );

					if( ReadOnly && !files.Any() )
						Visible = false;
				} );
		}

		private void addFileRow( DynamicTable table, BlobFile file, List<Func<bool>> deleteModMethods ) {
			var cells = new List<EwfTableCell>();

			var thumbnailControl = BlobManagementStatics.GetThumbnailControl( file, ThumbnailResourceInfoCreator ).ToImmutableArray();
			if( thumbnailControl.Any() )
				cells.Add( thumbnailControl.ToCell() );

			var fileIsUnread = fileIdsMarkedAsRead != null && !fileIdsMarkedAsRead.Contains( file.FileId );

			cells.Add(
				new PostBackButton(
					new TextActionControlStyle( file.FileName ),
					usesSubmitBehavior: false,
					postBack: PostBack.CreateFull(
						id: PostBack.GetCompositeId( postBackIdBase, file.FileId.ToString() ),
						firstModificationMethod: () => {
							if( fileIsUnread )
								markFileAsReadMethod?.Invoke( file.FileId );
						},
						actionGetter: () => new PostBackAction(
							new PageReloadBehavior( secondaryResponse: new SecondaryResponse( new BlobFileResponse( file.FileId, () => true ), false ) ) ) ) )
					{
						ToolTip = file.FileName
					} );

			cells.Add( file.UploadedDate.ToDayMonthYearString( false ) );
			cells.Add( ( fileIsUnread ? "New!" : "" ).ToCell( new TableCellSetup( classes: "ewfNewness".ToCollection() ) ) );

			var delete = false;
			var deleteCheckBox = FormItem.Create(
					"",
					new EwfCheckBox( false ),
					validationGetter: control => new EwfValidation( ( pbv, v ) => { delete = control.IsCheckedInPostBack( pbv ); } ) )
				.ToControl();
			cells.Add( ReadOnly ? null : deleteCheckBox );
			deleteModMethods.Add(
				() => {
					if( !delete )
						return false;
					BlobStorageStatics.SystemProvider.DeleteFile( file.FileId );
					return true;
				} );

			table.AddRow( cells.ToArray() );
		}

		private ControlList getUploadControlList() {
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

					NewFileNotificationMethod?.Invoke( newFileId );
					EwfPage.AddStatusMessage( StatusMessageType.Info, "File uploaded successfully." );
				} );
			return FormState.ExecuteWithDataModificationsAndDefaultAction(
				dm.ToCollection(),
				() => {
					var fi = new FileUpload(
						validationMethod: ( postBackValue, validator ) => {
							BlobManagementStatics.ValidateUploadedFile( validator, postBackValue, acceptableFileExtensions, ValidateImage, AcceptOnlyImages );
							file = postBackValue;
						} ).ToFormItem();

					return ControlList.CreateWithControls(
						true,
						"Select and upload a new file:",
						fi.ToControl(),
						new PostBackButton( new ButtonActionControlStyle( "Upload new file" ), usesSubmitBehavior: false ) );
				} );
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