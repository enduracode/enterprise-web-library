using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A source-set for an image with a fixed size.
	/// </summary>
	public class FixedSizeImageSourceSet {
		internal readonly Tuple<Func<string>, Func<string>> SrcAndSrcsetGetters;

		/// <summary>
		/// Creates an image source-set.
		/// </summary>
		/// <param name="resourceMaxWidth">The maximum width supported by the image resource, in pixels.</param>
		/// <param name="imageWidth">The width of the image, in CSS pixels. Must be less than or equal to the resource max width.</param>
		/// <param name="resourceGetter">A function that takes a pixel width and returns a representation of the image resource at that width.</param>
		public FixedSizeImageSourceSet( uint resourceMaxWidth, decimal imageWidth, Func<uint, ResourceInfo> resourceGetter ) {
			if( imageWidth > resourceMaxWidth )
				throw new ApplicationException( "The image width must be less than or equal to the resource max width." );
			SrcAndSrcsetGetters = Tuple.Create(
				new Func<string>( () => resourceGetter( (uint)Math.Ceiling( imageWidth ) ).GetUrl() ),
				new Func<string>(
					() => {
						if( (uint)Math.Ceiling( imageWidth ) == resourceMaxWidth )
							return "";

						var highResDevicePixelRatioAndResourceWidthPairs = new List<Tuple<decimal, uint>>();
						for( var devicePixelRatio = 2m; devicePixelRatio <= 4; devicePixelRatio += 1 ) {
							var resourceWidth = (uint)Math.Ceiling( devicePixelRatio * imageWidth );
							if( resourceWidth > resourceMaxWidth )
								break;
							highResDevicePixelRatioAndResourceWidthPairs.Add( Tuple.Create( devicePixelRatio, resourceWidth ) );
						}

						var maxDevicePixelRatio = Math.Round( resourceMaxWidth / imageWidth, 5 );
						if( !highResDevicePixelRatioAndResourceWidthPairs.Any() || maxDevicePixelRatio > highResDevicePixelRatioAndResourceWidthPairs.Last().Item1 )
							highResDevicePixelRatioAndResourceWidthPairs.Add( Tuple.Create( maxDevicePixelRatio, resourceMaxWidth ) );

						return StringTools.ConcatenateWithDelimiter(
							", ",
							highResDevicePixelRatioAndResourceWidthPairs.Select( i => "{0} {1}x".FormatWith( resourceGetter( i.Item2 ).GetUrl(), i.Item1 ) ).ToArray() );
					} ) );
		}
	}
}