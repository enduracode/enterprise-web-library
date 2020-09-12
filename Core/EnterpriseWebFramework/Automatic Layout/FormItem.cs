using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using JetBrains.Annotations;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Contains metadata about a control, such as what it is called, ways in which it can be displayed, how it should be validated, etc.
	/// </summary>
	public class FormItem {
		private static readonly ElementClass itemClass = new ElementClass( "ewfFi" );
		private static readonly ElementClass labelClass = new ElementClass( "ewfFiL" );
		private static readonly ElementClass contentClass = new ElementClass( "ewfFiC" );

		[ UsedImplicitly ]
		private class CssElementCreator: ControlCssElementCreator {
			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
				new CssElement( "FormItem", "div.{0}".FormatWith( itemClass.ClassName ) ).ToCollection()
					.Append( new CssElement( "FormItemLabel", "span.{0}".FormatWith( labelClass.ClassName ) ) )
					.Append( new CssElement( "FormItemContent", "div.{0}".FormatWith( contentClass.ClassName ) ) )
					.Materialize();
		}

		internal readonly FormItemSetup Setup;
		private readonly IReadOnlyCollection<PhrasingComponent> label;
		private readonly IReadOnlyCollection<FlowComponent> content;
		public readonly EwfValidation Validation;
		internal readonly ErrorSourceSet ErrorSourceSet;

		internal FormItem(
			FormItemSetup setup, IReadOnlyCollection<PhrasingComponent> label, IReadOnlyCollection<FlowComponent> content, EwfValidation validation ) {
			Setup = setup ?? new FormItemSetup();
			this.label = label;
			this.content = content;
			Validation = validation;
			ErrorSourceSet = validation != null ? new ErrorSourceSet( validations: validation.ToCollection() ) : null;
		}

		/// <summary>
		/// Gets the label.
		/// </summary>
		public IReadOnlyCollection<PhrasingComponent> Label => label;

		/// <summary>
		/// Gets the content.
		/// </summary>
		public IReadOnlyCollection<FlowComponent> Content => content;

		/// <summary>
		/// Creates a collection of components representing this form item.
		/// This can be used to display a form item without a <see cref="FormItemList"/>.
		/// </summary>
		public IReadOnlyCollection<FlowComponent> ToComponentCollection( bool omitLabel = false ) =>
			new FlowIdContainer(
				new DisplayableElement(
					context => new DisplayableElementData(
						Setup.DisplaySetup,
						() => ListErrorDisplayStyle.GetErrorFocusableElementLocalData( context, "div", ErrorSourceSet, null ),
						classes: itemClass,
						children: ( !label.Any() || omitLabel
							            ? Enumerable.Empty<FlowComponent>()
							            : new GenericPhrasingContainer( label, classes: labelClass ).ToCollection<PhrasingComponent>().Append( new LineBreak() ) )
						.Append( new GenericFlowContainer( content, classes: contentClass ) )
						.Concat( getErrorContainer() )
						.Materialize() ) ).ToCollection(),
				updateRegionSets: Setup.UpdateRegionSets ).ToCollection();

		/// <summary>
		/// Creates a list item representing this form item, without its label. Useful for lists of checkboxes, or any single control that needs to be repeated.
		/// </summary>
		/// <param name="visualOrderRank"></param>
		/// <param name="etherealContent"></param>
		public ComponentListItem ToListItem( int? visualOrderRank = null, IReadOnlyCollection<EtherealComponent> etherealContent = null ) =>
			content.Concat( getErrorContainer() )
				.Materialize()
				.ToComponentListItem( Setup.DisplaySetup, null, visualOrderRank, Setup.UpdateRegionSets, etherealContent, getErrorFocusableElementLocalData );

		/// <summary>
		/// Creates a list item representing this form item, without its label. Useful for lists of checkboxes, or any single control that needs to be repeated.
		/// </summary>
		/// <param name="id">The ID of the item. This is required if you're adding the item on an intermediate post-back or want to remove the item on an
		/// intermediate post-back. Do not pass null or the empty string.</param>
		/// <param name="visualOrderRank"></param>
		/// <param name="removalUpdateRegionSets">The intermediate-post-back update-region sets that this item's removal will be a part of.</param>
		/// <param name="etherealContent"></param>
		public ComponentListItem ToListItem(
			string id, int? visualOrderRank = null, IEnumerable<UpdateRegionSet> removalUpdateRegionSets = null,
			IReadOnlyCollection<EtherealComponent> etherealContent = null ) =>
			content.Concat( getErrorContainer() )
				.Materialize()
				.ToComponentListItem(
					id,
					Setup.DisplaySetup,
					null,
					visualOrderRank,
					Setup.UpdateRegionSets,
					removalUpdateRegionSets,
					etherealContent,
					getErrorFocusableElementLocalData );

		private IEnumerable<FlowComponent> getErrorContainer() =>
			ErrorSourceSet != null
				? new FlowErrorContainer( ErrorSourceSet, new ListErrorDisplayStyle(), disableFocusabilityOnError: true ).ToCollection()
				: Enumerable.Empty<FlowComponent>();

		private DisplayableElementLocalData getErrorFocusableElementLocalData(
			ElementContext context, string elementName, IReadOnlyCollection<Tuple<string, string>> attributes ) =>
			ListErrorDisplayStyle.GetErrorFocusableElementLocalData( context, elementName, ErrorSourceSet, attributes );
	}

	public static class FormItemExtensionCreators {
		/// <summary>
		/// Creates a form item with this form control.
		/// </summary>
		/// <param name="formControl">Do not pass null.</param>
		/// <param name="label">The form-item label.</param>
		/// <param name="setup"></param>
		public static FormItem ToFormItem(
			this FormControl<FlowComponent> formControl, FormItemSetup setup = null, IReadOnlyCollection<PhrasingComponent> label = null ) {
			label = label ?? Enumerable.Empty<PhrasingComponent>().Materialize();
			return formControl.PageComponent.ToCollection()
				.ToFormItem(
					setup: setup,
					label: label.Any() && formControl.Labeler != null ? formControl.Labeler.CreateLabel( label ) : label,
					validation: formControl.Validation );
		}

		/// <summary>
		/// Creates a form item with these components.
		/// </summary>
		/// <param name="content">Do not pass null.</param>
		/// <param name="label">The form-item label.</param>
		/// <param name="setup"></param>
		/// <param name="validation"></param>
		public static FormItem ToFormItem(
			this IReadOnlyCollection<FlowComponent> content, FormItemSetup setup = null, IReadOnlyCollection<PhrasingComponent> label = null,
			EwfValidation validation = null ) {
			label = label ?? Enumerable.Empty<PhrasingComponent>().Materialize();
			return new FormItem( setup, label, content, validation );
		}

		/// <summary>
		/// Creates a form item with this component.
		/// </summary>
		/// <param name="content">Do not pass null.</param>
		/// <param name="label">The form-item label.</param>
		/// <param name="setup"></param>
		/// <param name="validation"></param>
		public static FormItem ToFormItem(
			this FlowComponent content, FormItemSetup setup = null, IReadOnlyCollection<PhrasingComponent> label = null, EwfValidation validation = null ) =>
			content.ToCollection().ToFormItem( setup: setup, label: label, validation: validation );

		/// <summary>
		/// Concatenates form items.
		/// </summary>
		public static IEnumerable<FormItem> Concat( this FormItem first, IEnumerable<FormItem> second ) => second.Prepend( first );

		/// <summary>
		/// Returns a sequence of two form items.
		/// </summary>
		public static IEnumerable<FormItem> Append( this FormItem first, FormItem second ) => Enumerable.Empty<FormItem>().Append( first ).Append( second );
	}
}