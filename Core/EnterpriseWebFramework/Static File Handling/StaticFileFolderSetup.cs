using System;
using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public abstract class StaticFileFolderSetup: EntitySetupBase {
		protected readonly Lazy<StaticFileFolderSetup> parentFolderSetup;

		protected StaticFileFolderSetup() {
			parentFolderSetup = new Lazy<StaticFileFolderSetup>( createParentFolderSetup );
		}

		protected sealed override void init() => base.init();

		protected override ResourceBase createParentResource() => parentFolderSetup.Value?.ParentResource;

		protected abstract StaticFileFolderSetup createParentFolderSetup();

		public sealed override string EntitySetupName => "";

		protected internal sealed override void InitParametersModification() {}

		protected sealed override IEnumerable<ResourceGroup> createListedResources() => Enumerable.Empty<ResourceGroup>();

		protected internal override ConnectionSecurity ConnectionSecurity => ParentResource?.ConnectionSecurity ?? ConnectionSecurity.MatchingCurrentRequest;

		protected sealed override UrlHandler getRequestHandler() => null;

		protected sealed override bool canRepresentRequestHandler() => base.canRepresentRequestHandler();

		protected abstract bool isFrameworkFolder { get; }

		protected abstract string folderPath { get; }

		public sealed override bool Equals( BasicUrlHandler other ) =>
			other is StaticFileFolderSetup otherFs && otherFs.isFrameworkFolder == isFrameworkFolder && otherFs.folderPath == folderPath;

		public sealed override int GetHashCode() => ( isFrameworkFolder, folderPath ).GetHashCode();
	}
}