using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An HTML section.
	/// </summary>
	public class Section: WebControl, ControlTreeDataLoader {
		// This class allows us to use just one selector in the SectionAllStylesBothStates element.
		private const string allStylesBothStatesClass = "ewfSec";

		private const string normalClosedClass = "ewfSecNorClosed";
		private const string normalExpandedClass = "ewfSecNorExpanded";
		private const string boxClosedClass = "ewfSecBoxClosed";
		private const string boxExpandedClass = "ewfSecBoxExpanded";
		private const string headingClass = "ewfSecHeading";
		private const string closeClass = "ewfSecClose";
		private const string expandClass = "ewfSecExpand";
		private const string contentClass = "ewfSecContent";

		internal class CssElementCreator: ControlCssElementCreator {
			CssElement[] ControlCssElementCreator.CreateCssElements() {
				const string normalClosedSelector = "section." + normalClosedClass;
				const string normalExpandedSelector = "section." + normalExpandedClass;
				const string boxClosedSelector = "section." + boxClosedClass;
				const string boxExpandedSelector = "section." + boxExpandedClass;
				return new[]
					{
						new CssElement( "SectionAllStylesBothStates", "section." + allStylesBothStatesClass ),
						new CssElement( "SectionAllStylesClosedState", normalClosedSelector, boxClosedSelector ),
						new CssElement( "SectionAllStylesExpandedState", normalExpandedSelector, boxExpandedSelector ),
						new CssElement( "SectionNormalStyleBothStates", normalClosedSelector, normalExpandedSelector ),
						new CssElement( "SectionNormalStyleClosedState", normalClosedSelector ), new CssElement( "SectionNormalStyleExpandedState", normalExpandedSelector ),
						new CssElement( "SectionBoxStyleBothStates", boxClosedSelector, boxExpandedSelector ), new CssElement( "SectionBoxStyleClosedState", boxClosedSelector ),
						new CssElement( "SectionBoxStyleExpandedState", boxExpandedSelector ), new CssElement( "SectionHeadingContainer", "* > div." + headingClass ),
						new CssElement( "SectionHeading", "h1." + headingClass ), new CssElement( "SectionExpandLabel", "span." + closeClass ),
						new CssElement( "SectionCloseLabel", "span." + expandClass ), new CssElement( "SectionContentContainer", "div." + contentClass )
					};
			}
		}

		private readonly SectionStyle style;
		private readonly string heading;
		private readonly Control[] childControls;
		private readonly bool? expanded;
		private readonly bool disableStatePersistence;

		/// <summary>
		/// Creates a section.
		/// </summary>
		/// <param name="childControls">The section's content.</param>
		/// <param name="style">The section's style.</param>
		public Section( IEnumerable<Control> childControls, SectionStyle style = SectionStyle.Normal ): this( "", childControls, style: style ) {}

		/// <summary>
		/// Creates a section. Do not pass null for the heading.
		/// </summary>
		/// <param name="heading">The section's heading.</param>
		/// <param name="childControls">The section's content.</param>
		/// <param name="style">The section's style.</param>
		/// <param name="expanded">Set to true or false if you want users to be able to expand or close the section by clicking on the heading.</param>
		public Section( string heading, IEnumerable<Control> childControls, SectionStyle style = SectionStyle.Normal, bool? expanded = null )
			: this( style, heading, childControls, expanded, false ) {}

		/// <summary>
		/// Standard library use only.
		/// </summary>
		public Section( SectionStyle style, string heading, IEnumerable<Control> childControls, bool? expanded, bool disableStatePersistence ): base( "section" ) {
			this.style = style;
			this.heading = heading;
			this.childControls = childControls.ToArray();
			this.expanded = expanded;
			this.disableStatePersistence = disableStatePersistence;
		}

		void ControlTreeDataLoader.LoadData() {
			CssClass =
				CssClass.ConcatenateWithSpace(
					allStylesBothStatesClass + " " +
					( style == SectionStyle.Normal ? getSectionClass( normalClosedClass, normalExpandedClass ) : getSectionClass( boxClosedClass, boxExpandedClass ) ) );

			if( heading.Any() ) {
				var headingControl = new WebControl( HtmlTextWriterTag.H1 ) { CssClass = headingClass }.AddControlsReturnThis( heading.GetLiteralControl() );
				if( expanded.HasValue ) {
					var toggleClasses = style == SectionStyle.Normal ? new[] { normalClosedClass, normalExpandedClass } : new[] { boxClosedClass, boxExpandedClass };

					var headingContainer = new Block(
						new EwfLabel { Text = "Click to Expand", CssClass = closeClass },
						new EwfLabel { Text = "Click to Close", CssClass = expandClass },
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

		private string getSectionClass( string closedClass, string expandedClass ) {
			return !expanded.HasValue || expanded.Value ? expandedClass : closedClass;
		}
	}

	[ Obsolete( "Guaranteed through 30 April 2015. Please use the Section control instead." ) ]
	public class Box: Section {
		[ Obsolete( "Guaranteed through 30 April 2015. Please use the Section control instead." ) ]
		public Box( IEnumerable<Control> childControls ): this( "", childControls ) {}

		[ Obsolete( "Guaranteed through 30 April 2015. Please use the Section control instead." ) ]
		public Box( string heading, IEnumerable<Control> childControls, bool? expanded = null ): base( SectionStyle.Box, heading, childControls, expanded, false ) {}
	}
}