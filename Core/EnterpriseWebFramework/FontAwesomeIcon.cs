using System.Collections.Generic;
using System.Linq;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class FontAwesomeIcon: PhrasingComponent {
		internal class CssElementCreator: ControlCssElementCreator {
			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
				return new[] { new CssElement( "Icon", "span.fa" ) };
			}
		}

		private readonly IReadOnlyCollection<GenericPhrasingContainer> children;

		/// <summary>
		/// Creates a Font Awesome icon.
		/// </summary>
		/// <param name="iconName">The name of the icon. Do not pass null or the empty string.</param>
		/// <param name="additionalClasses">Additional classes that will be added to the icon element.</param>
		public FontAwesomeIcon( string iconName, params string[] additionalClasses ) {
			children = new GenericPhrasingContainer(
				null,
				classes: additionalClasses.Aggregate(
					new ElementClass( "fa" ).Add( new ElementClass( iconName ) ),
					( set, additionalClass ) => set.Add( new ElementClass( additionalClass ) ) ) ).ToCollection();
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}