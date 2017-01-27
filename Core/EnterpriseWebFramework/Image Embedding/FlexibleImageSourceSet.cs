using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A source-set for an image whose size depends on the viewport size.
	/// </summary>
	public class FlexibleImageSourceSet {
		internal readonly Tuple<Func<string>, Func<string>> SrcAndSrcsetGetters;

		/// <summary>
		/// Creates an image source-set.
		/// </summary>
		/// <param name="resourceMaxWidth">The maximum width supported by the image resource, in pixels.</param>
		/// <param name="resourceGetter">A function that takes a pixel width and returns a representation of the image resource at that width.</param>
		public FlexibleImageSourceSet( uint resourceMaxWidth, Func<uint, ResourceInfo> resourceGetter ) {
			SrcAndSrcsetGetters = Tuple.Create( new Func<string>( () => resourceGetter( resourceMaxWidth ).GetUrl() ), new Func<string>( () => "" ) );
		}
	}
}