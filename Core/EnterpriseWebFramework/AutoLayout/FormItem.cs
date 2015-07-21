using System;
using System.Linq;
using System.Web.UI;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Contains metadata about a control, such as what it is called, ways in which it can be displayed, how it should be validated, etc.
	/// </summary>
	public abstract class FormItem {
		/// <summary>
		/// Creates a form item with the given label and control. Cell span only applies to adjacent layouts.
		/// </summary>
		// By taking a FormItemLabel instead of a Control for label, we retain the ability to implement additional behavior for string labels, such as automatically
		// making them bold.
		public static FormItem<ControlType> Create<ControlType>(
			FormItemLabel label, ControlType control, int? cellSpan = null, TextAlignment textAlignment = TextAlignment.NotSpecified,
			Func<ControlType, EwfValidation> validationGetter = null ) where ControlType: Control {
			return new FormItem<ControlType>( label, control, cellSpan, textAlignment, validationGetter != null ? validationGetter( control ) : null );
		}

		private readonly FormItemLabel label;
		private readonly Control control;

		// NOTE: We may want to bundle all display stuff into one class. How would we make that easy to use, though?
		private readonly int? cellSpan;
		private readonly TextAlignment textAlignment;

		private readonly EwfValidation validation;

		/// <summary>
		/// Creates a form item.
		/// </summary>
		protected FormItem( FormItemLabel label, Control control, int? cellSpan, TextAlignment textAlignment, EwfValidation validation ) {
			if( label == null )
				throw new ApplicationException( "The label cannot be a null FormItemLabel reference." );
			this.label = label;

			this.control = control;
			this.cellSpan = cellSpan;
			this.textAlignment = textAlignment;
			this.validation = validation;
		}

		/// <summary>
		/// Gets the label.
		/// </summary>
		public virtual Control Label { get { return label.Text != null ? label.Text.Any() ? label.Text.GetLiteralControl() : null : label.Control; } }

		/// <summary>
		/// Gets the control.
		/// </summary>
		public virtual Control Control { get { return control; } }

		internal int? CellSpan { get { return cellSpan; } }
		internal TextAlignment TextAlignment { get { return textAlignment; } }

		/// <summary>
		/// Gets the validation.
		/// </summary>
		public virtual EwfValidation Validation { get { return validation; } }

		/// <summary>
		/// Creates a labeled control for this form item.
		/// This can be used to insert controls to a page without a <see cref="FormItemBlock"/> and display inline error messages.
		/// </summary>
		public LabeledControl ToControl( bool omitLabel = false ) {
			return new LabeledControl( omitLabel ? null : Label, control, validation );
		}
	}

	/// <summary>
	/// Contains metadata about a control, such as what it is called, ways in which it can be displayed, how it should be validated, etc.
	/// </summary>
	public class FormItem<ControlType>: FormItem where ControlType: Control {
		private readonly ControlType control;

		internal FormItem( FormItemLabel label, ControlType control, int? cellSpan, TextAlignment textAlignment, EwfValidation validation )
			: base( label, control, cellSpan, textAlignment, validation ) {
			this.control = control;
		}

		/// <summary>
		/// Gets the control.
		/// </summary>
		public new ControlType Control { get { return control; } }
	}
}