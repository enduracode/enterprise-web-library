using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public abstract class StaticFileFolderSetup: EntitySetupBase {
	protected sealed override void init() => base.init();

	protected override string getEntitySetupName() => "";

	public sealed override ResourceBase DefaultResource => throw new NotSupportedException();

	protected sealed override IEnumerable<ResourceGroup> createListedResources() => [ ];

	protected override IReadOnlyCollection<NestedUrl?> getNestedUrls() => [ ];

	public override ConnectionSecurity ConnectionSecurity => Parent?.ConnectionSecurity ?? ConnectionSecurity.MatchingCurrentRequest;

	protected sealed override UrlHandler? getRequestHandler() => null;

	protected sealed override bool canRepresentRequestHandler() => base.canRepresentRequestHandler();

	protected abstract bool isFrameworkFolder { get; }

	protected abstract string folderPath { get; }

	protected sealed override EntitySetupBase reCreate() => this;

	public sealed override bool Equals( BasicUrlHandler? other ) =>
		other is StaticFileFolderSetup otherFs && otherFs.isFrameworkFolder == isFrameworkFolder && otherFs.folderPath == folderPath;

	public sealed override int GetHashCode() => ( isFrameworkFolder, folderPath ).GetHashCode();
}