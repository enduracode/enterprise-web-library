using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A captioned rectangular area that distinguishes itself from its surroundings.
	/// </summary>
	public class Box: WebControl, ControlTreeDataLoader {
		private const string closedClass = "ewfBoxC";
		private const string expandedClass = "ewfBoxE";
		private const string headingAndContentClass = "ewfBox";

		internal class CssElementCreator: ControlCssElementCreator {
			CssElement[] ControlCssElementCreator.CreateCssElements() {
				const string closedSelector = "section." + closedClass;
				const string expandedSelector = "section." + expandedClass;
				return new[]
					{
						new CssElement( "BoxBothStates", closedSelector, expandedSelector ), new CssElement( "BoxClosedState", closedSelector ),
						new CssElement( "BoxExpandedState", expandedSelector ), new CssElement( "BoxHeadingAllLevels", "* > h1." + headingAndContentClass ),
						new CssElement( "BoxExpandLabel", "* > span." + closedClass ), new CssElement( "BoxCloseLabel", "* > span." + expandedClass ),
						new CssElement( "BoxContentBlock", "div." + headingAndContentClass )
					};
			}
		}

		private readonly string heading;
		private readonly Control[] childControls;
		private readonly bool? expanded;

		/// <summary>
		/// Creates a box.
		/// </summary>
		/// <param name="childControls">The box content.</param>
		public Box( IEnumerable<Control> childControls ): this( "", childControls ) {}

		/// <summary>
		/// Creates a box. Do not pass null for the heading.
		/// </summary>
		/// <param name="heading">The box heading.</param>
		/// <param name="childControls">The box content.</param>
		/// <param name="expanded">Set to true or false if you want users to be able to expand or close the box by clicking on the heading.</param>
		public Box( string heading, IEnumerable<Control> childControls, bool? expanded = null ): base( "section" ) {
			this.heading = heading;
			this.childControls = childControls.ToArray();
			this.expanded = expanded;
		}

		void ControlTreeDataLoader.LoadData() {
			CssClass = CssClass.ConcatenateWithSpace( !expanded.HasValue || expanded.Value ? expandedClass : closedClass );

			var contentBlock = new Block( childControls ) { CssClass = headingAndContentClass };
			if( heading.Length > 0 ) {
				var headingControl = new WebControl( HtmlTextWriterTag.H1 ) { CssClass = headingAndContentClass }.AddControlsReturnThis( heading.GetLiteralControl() );
				this.AddControlsReturnThis(
					expanded.HasValue
						? new ToggleButton(
							  this.ToSingleElementArray(),
							  new CustomActionControlStyle(
							  c =>
							  c.AddControlsReturnThis(
								  new EwfLabel { Text = "Click to Expand", CssClass = closedClass },
								  new EwfLabel { Text = "Click to Close", CssClass = expandedClass },
								  headingControl ) ),
							  toggleClasses: new[] { closedClass, expandedClass } ) as Control
						: new Block( headingControl ) );
			}
			this.AddControlsReturnThis( contentBlock );
		}
	}
}