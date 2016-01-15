using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
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
		private readonly IEnumerable<Control> postHeadingControls;
		private readonly IEnumerable<Control> contentControls;
		private readonly bool? expanded;
		private readonly bool disableStatePersistence;

		/// <summary>
		/// Creates a section.
		/// </summary>
		/// <param name="contentControls">The section's content.</param>
		/// <param name="style">The section's style.</param>
		public Section( IEnumerable<Control> contentControls, SectionStyle style = SectionStyle.Normal ): this( "", contentControls, style: style ) {}

		/// <summary>
		/// Creates a section.
		/// </summary>
		/// <param name="heading">The section's heading. Do not pass null.</param>
		/// <param name="contentControls">The section's content.</param>
		/// <param name="style">The section's style.</param>
		/// <param name="postHeadingControls">Controls that follow the heading but are still part of the heading container.</param>
		/// <param name="expanded">Set to true or false if you want users to be able to expand or close the section by clicking on the heading.</param>
		public Section(
			string heading, IEnumerable<Control> contentControls, SectionStyle style = SectionStyle.Normal, IEnumerable<Control> postHeadingControls = null,
			bool? expanded = null ): this( style, heading, postHeadingControls, contentControls, expanded, false ) {}

		/// <summary>
		/// Standard library use only.
		/// </summary>
		public Section(
			SectionStyle style, string heading, IEnumerable<Control> postHeadingControls, IEnumerable<Control> contentControls, bool? expanded,
			bool disableStatePersistence ): base( "section" ) {
			this.style = style;
			this.heading = heading;
			this.postHeadingControls = postHeadingControls != null ? postHeadingControls.ToArray() : new Control[ 0 ];
			this.contentControls = contentControls != null ? contentControls.ToArray() : new Control[ 0 ];
			this.expanded = expanded;
			this.disableStatePersistence = disableStatePersistence;
		}

		void ControlTreeDataLoader.LoadData() {
			CssClass =
				CssClass.ConcatenateWithSpace(
					allStylesBothStatesClass + " " +
					( style == SectionStyle.Normal ? getSectionClass( normalClosedClass, normalExpandedClass ) : getSectionClass( boxClosedClass, boxExpandedClass ) ) );

			if( heading.Any() ) {
				var headingControls =
					new WebControl( HtmlTextWriterTag.H1 ) { CssClass = headingClass }.AddControlsReturnThis( heading.GetLiteralControl() )
						.ToSingleElementArray()
						.Concat( postHeadingControls );
				if( expanded.HasValue ) {
					var toggleClasses = style == SectionStyle.Normal ? new[] { normalClosedClass, normalExpandedClass } : new[] { boxClosedClass, boxExpandedClass };

					var headingContainer =
						new Block(
							new[] { new EwfLabel { Text = "Click to Expand", CssClass = closeClass }, new EwfLabel { Text = "Click to Close", CssClass = expandClass } }.Concat(
								headingControls ).ToArray() ) { CssClass = headingClass };
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
					var headingContainer = new Block( headingControls.ToArray() ) { CssClass = headingClass };
					this.AddControlsReturnThis( new Block( headingContainer ) );
				}
			}
			if( contentControls.Any() )
				this.AddControlsReturnThis( new Block( contentControls.ToArray() ) { CssClass = contentClass } );
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
		public Box( string heading, IEnumerable<Control> childControls, bool? expanded = null )
			: base( SectionStyle.Box, heading, null, childControls, expanded, false ) {}
	}
}