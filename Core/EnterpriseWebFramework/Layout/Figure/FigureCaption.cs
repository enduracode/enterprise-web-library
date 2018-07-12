using System.Collections.Generic;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A caption for a figure.
	/// </summary>
	public class FigureCaption {
		internal class CssElementCreator: ControlCssElementCreator {
			internal static readonly ElementClass Class = new ElementClass( "ewfFig" );

			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
				return new[] { new CssElement( "FigureCaption", "figcaption.{0}".FormatWith( Class.ClassName ) ) };
			}
		}

		internal readonly bool FigureIsTextual;
		internal readonly IReadOnlyCollection<FlowComponent> Components;

		/// <summary>
		/// Creates a caption.
		/// </summary>
		/// <param name="components"></param>
		/// <param name="figureIsTextual">Pass true to place the caption above the content.</param>
		public FigureCaption( IReadOnlyCollection<FlowComponent> components, bool figureIsTextual = false ) {
			FigureIsTextual = figureIsTextual;
			Components = new DisplayableElement(
				context => new DisplayableElementData(
					null,
					() => new DisplayableElementLocalData( "figcaption" ),
					classes: CssElementCreator.Class,
					children: components ) ).ToCollection();
		}
	}
}