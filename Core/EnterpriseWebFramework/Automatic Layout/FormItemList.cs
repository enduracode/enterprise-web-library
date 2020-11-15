using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using JetBrains.Annotations;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Helps lay out form items in useful ways.
	/// </summary>
	// After https://bugs.chromium.org/p/chromium/issues/detail?id=375693 is fixed, change the top-level element to fieldset. Don't support legend.
	public class FormItemList: FlowComponent {
		// This class allows us to use just one selector in the FormItemList element.
		private static readonly ElementClass allListsClass = new ElementClass( "ewfFil" );

		private static readonly ElementClass stackClass = new ElementClass( "ewfSfil" );
		private static readonly ElementClass wrappingClass = new ElementClass( "ewfWfil" );
		private static readonly ElementClass gridClass = new ElementClass( "ewfGfil" );

		private static readonly ElementClass itemClass = new ElementClass( "ewfFilI" );
		private static readonly ElementClass buttonItemClass = new ElementClass( "ewfFilB" );
		private static readonly ElementClass labelClass = new ElementClass( "ewfFilL" );
		private static readonly ElementClass contentClass = new ElementClass( "ewfFilC" );

		[ UsedImplicitly ]
		private class CssElementCreator: ControlCssElementCreator {
			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
				new CssElement( "FormItemList", "div.{0}".FormatWith( allListsClass.ClassName ) ).ToCollection()
					.Append( new CssElement( "StackFormItemList", "div.{0}".FormatWith( stackClass.ClassName ) ) )
					.Append( new CssElement( "WrappingFormItemList", "div.{0}".FormatWith( wrappingClass.ClassName ) ) )
					.Append( new CssElement( "GridFormItemList", "div.{0}".FormatWith( gridClass.ClassName ) ) )
					.Append( new CssElement( "FormItemListItem", "div.{0}".FormatWith( itemClass.ClassName ) ) )
					.Append( new CssElement( "FormItemListButtonItem", "div.{0}".FormatWith( buttonItemClass.ClassName ) ) )
					.Append( new CssElement( "FormItemListItemLabel", "div.{0}".FormatWith( labelClass.ClassName ) ) )
					.Append( new CssElement( "FormItemListItemContent", "div.{0}".FormatWith( contentClass.ClassName ) ) )
					.Materialize();
		}

		/// <summary>
		/// Creates a list with a classic "label on the left, content on the right" layout. Labels and content will automatically stack when the width of the list
		/// is constrained.
		/// </summary>
		public static FormItemList CreateStack(
			FormItemListSetup generalSetup = null, ( ContentBasedLength label, ContentBasedLength content )? minWidths = null,
			IReadOnlyCollection<FormItem> items = null ) {
			minWidths = minWidths ?? ( 12.ToEm(), 24.ToEm() );
			return new FormItemList(
				generalSetup,
				stackClass,
				"",
				i => ElementClassSet.Empty,
				i => "",
				null,
				i => new DisplayableElement(
						context => new DisplayableElementData(
							null,
							() => new DisplayableElementLocalData(
								"div",
								focusDependentData: new DisplayableElementFocusDependentData(
									attributes: Tuple.Create( "style", "flex-basis: {0}".FormatWith( ( (CssLength)minWidths.Value.label ).Value ) ).ToCollection() ) ),
							classes: labelClass,
							children: i.Label ) ).ToCollection()
					.Append(
						new DisplayableElement(
							context => new DisplayableElementData(
								null,
								() => new DisplayableElementLocalData(
									"div",
									focusDependentData: new DisplayableElementFocusDependentData(
										attributes: Tuple.Create( "style", "flex-basis: {0}".FormatWith( ( (CssLength)minWidths.Value.content ).Value ) ).ToCollection() ) ),
								classes: contentClass.Add( TextAlignmentStatics.Class( i.Setup.TextAlignment ) ),
								children: i.Content.Concat(
										i.ErrorSourceSet == null
											? Enumerable.Empty<FlowComponent>()
											: new FlowErrorContainer( i.ErrorSourceSet, new ListErrorDisplayStyle(), disableFocusabilityOnError: true ).ToCollection() )
									.Materialize() ) ) )
					.Materialize(),
				items );
		}

		/// <summary>
		/// Creates a wrapping list.
		/// </summary>
		/// <param name="setup"></param>
		/// <param name="items"></param>
		public static FormItemList CreateWrapping( FormItemListSetup setup = null, IReadOnlyCollection<FormItem> items = null ) =>
			new FormItemList( setup, wrappingClass, "", i => ElementClassSet.Empty, i => "", null, getItemComponents, items );

		/// <summary>
		/// Creates a list with the specified number of columns where each form item's label is placed directly on top of it.
		/// </summary>
		/// <param name="numberOfColumns"></param>
		/// <param name="generalSetup"></param>
		/// <param name="minWidth">The minimum width of the grid. For a responsive design, we recommend omitting this and instead using media queries to change the
		/// form of the grid on narrow screens.</param>
		/// <param name="defaultColumnSpan"></param>
		/// <param name="verticalAlignment"></param>
		/// <param name="items"></param>
		public static FormItemList CreateGrid(
			int numberOfColumns, FormItemListSetup generalSetup = null, ContentBasedLength minWidth = null, int defaultColumnSpan = 1,
			GridVerticalAlignment verticalAlignment = GridVerticalAlignment.NotSpecified, IReadOnlyCollection<FormItem> items = null ) {
			if( defaultColumnSpan > numberOfColumns )
				throw new ApplicationException( "The default column span is {0}, but the number of columns is {1}.".FormatWith( defaultColumnSpan, numberOfColumns ) );

			return new FormItemList(
				generalSetup,
				gridClass.Add( GridVerticalAlignmentStatics.Class( verticalAlignment ) ),
				( minWidth != null ? "min-width: {0}; ".FormatWith( ( (CssLength)minWidth ).Value ) : "" ) +
				"grid-template-columns: repeat( {0}, 1fr )".FormatWith( numberOfColumns ),
				i => TextAlignmentStatics.Class( i.Setup.TextAlignment ),
				i => {
					var span = defaultColumnSpan;
					if( i.Setup.ColumnSpan.HasValue ) {
						if( i.Setup.ColumnSpan.Value > numberOfColumns )
							throw new ApplicationException(
								"An item has a column span of {0}, but the number of columns is {1}.".FormatWith( i.Setup.ColumnSpan.Value, numberOfColumns ) );
						span = i.Setup.ColumnSpan.Value;
					}
					return span == 1 ? "" : "grid-column-end: span {0}".FormatWith( span );
				},
				displaySetup => new FormItemSetup( displaySetup: displaySetup, columnSpan: defaultColumnSpan ),
				getItemComponents,
				items );
		}

		/// <summary>
		/// Creates a raw list, which is helpful when you want to handle responsiveness in your own style sheets.
		/// </summary>
		/// <param name="setup"></param>
		/// <param name="items"></param>
		public static FormItemList CreateRaw( FormItemListSetup setup = null, IReadOnlyCollection<FormItem> items = null ) =>
			new FormItemList( setup, ElementClassSet.Empty, "", i => ElementClassSet.Empty, i => "", null, getItemComponents, items );

		private static IReadOnlyCollection<FlowComponent> getItemComponents( FormItem item ) =>
			( item.Label.Any() ? new GenericFlowContainer( item.Label, classes: labelClass ).ToCollection() : Enumerable.Empty<FlowComponent>() ).Append(
				new GenericFlowContainer(
					item.Content.Concat(
							item.ErrorSourceSet == null
								? Enumerable.Empty<FlowComponent>()
								: new FlowErrorContainer( item.ErrorSourceSet, new ListErrorDisplayStyle(), disableFocusabilityOnError: true ).ToCollection() )
						.Materialize(),
					classes: contentClass ) )
			.Materialize();

		private readonly IReadOnlyCollection<DisplayableElement> children;
		private readonly List<FormItem> items;

		private FormItemList(
			FormItemListSetup setup, ElementClassSet classes, string listStyleAttribute, Func<FormItem, ElementClassSet> itemClassGetter,
			Func<FormItem, string> itemStyleAttributeGetter, Func<DisplaySetup, FormItemSetup> buttonItemSetupGetter,
			Func<FormItem, IReadOnlyCollection<FlowComponent>> itemComponentGetter, IReadOnlyCollection<FormItem> items ) {
			setup = setup ?? new FormItemListSetup();
			buttonItemSetupGetter = buttonItemSetupGetter ?? ( displaySetup => new FormItemSetup( displaySetup: displaySetup ) );

			var buttonItem = setup.ButtonItemGetter( buttonItemSetupGetter );
			children = new DisplayableElement(
				listContext => new DisplayableElementData(
					setup.DisplaySetup,
					() => new DisplayableElementLocalData(
						"div",
						focusDependentData: new DisplayableElementFocusDependentData(
							attributes: listStyleAttribute.Any() ? Tuple.Create( "style", listStyleAttribute ).ToCollection() : null ) ),
					classes: allListsClass.Add( classes ).Add( setup.Classes ?? ElementClassSet.Empty ),
					children: this.items.Select( i => ( item: i, elementClass: itemClass ) )
						.Concat( buttonItem.Select( i => ( item: i, elementClass: buttonItemClass ) ) )
						.Select(
							i => new FlowIdContainer(
								new DisplayableElement(
									itemContext => new DisplayableElementData(
										i.item.Setup.DisplaySetup,
										() => {
											var styleAttribute = itemStyleAttributeGetter( i.item );
											return ListErrorDisplayStyle.GetErrorFocusableElementLocalData(
												itemContext,
												"div",
												i.item.ErrorSourceSet,
												styleAttribute.Any() ? Tuple.Create( "style", styleAttribute ).ToCollection() : null );
										},
										classes: i.elementClass.Add( itemClassGetter( i.item ) ),
										children: itemComponentGetter( i.item ) ) ).ToCollection(),
								updateRegionSets: i.item.Setup.UpdateRegionSets ) )
						.Materialize(),
					etherealChildren: setup.EtherealContent ) ).ToCollection();

			this.items = ( items ?? Enumerable.Empty<FormItem>() ).ToList();
		}

		/// <summary>
		/// Adds an item to the list. This method can be called repeatedly.
		/// </summary>
		public void AddItem( FormItem item ) => items.Add( item );

		/// <summary>
		/// Adds items to the list. This method can be called repeatedly.
		/// </summary>
		public void AddItems( IReadOnlyCollection<FormItem> items ) => this.items.AddRange( items );

		/// <summary>
		/// Do not use.
		/// </summary>
		public void AddFormItems( params FormItem[] items ) {
			this.items.AddRange( items );
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => children;
	}
}