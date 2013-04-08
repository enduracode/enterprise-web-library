using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;
using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A time picker.
	/// </summary>
	public class TimePicker: WebControl, ControlTreeDataLoader, ControlWithJsInitLogic, ControlWithCustomFocusLogic {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfTimePicker";

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				return new[] { new CssElement( "TimePicker", "div." + CssClass ) };
			}
		}

		private TimeSpan? value;
		private bool autoPostBack;
		private readonly int minuteInterval;

		private EwfTextBox textBox;

		/// <summary>
		/// Creates a time picker. The minute interval allows the user to select values only in the given increments. 
		/// Be aware that other values can still be sent from the browser via a crafted request.
		/// </summary>
		public TimePicker( TimeSpan? value, int minuteInterval = 15 ) {
			this.value = value;
			this.minuteInterval = minuteInterval;
		}

		/// <summary>
		/// Sets whether this time picker will post back automatically.
		/// </summary>
		public bool AutoPostBack { set { autoPostBack = value; } }

		/// <summary>
		/// EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property.
		/// </summary>
		public override string ToolTip { get; set; }

		/// <summary>
		/// Control to display inside the tool tip. Do not pass null. This will ignore the ToolTip property.
		/// </summary>
		public Control ToolTipControl { get; set; }

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			CssClass = CssClass.ConcatenateWithSpace( CssElementCreator.CssClass );

			textBox = new EwfTextBox( value.HasValue ? value.Value.ToTimeOfDayHourAndMinuteString() : "", preventAutoComplete: true ) { AutoPostBack = autoPostBack };
			Controls.Add( textBox );

			if( ToolTip != null || ToolTipControl != null )
				new ToolTip( ToolTipControl ?? EnterpriseWebFramework.Controls.ToolTip.GetToolTipTextControl( ToolTip ), this );
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			return "$( '#" + textBox.TextBoxClientId + "' ).timepicker( { ampm: true, timeFormat: 'h:mmt', stepMinute: " + minuteInterval + " } );";
		}

		void ControlWithCustomFocusLogic.SetFocus() {
			( textBox as ControlWithCustomFocusLogic ).SetFocus();
		}

		/// <summary>
		/// Validates the time and returns the nullable time. The value is expressed in time since 12AM on an arbitrary day.
		/// </summary>
		public TimeSpan? ValidateAndGetNullableTimeSpan( PostBackValueDictionary postBackValues, Validator validator, ValidationErrorHandler errorHandler,
		                                                 bool allowEmpty ) {
			return validator.GetNullableTimeOfDayTimeSpan( errorHandler,
			                                               textBox.GetPostBackValue( postBackValues ).ToUpper(),
			                                               DateTimeTools.HourAndMinuteFormat.ToSingleElementArray(),
			                                               allowEmpty );
		}

		/// <summary>
		/// Validates the time and returns the time. The value is expressed in time since 12AM on an arbitrary day.
		/// </summary>
		public TimeSpan ValidateAndGetTimeSpan( PostBackValueDictionary postBackValues, Validator validator, ValidationErrorHandler errorHandler ) {
			return validator.GetTimeOfDayTimeSpan( errorHandler,
			                                       textBox.GetPostBackValue( postBackValues ).ToUpper(),
			                                       DateTimeTools.HourAndMinuteFormat.ToSingleElementArray() );
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