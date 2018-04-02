using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using MoreLinq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An HTML section.
	/// </summary>
	// When we convert this to a component, uncomment and fix SectionErrorDisplayStyle.
	public sealed class Section: WebControl {
		// This class allows us to use just one selector in the SectionAllStylesBothStates element.
		private const string allStylesBothStatesClass = "ewfSec";

		private const string normalClosedClass = "ewfSecNorClosed";
		private const string normalExpandedClass = "ewfSecNorExpanded";
		private const string boxClosedClass = "ewfSecBoxClosed";
		private const string boxExpandedClass = "ewfSecBoxExpanded";
		private const string headingClass = "ewfSecHeading";
		private static readonly ElementClass closeClass = new ElementClass( "ewfSecClose" );
		private static readonly ElementClass expandClass = new ElementClass( "ewfSecExpand" );
		private const string contentClass = "ewfSecContent";

		internal class CssElementCreator: ControlCssElementCreator {
			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
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
						new CssElement( "SectionHeading", "h1." + headingClass ), new CssElement( "SectionExpandLabel", "span." + closeClass.ClassName ),
						new CssElement( "SectionCloseLabel", "span." + expandClass.ClassName ), new CssElement( "SectionContentContainer", "div." + contentClass )
					};
			}
		}

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
			postHeadingControls = postHeadingControls?.ToArray() ?? new Control[ 0 ];
			contentControls = contentControls?.ToArray() ?? new Control[ 0 ];

			CssClass = CssClass.ConcatenateWithSpace(
				allStylesBothStatesClass + " " + ( style == SectionStyle.Normal
					                                   ? getSectionClass( expanded, normalClosedClass, normalExpandedClass )
					                                   : getSectionClass( expanded, boxClosedClass, boxExpandedClass ) ) );

			if( heading.Any() ) {
				var headingControls = new WebControl( HtmlTextWriterTag.H1 ) { CssClass = headingClass }.AddControlsReturnThis( heading.ToComponents().GetControls() )
					.ToCollection()
					.Concat( postHeadingControls );
				if( expanded.HasValue ) {
					var toggleClasses = style == SectionStyle.Normal ? new[] { normalClosedClass, normalExpandedClass } : new[] { boxClosedClass, boxExpandedClass };

					var headingContainer =
						new Block(
							new GenericPhrasingContainer( "Click to Expand".ToComponents(), classes: closeClass ).ToCollection()
								.Concat( new GenericPhrasingContainer( "Click to Close".ToComponents(), classes: expandClass ) )
								.GetControls()
								.Concat( headingControls )
								.ToArray() ) { CssClass = headingClass };
					var actionControlStyle = new CustomActionControlStyle( c => c.AddControlsReturnThis( headingContainer ) );

					this.AddControlsReturnThis(
						disableStatePersistence
							? new CustomButton( () => "$( '#" + ClientID + "' ).toggleClass( '" + StringTools.ConcatenateWithDelimiter( " ", toggleClasses ) + "', 200 )" )
								{
									ActionControlStyle = actionControlStyle
								}
							: new ToggleButton( this.ToCollection(), actionControlStyle, false, ( postBackValue, validator ) => {}, toggleClasses: toggleClasses ) as Control );
				}
				else {
					var headingContainer = new Block( headingControls.ToArray() ) { CssClass = headingClass };
					this.AddControlsReturnThis( new Block( headingContainer ) );
				}
			}
			if( contentControls.Any() )
				this.AddControlsReturnThis( new Block( contentControls.ToArray() ) { CssClass = contentClass } );
		}

		private string getSectionClass( bool? expanded, string closedClass, string expandedClass ) {
			return !expanded.HasValue || expanded.Value ? expandedClass : closedClass;
		}
	}
}