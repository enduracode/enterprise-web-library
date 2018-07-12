using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An image.
	/// </summary>
	public class EwfImage: PhrasingComponent {
		private readonly IReadOnlyCollection<FlowComponent> children;

		/// <summary>
		/// Creates a fixed-size image.
		/// </summary>
		/// <param name="generalSetup">The general setup object for the image.</param>
		/// <param name="sourceSet">The source-set. Do not pass null.</param>
		public EwfImage( ImageSetup generalSetup, FixedSizeImageSourceSet sourceSet ) {
			children = generalSetup.ComponentGetter( sourceSet.SrcAndSrcsetGetters.Item1, sourceSet.SrcAndSrcsetGetters.Item2 );
		}

		/// <summary>
		/// Creates a flexible image.
		/// </summary>
		/// <param name="generalSetup">The general setup object for the image.</param>
		/// <param name="sourceSet">The source-set. Do not pass null.</param>
		public EwfImage( ImageSetup generalSetup, FlexibleImageSourceSet sourceSet ) {
			// Reimplement with srcset width descriptors and the sizes attribute if needed.
			children = generalSetup.ComponentGetter( sourceSet.SrcAndSrcsetGetters.Item1, sourceSet.SrcAndSrcsetGetters.Item2 );
		}

		/// <summary>
		/// Creates an image with a single source. Use only when you do not know the width of the image resource, or if the resource only supports a single width.
		/// </summary>
		/// <param name="generalSetup">The general setup object for the image.</param>
		/// <param name="imageResource">Do not pass null.</param>
		public EwfImage( ImageSetup generalSetup, ResourceInfo imageResource ) {
			children = generalSetup.ComponentGetter( () => imageResource.GetUrl(), () => "" );
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}