using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using Humanizer;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An HTML section.
	/// </summary>
	public sealed class Section: FlowComponent {
		// This class allows us to use just one selector in the SectionAllStylesBothStates element.
		private static readonly ElementClass allStylesBothStatesClass = new ElementClass( "ewfSec" );

		private static readonly ElementClass normalClosedClass = new ElementClass( "ewfSecNorClosed" );
		private static readonly ElementClass normalExpandedClass = new ElementClass( "ewfSecNorExpanded" );
		private static readonly ElementClass boxClosedClass = new ElementClass( "ewfSecBoxClosed" );
		private static readonly ElementClass boxExpandedClass = new ElementClass( "ewfSecBoxExpanded" );
		private static readonly ElementClass headingClass = new ElementClass( "ewfSecHeading" );
		private static readonly ElementClass closeClass = new ElementClass( "ewfSecClose" );
		private static readonly ElementClass expandClass = new ElementClass( "ewfSecExpand" );
		private static readonly ElementClass contentClass = new ElementClass( "ewfSecContent" );

		[ UsedImplicitly ]
		private class CssElementCreator: ControlCssElementCreator {
			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
				var normalClosedSelector = "section." + normalClosedClass.ClassName;
				var normalExpandedSelector = "section." + normalExpandedClass.ClassName;
				var boxClosedSelector = "section." + boxClosedClass.ClassName;
				var boxExpandedSelector = "section." + boxExpandedClass.ClassName;
				return new[]
					{
						new CssElement( "SectionAllStylesBothStates", "section." + allStylesBothStatesClass.ClassName ),
						new CssElement( "SectionAllStylesClosedState", normalClosedSelector, boxClosedSelector ),
						new CssElement( "SectionAllStylesExpandedState", normalExpandedSelector, boxExpandedSelector ),
						new CssElement( "SectionNormalStyleBothStates", normalClosedSelector, normalExpandedSelector ),
						new CssElement( "SectionNormalStyleClosedState", normalClosedSelector ), new CssElement( "SectionNormalStyleExpandedState", normalExpandedSelector ),
						new CssElement( "SectionBoxStyleBothStates", boxClosedSelector, boxExpandedSelector ), new CssElement( "SectionBoxStyleClosedState", boxClosedSelector ),
						new CssElement( "SectionBoxStyleExpandedState", boxExpandedSelector ), new CssElement( "SectionHeadingContainer", "* > div." + headingClass.ClassName ),
						new CssElement( "SectionHeading", "h1." + headingClass.ClassName ), new CssElement( "SectionExpandLabel", "span." + closeClass.ClassName ),
						new CssElement( "SectionCloseLabel", "span." + expandClass.ClassName ), new CssElement( "SectionContentContainer", "div." + contentClass.ClassName )
					};
			}
		}

		private readonly IReadOnlyCollection<DisplayableElement> children;

		/// <summary>
		/// Creates a section.
		/// </summary>
		/// <param name="content">The section's content.</param>
		/// <param name="displaySetup"></param>
		/// <param name="style">The section's style.</param>
		/// <param name="classes">The classes on the section.</param>
		/// <param name="etherealChildren"></param>
		public Section(
			IEnumerable<FlowComponent> content, DisplaySetup displaySetup = null, SectionStyle style = SectionStyle.Normal, ElementClassSet classes = null,
			IEnumerable<EtherealComponent> etherealChildren = null ): this(
			"",
			content,
			displaySetup: displaySetup,
			style: style,
			classes: classes,
			etherealChildren: etherealChildren ) {}

		/// <summary>
		/// Creates a section.
		/// </summary>
		/// <param name="heading">The section's heading. Do not pass null.</param>
		/// <param name="content">The section's content.</param>
		/// <param name="displaySetup"></param>
		/// <param name="style">The section's style.</param>
		/// <param name="classes">The classes on the section.</param>
		/// <param name="postHeadingComponents">Components that follow the heading but are still part of the heading container.</param>
		/// <param name="expanded">Set to true or false if you want users to be able to expand or close the section by clicking on the heading.</param>
		/// <param name="etherealChildren"></param>
		public Section(
			string heading, IEnumerable<FlowComponent> content, DisplaySetup displaySetup = null, SectionStyle style = SectionStyle.Normal,
			ElementClassSet classes = null, IEnumerable<FlowComponent> postHeadingComponents = null, bool? expanded = null,
			IEnumerable<EtherealComponent> etherealChildren = null ): this(
			displaySetup,
			style,
			classes,
			heading,
			postHeadingComponents,
			content,
			expanded,
			false,
			etherealChildren ) {}

		/// <summary>
		/// BasicPage.master use only.
		/// </summary>
		public Section(
			DisplaySetup displaySetup, SectionStyle style, ElementClassSet classes, string heading, IEnumerable<FlowComponent> postHeadingComponents,
			IEnumerable<FlowComponent> content, bool? expanded, bool disableStatePersistence, IEnumerable<EtherealComponent> etherealChildren ) {
			children = new DisplayableElement(
				context => {
					var hiddenFieldId = new HiddenFieldId();
					var expandedPmv = heading.Any() && expanded.HasValue && !disableStatePersistence ? new PageModificationValue<string>() : null;

					FlowComponent getHeadingButton() {
						var headingComponents =
							new DisplayableElement(
									headingContext => new DisplayableElementData(
										null,
										() => new DisplayableElementLocalData( "h1" ),
										classes: headingClass,
										children: heading.ToComponents() ) ).ToCollection<FlowComponent>()
								.Concat( postHeadingComponents ?? Enumerable.Empty<FlowComponent>() );

						return expanded.HasValue
							       ?
							       // We cannot use EwfButton because we have flow content.
							       (FlowComponent)new DisplayableElement(
								       buttonContext => new DisplayableElementData(
									       null,
									       () => new DisplayableElementLocalData(
										       "div",
										       new FocusabilityCondition( true ),
										       isFocused => new DisplayableElementFocusDependentData(
											       attributes: new[] { Tuple.Create( "tabindex", "0" ), Tuple.Create( "role", "button" ) },
											       includeIdAttribute: true,
											       jsInitStatements:
											       "$( '#{0}' ).click( function() {{ {1} }} ); $( '#{0}' ).keypress( function( e ) {{ if( e.key === ' ' || e.key === 'Enter' ) $( this ).click(); }} );"
												       .FormatWith(
													       buttonContext.Id,
													       disableStatePersistence
														       ? "$( '#{0}' ).toggleClass( '{1}', 200 );".FormatWith(
															       context.Id,
															       StringTools.ConcatenateWithDelimiter(
																       " ",
																       style == SectionStyle.Normal
																	       ? new[] { normalClosedClass.ClassName, normalExpandedClass.ClassName }
																	       : new[] { boxClosedClass.ClassName, boxExpandedClass.ClassName } ) )
														       : hiddenFieldId.GetJsValueModificationStatements(
															       "document.getElementById( '{0}' ).value === '{2}' ? '{1}' : '{2}'".FormatWith(
																       hiddenFieldId.ElementId.Id,
																       bool.FalseString,
																       bool.TrueString ) ) ) + ( isFocused ? " document.getElementById( '{0}' ).focus();".FormatWith( buttonContext.Id ) : "" ) ) ),
									       children: new GenericFlowContainer(
										       new GenericPhrasingContainer( "Click to Expand".ToComponents(), classes: closeClass ).ToCollection()
											       .Append( new GenericPhrasingContainer( "Click to Close".ToComponents(), classes: expandClass ) )
											       .Concat( headingComponents ),
										       classes: headingClass ).ToCollection() ) )
							       : new GenericFlowContainer( new GenericFlowContainer( headingComponents, classes: headingClass ).ToCollection() );
					}

					content = content?.ToImmutableArray() ?? Enumerable.Empty<FlowComponent>();
					return new DisplayableElementData(
						displaySetup,
						() => new DisplayableElementLocalData(
							"section",
							focusDependentData: new DisplayableElementFocusDependentData( includeIdAttribute: heading.Any() && expanded.HasValue && disableStatePersistence ) ),
						classes: allStylesBothStatesClass
							.Add(
								style == SectionStyle.Normal
									? getSectionClasses( expanded, expandedPmv, normalClosedClass, normalExpandedClass )
									: getSectionClasses( expanded, expandedPmv, boxClosedClass, boxExpandedClass ) )
							.Add( classes ?? ElementClassSet.Empty ),
						children: ( heading.Any() ? getHeadingButton().ToCollection() : Enumerable.Empty<FlowComponent>() ).Concat(
							content.Any() ? new GenericFlowContainer( content, classes: contentClass ).ToCollection() : Enumerable.Empty<FlowComponent>() ),
						etherealChildren: ( expandedPmv != null
							                    ? new EwfHiddenField( expanded.Value.ToString(), id: hiddenFieldId, pageModificationValue: expandedPmv ).PageComponent.ToCollection()
							                    : Enumerable.Empty<EtherealComponent>() ).Concat( etherealChildren ?? Enumerable.Empty<EtherealComponent>() ) );
				} ).ToCollection();
		}

		private ElementClassSet getSectionClasses( bool? expanded, PageModificationValue<string> expandedPmv, ElementClass closedClass, ElementClass expandedClass ) {
			return !expanded.HasValue
				       ? expandedClass
				       : expandedPmv == null
					       ? expanded.Value
						         ? expandedClass
						         : closedClass
					       : expandedPmv.ToCondition( bool.FalseString.ToCollection() )
						       .ToElementClassSet( closedClass )
						       .Add( expandedPmv.ToCondition( bool.TrueString.ToCollection() ).ToElementClassSet( expandedClass ) );
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}

	// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
	public sealed class LegacySection: WebControl {
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

		/// <summary>
		/// Creates a section.
		/// </summary>
		/// <param name="contentControls">The section's content.</param>
		/// <param name="style">The section's style.</param>
		public LegacySection( IEnumerable<Control> contentControls, SectionStyle style = SectionStyle.Normal ): this( "", contentControls, style: style ) {}

		/// <summary>
		/// Creates a section.
		/// </summary>
		/// <param name="heading">The section's heading. Do not pass null.</param>
		/// <param name="contentControls">The section's content.</param>
		/// <param name="style">The section's style.</param>
		/// <param name="postHeadingControls">Controls that follow the heading but are still part of the heading container.</param>
		/// <param name="expanded">Set to true or false if you want users to be able to expand or close the section by clicking on the heading.</param>
		public LegacySection(
			string heading, IEnumerable<Control> contentControls, SectionStyle style = SectionStyle.Normal, IEnumerable<Control> postHeadingControls = null,
			bool? expanded = null ): this( style, heading, postHeadingControls, contentControls, expanded, false ) {}

		/// <summary>
		/// Standard library use only.
		/// </summary>
		public LegacySection(
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
								.Append( new GenericPhrasingContainer( "Click to Close".ToComponents(), classes: expandClass ) )
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