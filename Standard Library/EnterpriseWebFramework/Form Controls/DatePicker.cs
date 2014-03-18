using System;
using System.Web.UI;
using System.Web.UI.WebControls;
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
		private readonly PostBack postBack;

		private EwfTextBox textBox;
		private DateTime min;
		private DateTime max;

		/// <summary>
		/// Creates a date picker.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="postBack">The post-back that will be performed when the user hits Enter on the date picker.</param>
		public DatePicker( DateTime? value, PostBack postBack = null ) {
			this.value = value.HasValue ? value.Value.Date as DateTime? : null;
			this.postBack = postBack;
		}

		/// <summary>
		/// Sets whether this date picker will post back automatically.
		/// </summary>
		public bool AutoPostBack { set { autoPostBack = value; } }

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

		void ControlTreeDataLoader.LoadData() {
			CssClass = CssClass.ConcatenateWithSpace( CssElementCreator.CssClass );

			textBox = new EwfTextBox( value.HasValue ? value.Value.ToMonthDayYearString() : "",
			                          disableBrowserAutoComplete: true,
			                          postBack: postBack,
			                          autoPostBack: autoPostBack );

			Controls.Add( new ControlLine( textBox, getIconButton() ) );

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

		private WebControl getIconButton() {
			var icon = new LiteralControl { Text = @"<i class=""{0}""></i>".FormatWith( "fa fa-calendar datepickerIcon" ) };
			var style = new CustomActionControlStyle( control => control.AddControlsReturnThis( icon ) );
			return new CustomButton( () => "$( '#{0}' ).datepicker( 'show' )".FormatWith( textBox.TextBoxClientId ) ) { ActionControlStyle = style, CssClass = "icon" };
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
			var date = validator.GetNullableDateTime( errorHandler, textBox.GetPostBackValue( postBackValues ), null, allowEmpty, min, max );
			if( errorHandler.LastResult == ErrorCondition.NoError && date.HasTime() )
				validator.NoteErrorAndAddMessage( "Time information is not allowed." );
			return date;
		}

		/// <summary>
		/// Validates the date and returns the date.
		/// </summary>
		public DateTime ValidateAndGetPostBackDate( PostBackValueDictionary postBackValues, Validator validator, ValidationErrorHandler errorHandler ) {
			var date = validator.GetDateTime( errorHandler, textBox.GetPostBackValue( postBackValues ), null, min, max );
			if( errorHandler.LastResult == ErrorCondition.NoError && date.HasTime() )
				validator.NoteErrorAndAddMessage( "Time information is not allowed." );
			return date;
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