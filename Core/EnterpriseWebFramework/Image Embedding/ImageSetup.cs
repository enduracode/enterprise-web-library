using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The general configuration for an image.
	/// </summary>
	public class ImageSetup {
		/// <summary>
		/// EWL use only.
		/// </summary>
		public class CssElementCreator: ControlCssElementCreator {
			internal static readonly ElementClass Class = new ElementClass( "ewfImage" );

			/// <summary>
			/// EWL use only.
			/// </summary>
			public static readonly IReadOnlyCollection<string> Selectors = "img.{0}".FormatWith( Class.ClassName ).ToCollection();

			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
				return new[] { new CssElement( "Image", Selectors.ToArray() ) };
			}
		}

		internal readonly Func<Func<string>, Func<string>, IReadOnlyCollection<FlowComponentOrNode>> ComponentGetter;

		/// <summary>
		/// Creates an image setup object.
		/// </summary>
		/// <param name="alternativeText">The alternative text for the image; see https://html.spec.whatwg.org/multipage/embedded-content.html#alt. Pass null (which
		/// omits the alt attribute) or the empty string only when the specification allows.</param>
		/// <param name="displaySetup"></param>
		/// <param name="sizesToAvailableWidth">Whether the image sizes itself to fit all available width.</param>
		/// <param name="classes">The classes on the image.</param>
		public ImageSetup( string alternativeText, DisplaySetup displaySetup = null, bool sizesToAvailableWidth = false, ElementClassSet classes = null ) {
			ComponentGetter = ( srcGetter, srcsetGetter ) => {
				return new DisplayableElement(
					context => new DisplayableElementData(
						           displaySetup,
						           () => {
							           var attributes = new List<Tuple<string, string>>();
							           attributes.Add( Tuple.Create( "src", srcGetter() ) );
							           var srcset = srcsetGetter();
							           if( srcset.Any() )
								           attributes.Add( Tuple.Create( "srcset", srcset ) );
							           if( alternativeText != null )
								           attributes.Add( Tuple.Create( "alt", alternativeText ) );

							           return new DisplayableElementLocalData(
								           "img",
								           classes:
									           CssElementCreator.Class.Union( sizesToAvailableWidth ? new ElementClass( "ewfAutoSizer" ) : ElementClassSet.Empty )
										           .Union( classes ?? ElementClassSet.Empty ),
								           additionalAttributes: attributes );
						           } ) ).ToCollection();
			};
		}
	}
}