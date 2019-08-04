using System.Collections.Generic;
using System.Linq;
using Humanizer;
using JetBrains.Annotations;

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

		internal FormItem(
			FormItemSetup setup, IReadOnlyCollection<PhrasingComponent> label, IReadOnlyCollection<FlowComponent> content, EwfValidation validation ) {
			Setup = setup ?? new FormItemSetup();
			this.label = label;
			this.content = content;
			Validation = validation;
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
		/// Creates a component representing this form item.
		/// This can be used to display a form item without a <see cref="FormItemList"/>.
		/// </summary>
		public FlowComponent ToComponent( bool omitLabel = false ) =>
			new GenericFlowContainer(
				( !label.Any() || omitLabel
					  ? Enumerable.Empty<FlowComponent>()
					  : new GenericPhrasingContainer( label, classes: labelClass ).ToCollection<PhrasingComponent>().Append( new LineBreak() ) )
				.Append( new GenericFlowContainer( content, classes: contentClass ) )
				.Concat(
					Validation == null
						? Enumerable.Empty<FlowComponent>()
						: new FlowErrorContainer( new ErrorSourceSet( validations: Validation.ToCollection() ), new ListErrorDisplayStyle() ).ToCollection() )
				.Materialize(),
				displaySetup: Setup.DisplaySetup,
				classes: itemClass );
	}

	public static class FormItemExtensionCreators {
		/// <summary>
		/// Creates a form item with this form control.
		/// </summary>
		/// <param name="formControl"></param>
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
		/// <param name="content"></param>
		/// <param name="label">The form-item label.</param>
		/// <param name="setup"></param>
		/// <param name="validation"></param>
		public static FormItem ToFormItem(
			this IReadOnlyCollection<FlowComponent> content, FormItemSetup setup = null, IReadOnlyCollection<PhrasingComponent> label = null,
			EwfValidation validation = null ) {
			label = label ?? Enumerable.Empty<PhrasingComponent>().Materialize();
			return new FormItem( setup, label, content, validation );
		}
	}
}