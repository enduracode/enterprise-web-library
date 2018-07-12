using System.Collections.Generic;
using System.Linq;
using Humanizer;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A div element containing a block of HTML.
	/// </summary>
	public class HtmlBlockContainer: FlowComponent {
		private static readonly ElementClass elementClass = new ElementClass( "ewfHtmlBlock" );

		[ UsedImplicitly ]
		private class CssElementCreator: ControlCssElementCreator {
			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
				return new CssElement( "HtmlBlock", "div.{0}".FormatWith( elementClass.ClassName ) ).ToCollection();
			}
		}

		private readonly string html;
		private readonly IReadOnlyCollection<FlowComponent> children;

		/// <summary>
		/// Creates an HTML block container.
		/// </summary>
		/// <param name="htmlBlockId"></param>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the container.</param>
		public HtmlBlockContainer( int htmlBlockId, DisplaySetup displaySetup = null, ElementClassSet classes = null ): this(
			HtmlBlockStatics.GetHtml( htmlBlockId ),
			displaySetup: displaySetup,
			classes: classes ) {}

		/// <summary>
		/// Creates an HTML block container. Do not pass null for HTML. This overload is useful when you've already loaded the HTML.
		/// </summary>
		/// <param name="html"></param>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the container.</param>
		public HtmlBlockContainer( string html, DisplaySetup displaySetup = null, ElementClassSet classes = null ) {
			this.html = html;
			children = new DisplayableElement(
				context => new DisplayableElementData(
					displaySetup,
					() => new DisplayableElementLocalData( "div" ),
					classes: elementClass.Add( classes ?? ElementClassSet.Empty ),
					children: new TrustedHtmlString( html ).ToComponent().ToCollection() ) ).ToCollection();
		}

		/// <summary>
		/// Gets whether the HTML block has HTML (i.e. is not empty).
		/// </summary>
		public bool HasHtml => html.Any();

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}