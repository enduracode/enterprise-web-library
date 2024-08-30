using EnterpriseWebLibrary.Configuration.Providers;

// EwlPage

namespace EnterpriseWebLibrary.Website.WebFrameworkDemo;

partial class FileManagers {
	protected override PageContent getContent() =>
		new UiPageContent( omitContentBox: true )
			.Add(
				new Section(
					"File manager",
					new BlobFileManager( BlobStorage.FileManagerCollectionId, false, _ => {}, out _ ).ToCollection(),
					style: SectionStyle.Box ) )
			.Add(
				new Section(
					"File-collection manager",
					new BlobFileCollectionManager( BlobStorage.FileCollectionManagerCollectionId ).ToCollection(),
					style: SectionStyle.Box ) );
}