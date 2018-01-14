using System.Collections.Generic;
using System.Linq;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An HTML figure. See https://html.spec.whatwg.org/multipage/semantics.html#the-figure-element.
	/// </summary>
	public class EwfFigure: FlowComponent {
		internal class CssElementCreator: ControlCssElementCreator {
			internal static readonly ElementClass Class = new ElementClass( "ewfFig" );

			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
				return new[] { new CssElement( "Figure", "figure.{0}".FormatWith( Class.ClassName ) ) };
			}
		}

		private readonly IReadOnlyCollection<FlowComponent> children;

		/// <summary>
		/// Creates a figure.
		/// </summary>
		/// <param name="content"></param>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the figure.</param>
		/// <param name="caption">The caption.</param>
		public EwfFigure( IEnumerable<FlowComponent> content, DisplaySetup displaySetup = null, ElementClassSet classes = null, FigureCaption caption = null ) {
			children = new DisplayableElement(
					context => new DisplayableElementData(
						displaySetup,
						() => new DisplayableElementLocalData( "figure" ),
						classes: CssElementCreator.Class.Add( classes ?? ElementClassSet.Empty ),
						children: caption != null ? caption.FigureIsTextual ? caption.Components.Concat( content ) : content.Concat( caption.Components ) : content ) )
				.ToCollection();
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}