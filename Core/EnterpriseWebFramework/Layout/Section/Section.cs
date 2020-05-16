using System;
using System.Collections.Generic;
using System.Linq;
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
						new CssElement( "SectionNormalStyleClosedState", normalClosedSelector ),
						new CssElement( "SectionNormalStyleExpandedState", normalExpandedSelector ),
						new CssElement( "SectionBoxStyleBothStates", boxClosedSelector, boxExpandedSelector ),
						new CssElement( "SectionBoxStyleClosedState", boxClosedSelector ), new CssElement( "SectionBoxStyleExpandedState", boxExpandedSelector ),
						new CssElement( "SectionHeadingContainer", "* > div." + headingClass.ClassName ),
						new CssElement( "SectionHeading", "h1." + headingClass.ClassName ), new CssElement( "SectionExpandLabel", "span." + closeClass.ClassName ),
						new CssElement( "SectionCloseLabel", "span." + expandClass.ClassName ),
						new CssElement( "SectionContentContainer", "div." + contentClass.ClassName )
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
		/// <param name="etherealContent"></param>
		public Section(
			IReadOnlyCollection<FlowComponent> content, DisplaySetup displaySetup = null, SectionStyle style = SectionStyle.Normal, ElementClassSet classes = null,
			IReadOnlyCollection<EtherealComponent> etherealContent = null ): this(
			"",
			content,
			displaySetup: displaySetup,
			style: style,
			classes: classes,
			etherealContent: etherealContent ) {}

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
		/// <param name="etherealContent"></param>
		public Section(
			string heading, IReadOnlyCollection<FlowComponent> content, DisplaySetup displaySetup = null, SectionStyle style = SectionStyle.Normal,
			ElementClassSet classes = null, IReadOnlyCollection<FlowComponent> postHeadingComponents = null, bool? expanded = null,
			IReadOnlyCollection<EtherealComponent> etherealContent = null ): this(
			displaySetup,
			style,
			classes,
			heading,
			postHeadingComponents,
			content,
			expanded,
			false,
			etherealContent ) {}

		/// <summary>
		/// BasicPage.master use only.
		/// </summary>
		public Section(
			DisplaySetup displaySetup, SectionStyle style, ElementClassSet classes, string heading, IReadOnlyCollection<FlowComponent> postHeadingComponents,
			IReadOnlyCollection<FlowComponent> content, bool? expanded, bool disableStatePersistence, IReadOnlyCollection<EtherealComponent> etherealContent ) {
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
							       ElementActivationBehavior.GetActivatableElement(
								       "div",
								       ElementClassSet.Empty,
								       Enumerable.Empty<Tuple<string, string>>().Materialize(),
								       ElementActivationBehavior.CreateButton(
									       buttonBehavior: new CustomButtonBehavior(
										       () => disableStatePersistence
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
													             bool.TrueString ) ) ) ),
								       new GenericFlowContainer(
									       new GenericPhrasingContainer( "Click to Expand".ToComponents(), classes: closeClass ).ToCollection()
										       .Append( new GenericPhrasingContainer( "Click to Close".ToComponents(), classes: expandClass ) )
										       .Concat( headingComponents )
										       .Materialize(),
									       classes: headingClass ).ToCollection(),
								       Enumerable.Empty<EtherealComponent>().Materialize() )
							       : new GenericFlowContainer( new GenericFlowContainer( headingComponents.Materialize(), classes: headingClass ).ToCollection() );
					}

					content = content ?? Enumerable.Empty<FlowComponent>().Materialize();
					return new DisplayableElementData(
						displaySetup,
						() => new DisplayableElementLocalData(
							"section",
							focusDependentData:
							new DisplayableElementFocusDependentData( includeIdAttribute: heading.Any() && expanded.HasValue && disableStatePersistence ) ),
						classes: allStylesBothStatesClass
							.Add(
								style == SectionStyle.Normal
									? getSectionClasses( expanded, expandedPmv, normalClosedClass, normalExpandedClass )
									: getSectionClasses( expanded, expandedPmv, boxClosedClass, boxExpandedClass ) )
							.Add( classes ?? ElementClassSet.Empty ),
						children: ( heading.Any() ? getHeadingButton().ToCollection() : Enumerable.Empty<FlowComponent>() ).Concat(
							content.Any() ? new GenericFlowContainer( content, classes: contentClass ).ToCollection() : Enumerable.Empty<FlowComponent>() )
						.Materialize(),
						etherealChildren: ( expandedPmv != null
							                    ? new EwfHiddenField( expanded.Value.ToString(), id: hiddenFieldId, pageModificationValue: expandedPmv ).PageComponent
								                    .ToCollection()
							                    : Enumerable.Empty<EtherealComponent>() ).Concat( etherealContent ?? Enumerable.Empty<EtherealComponent>() )
						.Materialize() );
				} ).ToCollection();
		}

		private ElementClassSet getSectionClasses(
			bool? expanded, PageModificationValue<string> expandedPmv, ElementClass closedClass, ElementClass expandedClass ) {
			return !expanded.HasValue ? expandedClass :
			       expandedPmv == null ? expanded.Value ? expandedClass : closedClass :
			       expandedPmv.ToCondition( bool.FalseString.ToCollection() )
				       .ToElementClassSet( closedClass )
				       .Add( expandedPmv.ToCondition( bool.TrueString.ToCollection() ).ToElementClassSet( expandedClass ) );
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}