using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using MoreLinq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A style that displays errors as a list.
	/// </summary>
	public class ListErrorDisplayStyle: ErrorDisplayStyle {
		/// <summary>
		/// EWL use only.
		/// </summary>
		public class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfErrorMessageListBlock";

			/// <summary>
			/// EWL use only.
			/// </summary>
			public static readonly IReadOnlyCollection<string> Selectors = ( "div." + CssClass ).ToCollection();

			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
				return new CssElement( "ErrorMessageControlListBlock", Selectors.ToArray() ).ToCollection();
			}
		}

		private readonly IEnumerable<string> additionalClasses;

		/// <summary>
		/// Creates a list error-display style.
		/// </summary>
		/// <param name="additionalClasses">Additional classes that will be added to the list block.</param>
		public ListErrorDisplayStyle( IEnumerable<string> additionalClasses = null ) {
			this.additionalClasses = ( additionalClasses ?? ImmutableArray<string>.Empty );
		}

		IEnumerable<Control> ErrorDisplayStyle.GetControls( IEnumerable<string> errors ) {
			return errors.Any() ? GetErrorMessageListBlock( additionalClasses, errors ).ToCollection() : new Control[ 0 ];
		}

		internal static Control GetErrorMessageListBlock( IEnumerable<string> additionalClasses, IEnumerable<string> errors ) {
			// Client code that uses NetTools.BuildBasicLink depends on us not HTML encoding error messages here. If raw or stored user input is ever used in error
			// messages, we are exposed to injection attacks.
			return
				new Block(
					ControlStack.CreateWithControls(
						true,
						errors.Select(
							i =>
							(Control)
							new PlaceHolder().AddControlsReturnThis(
								new FontAwesomeIcon( "fa-times-circle", "fa-lg" ).ToCollection().Concat( " ".ToComponents() ).GetControls().Concat( new Literal { Text = i } ) ) )
							.ToArray() ) )
					{
						CssClass = StringTools.ConcatenateWithDelimiter( " ", CssElementCreator.CssClass.ToCollection().Concat( additionalClasses ).ToArray() )
					};
		}
	}
}