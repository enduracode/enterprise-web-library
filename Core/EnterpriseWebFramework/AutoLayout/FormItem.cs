using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Contains metadata about a control, such as what it is called, ways in which it can be displayed, how it should be validated, etc.
	/// </summary>
	public abstract class FormItem {
		/// <summary>
		/// Creates a form item with the given label and control.
		/// </summary>
		// By taking a FormItemLabel instead of a Control for label, we retain the ability to implement additional behavior for string labels, such as automatically
		// making them bold.
		public static FormItem<ControlType> Create<ControlType>(
			FormItemLabel label, ControlType control, FormItemSetup setup = null, Func<ControlType, EwfValidation> validationGetter = null ) where ControlType: Control {
			return new FormItem<ControlType>( setup, label, control, validationGetter?.Invoke( control ) );
		}

		internal readonly FormItemSetup Setup;
		private readonly FormItemLabel label;
		private readonly Control control;
		public readonly EwfValidation Validation;

		/// <summary>
		/// Creates a form item.
		/// </summary>
		protected FormItem( FormItemSetup setup, FormItemLabel label, Control control, EwfValidation validation ) {
			Setup = setup ?? new FormItemSetup();
			this.label = label ?? throw new ApplicationException( "The label cannot be a null FormItemLabel reference." );
			this.control = control;
			Validation = validation;
		}

		/// <summary>
		/// Gets the label.
		/// </summary>
		public virtual Control Label => label.Text != null
			                                ? label.Text.Any()
				                                  ? new PlaceHolder().AddControlsReturnThis( label.Text.ToComponents().GetControls() )
				                                  : null
			                                : label.Control;

		/// <summary>
		/// Gets the control.
		/// </summary>
		public virtual Control Control => control;

		/// <summary>
		/// Creates a labeled control for this form item.
		/// This can be used to insert controls to a page without a <see cref="FormItemBlock"/> and display inline error messages.
		/// </summary>
		public LabeledControl ToControl( bool omitLabel = false ) {
			return new LabeledControl( omitLabel ? null : Label, control, Validation );
		}
	}

	/// <summary>
	/// Contains metadata about a control, such as what it is called, ways in which it can be displayed, how it should be validated, etc.
	/// </summary>
	public class FormItem<ControlType>: FormItem where ControlType: Control {
		private readonly ControlType control;

		internal FormItem( FormItemSetup setup, FormItemLabel label, ControlType control, EwfValidation validation ): base( setup, label, control, validation ) {
			this.control = control;
		}

		/// <summary>
		/// Gets the control.
		/// </summary>
		public new ControlType Control => control;
	}

	public static class FormItemExtensionCreators {
		/// <summary>
		/// Creates a form item with this form control and the specified label.
		/// </summary>
		public static FormItem ToFormItem( this FormControl<FlowComponent> formControl, FormItemLabel label, FormItemSetup setup = null ) {
			// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
			if( formControl is WebControl webControl )
				return new FormItem<Control>( setup, label, webControl, formControl.Validation );

			return formControl.PageComponent.ToCollection().ToFormItem( label, setup: setup, validation: formControl.Validation );
		}

		/// <summary>
		/// Creates a form item with these components and the specified label.
		/// </summary>
		public static FormItem ToFormItem(
			this IEnumerable<FlowComponent> content, FormItemLabel label, FormItemSetup setup = null, EwfValidation validation = null ) {
			return new FormItem<Control>( setup, label, new PlaceHolder().AddControlsReturnThis( content.GetControls() ), validation );
		}
	}
}