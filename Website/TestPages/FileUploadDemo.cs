// EwlPage

namespace EnterpriseWebLibrary.Website.TestPages;

partial class FileUploadDemo {
	protected override string getResourceName() => "File Upload";

	protected override PageContent getContent() =>
		new UiPageContent( isAutoDataUpdater: true ).Add(
			FormItemList.CreateStack(
				items: new FileUpload(
						validationMethod: ( value, _ ) => AddStatusMessage(
							StatusMessageType.Info,
							value != null ? "File size: {0} bytes".FormatWith( value.Contents.Length ) : "No file uploaded." ) )
					.ToFormItem( label: "File upload".ToComponents() )
					.ToCollection() ) );
}