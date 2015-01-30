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
		private const string closedClass = "ewfSecClosed";
		private const string expandedClass = "ewfSecExpanded";
		private const string headingClass = "ewfSecHeading";
		private const string contentClass = "ewfSecContent";

		internal class CssElementCreator: ControlCssElementCreator {
			CssElement[] ControlCssElementCreator.CreateCssElements() {
				const string closedSelector = "section." + closedClass;
				const string expandedSelector = "section." + expandedClass;
				return new[]
					{
						new CssElement( "SectionBothStates", closedSelector, expandedSelector ), new CssElement( "SectionClosedState", closedSelector ),
						new CssElement( "SectionExpandedState", expandedSelector ), new CssElement( "SectionHeadingContainer", "* > div." + headingClass ),
						new CssElement( "SectionHeading", "h1." + headingClass ), new CssElement( "SectionExpandLabel", "span." + closedClass ),
						new CssElement( "SectionCloseLabel", "span." + expandedClass ), new CssElement( "SectionContentContainer", "div." + contentClass )
					};
			}
		}

		private readonly string heading;
		private readonly Control[] childControls;
		private readonly bool? expanded;
		private readonly bool disableStatePersistence;

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
		public Box( string heading, IEnumerable<Control> childControls, bool? expanded = null ): this( heading, childControls, expanded, false ) {}

		/// <summary>
		/// Standard library use only.
		/// </summary>
		public Box( string heading, IEnumerable<Control> childControls, bool? expanded, bool disableStatePersistence ): base( "section" ) {
			this.heading = heading;
			this.childControls = childControls.ToArray();
			this.expanded = expanded;
			this.disableStatePersistence = disableStatePersistence;
		}

		void ControlTreeDataLoader.LoadData() {
			CssClass = CssClass.ConcatenateWithSpace( !expanded.HasValue || expanded.Value ? expandedClass : closedClass );

			if( heading.Any() ) {
				var headingControl = new WebControl( HtmlTextWriterTag.H1 ) { CssClass = headingClass }.AddControlsReturnThis( heading.GetLiteralControl() );
				if( expanded.HasValue ) {
					var toggleClasses = new[] { closedClass, expandedClass };

					var headingContainer = new Block(
						new EwfLabel { Text = "Click to Expand", CssClass = closedClass },
						new EwfLabel { Text = "Click to Close", CssClass = expandedClass },
						headingControl ) { CssClass = headingClass };
					var actionControlStyle = new CustomActionControlStyle( c => c.AddControlsReturnThis( headingContainer ) );

					this.AddControlsReturnThis(
						disableStatePersistence
							? new CustomButton( () => "$( '#" + ClientID + "' ).toggleClass( '" + StringTools.ConcatenateWithDelimiter( " ", toggleClasses ) + "', 200 )" )
								{
									ActionControlStyle = actionControlStyle
								}
							: new ToggleButton( this.ToSingleElementArray(), actionControlStyle, toggleClasses: toggleClasses ) as Control );
				}
				else {
					var headingContainer = new Block( headingControl ) { CssClass = headingClass };
					this.AddControlsReturnThis( new Block( headingContainer ) );
				}
			}
			this.AddControlsReturnThis( new Block( childControls ) { CssClass = contentClass } );
		}
	}
}