using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A control wrapper that puts a label near the control being wrapped.
	/// </summary>
	public class LabeledControl: WebControl, ControlTreeDataLoader {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfLabeledControl";

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				return new[]
					{
						new CssElement( "LabeledControl", "div." + CssClass ),
						new CssElement( "LabeledControlItem",
						                ControlStack.CssElementCreator.Selectors.Select( i => i + " > " + ControlStack.CssElementCreator.ItemSelector ).ToArray() )
					};
			}
		}

		private readonly List<Control> wrappedControls = new List<Control>();
		private readonly Control label;
		private readonly Validation validation;

		/// <summary>
		/// Creates a new instance of a LabeledControl with the specified control and label. You may pass null for the label.
		/// </summary>
		public LabeledControl( Control wrappedControl, Control label ): this( label, wrappedControl, null ) {
			wrappedControls.Add( wrappedControl );
			this.label = label;
		}

		internal LabeledControl( Control label, Control control, Validation validation ) {
			this.label = label;
			wrappedControls.Add( control );
			this.validation = validation;
		}

		void ControlTreeDataLoader.LoadData() {
			CssClass = CssClass.ConcatenateWithSpace( CssElementCreator.CssClass );
			var controlStack = ControlStack.Create( false );
			if( label != null )
				controlStack.AddControls( label );
			if( validation != null ) {
				controlStack.AddModificationErrorItem( validation,
				                                       errors => ErrorMessageControlListBlockStatics.CreateErrorMessageListBlock( errors ).ToSingleElementArray() );
			}
			controlStack.AddControls( new PlaceHolder().AddControlsReturnThis( wrappedControls ) );
			Controls.Add( controlStack );
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}