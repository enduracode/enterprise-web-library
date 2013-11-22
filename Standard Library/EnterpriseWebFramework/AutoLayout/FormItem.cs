using System;
using System.Web.UI;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Contains metadata about a control, such as what it is called, ways in which it can be displayed, how it should be validated, etc.
	/// </summary>
	public abstract class FormItem {
		/// <summary>
		/// Creates a form item with the given label and control. Cell span only applies to adjacent layouts.
		/// </summary>
		public static FormItem<ControlType> Create<ControlType>( Control label, ControlType control, int? cellSpan = null,
		                                                         TextAlignment textAlignment = TextAlignment.NotSpecified,
		                                                         Func<ControlType, Validation> validationGetter = null ) where ControlType: Control {
			return new FormItem<ControlType>( label, control, cellSpan, textAlignment, validationGetter != null ? validationGetter( control ) : null );
		}

		private readonly Control label;
		private readonly Control control;

		// NOTE: We may want to bundle all display stuff into one class. How would we make that easy to use, though?
		private readonly int? cellSpan;
		private readonly TextAlignment textAlignment;

		private readonly Validation validation;

		/// <summary>
		/// Creates a form item.
		/// </summary>
		protected FormItem( Control label, Control control, int? cellSpan, TextAlignment textAlignment, Validation validation ) {
			this.label = label;
			this.control = control;
			this.cellSpan = cellSpan;
			this.textAlignment = textAlignment;
			this.validation = validation;
		}

		/// <summary>
		/// Gets the label.
		/// </summary>
		public virtual Control Label { get { return label; } }

		/// <summary>
		/// Gets the control.
		/// </summary>
		public virtual Control Control { get { return control; } }

		internal int? CellSpan { get { return cellSpan; } }
		internal TextAlignment TextAlignment { get { return textAlignment; } }

		/// <summary>
		/// Gets the validation.
		/// </summary>
		public virtual Validation Validation { get { return validation; } }

		/// <summary>
		/// Creates a labeled control for this form item.
		/// This can be used to insert controls to a page without a <see cref="FormItemBlock"/> and display inline error messages.
		/// </summary>
		public LabeledControl ToControl( bool omitLabel = false ) {
			return new LabeledControl( omitLabel ? null : label, control, validation );
		}
	}

	/// <summary>
	/// Contains metadata about a control, such as what it is called, ways in which it can be displayed, how it should be validated, etc.
	/// </summary>
	public class FormItem<ControlType>: FormItem where ControlType: Control {
		private readonly ControlType control;

		internal FormItem( Control label, ControlType control, int? cellSpan, TextAlignment textAlignment, Validation validation )
			: base( label, control, cellSpan, textAlignment, validation ) {
			this.control = control;
		}

		/// <summary>
		/// Gets the control.
		/// </summary>
		public new ControlType Control { get { return control; } }
	}
}