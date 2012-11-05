using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;
using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A date picker.
	/// </summary>
	public class DatePicker: WebControl, ControlTreeDataLoader, ControlWithJsInitLogic, ControlWithCustomFocusLogic {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfDatePicker";

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				return new[] { new CssElement( "DatePicker", "div." + CssClass ) };
			}
		}

		private DateTime? value;
		private bool autoPostBack;
		private DateTime? minDate;
		private DateTime? maxDate;
		private bool constrainToSqlSmallDateTimeRange = true;
		private PostBackButton defaultSubmitButton;

		private EwfTextBox textBox;
		private DateTime min;
		private DateTime max;

		/// <summary>
		/// Creates a date picker.
		/// </summary>
		public DatePicker( DateTime? value ) {
			this.value = value.HasValue ? value.Value.Date as DateTime? : null;
		}

		/// <summary>
		/// Do not use.
		/// </summary>
		public DatePicker(): this( null ) {}

		/// <summary>
		/// Sets whether this date picker will post back automatically.
		/// </summary>
		public bool AutoPostBack { set { autoPostBack = value; } }

		/// <summary>
		/// Do not use.
		/// </summary>
		public DateTime? Value { set { this.value = value.HasValue ? value.Value.Date as DateTime? : null; } }

		/// <summary>
		/// The earliest acceptable date.
		/// </summary>
		public DateTime MinDate { set { minDate = value; } }

		/// <summary>
		/// The latest acceptable date.
		/// </summary>
		public DateTime MaxDate { set { maxDate = value; } }

		/// <summary>
		/// EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property.
		/// </summary>
		public override string ToolTip { get; set; }

		/// <summary>
		/// Control to display inside the tool tip. Do not pass null. This will ignore the ToolTip property.
		/// </summary>
		public Control ToolTipControl { get; set; }

		/// <summary>
		/// Constrains the acceptable dates to those accepted by Sql's small date time type. This defaults to true.
		/// </summary>
		public bool ConstrainToSqlSmallDateTimeRange { set { constrainToSqlSmallDateTimeRange = value; } }

		/// <summary>
		/// Assigns this to submit the given PostBackButton. This will disable the button's submit behavior. Do not pass null.
		/// </summary>
		public void SetDefaultSubmitButton( PostBackButton pbb ) {
			defaultSubmitButton = pbb;
		}

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			CssClass = CssClass.ConcatenateWithSpace( CssElementCreator.CssClass );

			textBox = new EwfTextBox( value.HasValue ? value.Value.ToMonthDayYearString() : "", preventAutoComplete: true ) { AutoPostBack = autoPostBack };

			if( defaultSubmitButton != null )
				textBox.SetDefaultSubmitButton( defaultSubmitButton );
			Controls.Add( textBox );

			min = DateTime.MinValue;
			max = DateTime.MaxValue;
			if( constrainToSqlSmallDateTimeRange ) {
				min = Validator.SqlSmallDateTimeMinValue;
				max = Validator.SqlSmallDateTimeMaxValue;
			}
			if( minDate.HasValue && minDate.Value > min )
				min = minDate.Value;
			if( maxDate.HasValue && maxDate.Value < max )
				max = maxDate.Value;

			if( ToolTip != null || ToolTipControl != null )
				new ToolTip( ToolTipControl ?? EnterpriseWebFramework.Controls.ToolTip.GetToolTipTextControl( ToolTip ), this );
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			return "$( '#" + textBox.TextBoxClientId + "' ).datepicker( { minDate: new Date( " + min.Year + ", " + min.Month + " - 1, " + min.Day +
			       " ), maxDate: new Date( " + max.Year + ", " + max.Month + " - 1, " + max.Day + " ) } );";
		}

		void ControlWithCustomFocusLogic.SetFocus() {
			( textBox as ControlWithCustomFocusLogic ).SetFocus();
		}

		/// <summary>
		/// Validates the date and returns the nullable date.
		/// </summary>
		public DateTime? ValidateAndGetNullablePostBackDate( PostBackValueDictionary postBackValues, Validator validator, ValidationErrorHandler errorHandler,
		                                                     bool allowEmpty ) {
			return validator.GetNullableDateTime( errorHandler, textBox.GetPostBackValue( postBackValues ), null, allowEmpty, min, max );
		}

		/// <summary>
		/// Do not use.
		/// </summary>
		public DateTime? ValidateAndGetNullableDate( Validator validator, ValidationErrorHandler errorHandler, bool allowEmpty ) {
			return ValidateAndGetNullablePostBackDate( AppRequestState.Instance.EwfPageRequestState.PostBackValues, validator, errorHandler, allowEmpty );
		}

		/// <summary>
		/// Validates the date and returns the date.
		/// </summary>
		public DateTime ValidateAndGetPostBackDate( PostBackValueDictionary postBackValues, Validator validator, ValidationErrorHandler errorHandler ) {
			return validator.GetDateTime( errorHandler, textBox.GetPostBackValue( postBackValues ), null, min, max );
		}

		/// <summary>
		/// Do not use.
		/// </summary>
		public DateTime ValidateAndGetDate( Validator validator, ValidationErrorHandler errorHandler ) {
			return ValidateAndGetPostBackDate( AppRequestState.Instance.EwfPageRequestState.PostBackValues, validator, errorHandler );
		}

		/// <summary>
		/// Returns true if the value changed on this post back.
		/// </summary>
		public bool ValueChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return textBox.ValueChangedOnPostBack( postBackValues );
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}