using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.InputValidation;
using Humanizer;
using NodaTime;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A time picker.
	/// </summary>
	public class TimePicker: WebControl, ControlTreeDataLoader, ControlWithJsInitLogic {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfTimePicker";

			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
				return new[] { new CssElement( "TimePicker", "div." + CssClass ) };
			}
		}

		private readonly TimeSpan? value;
		private bool autoPostBack;
		private readonly LocalTime minValue;
		private readonly LocalTime? maxValue;
		private readonly int minuteInterval;
		private readonly FormAction action;
		private readonly IReadOnlyCollection<DataModification> dataModifications;

		private EwfTextBox textBox;
		private SelectList<TimeSpan?> selectList;

		/// <summary>
		/// Creates a time picker. The minute interval allows the user to select values only in the given increments. 
		/// Be aware that other values can still be sent from the browser via a crafted request.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="minValue">The earliest allowed time.</param>
		/// <param name="maxValue">The latest allowed time. This can be earlier than <paramref name="minValue"/> to create a range spanning midnight.</param>
		/// <param name="minuteInterval"></param>
		/// <param name="action">The post-back that will be performed when the user hits Enter on the time picker.</param>
		public TimePicker( TimeSpan? value, LocalTime? minValue = null, LocalTime? maxValue = null, int minuteInterval = 15, FormAction action = null ) {
			this.value = value;
			this.minValue = minValue ?? LocalTime.Midnight;
			this.maxValue = maxValue;
			this.minuteInterval = minuteInterval;
			this.action = action;

			dataModifications = FormState.Current.DataModifications;
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

		void ControlTreeDataLoader.LoadData() {
			FormState.ExecuteWithDataModificationsAndDefaultAction(
				dataModifications,
				() => {
					CssClass = CssClass.ConcatenateWithSpace( CssElementCreator.CssClass );

					if( minuteInterval < 30 ) {
						textBox = new EwfTextBox(
							value.HasValue ? value.Value.ToTimeOfDayHourAndMinuteString() : "",
							disableBrowserAutoComplete: true,
							action: action,
							autoPostBack: autoPostBack );
						Controls.Add( new ControlLine( textBox, getIconButton() ) );
					}
					else {
						selectList = SelectList.CreateDropDown(
							from time in getTimes()
							let timeSpan = new TimeSpan( time.TickOfDay )
							select SelectListItem.Create<TimeSpan?>( timeSpan, timeSpan.ToTimeOfDayHourAndMinuteString() ),
							value,
							width: Unit.Percentage( 100 ),
							placeholderIsValid: true,
							placeholderText: "",
							action: action,
							autoPostBack: autoPostBack );
						Controls.Add( selectList );
					}

					if( ToolTip != null || ToolTipControl != null )
						new ToolTip( ToolTipControl ?? EnterpriseWebFramework.Controls.ToolTip.GetToolTipTextControl( ToolTip ), this );
				} );
		}

		private WebControl getIconButton() {
			var icon = new FontAwesomeIcon( "fa-clock-o", "timepickerIcon" ).ToCollection().GetControls();
			var style = new CustomActionControlStyle( control => control.AddControlsReturnThis( icon ) );
			return new CustomButton( () => "$( '#{0}' ).timepicker( 'show' )".FormatWith( textBox.TextBoxClientId ) )
				{
					ActionControlStyle = style, CssClass = "icon"
				};
		}

		private IReadOnlyCollection<LocalTime> getTimes() {
			var times = new List<LocalTime>();
			var time = minValue;
			var wrapAllowed = maxValue < minValue;
			while( true ) {
				times.Add( time );
				time = time.PlusMinutes( minuteInterval );

				if( time < times.Last() )
					if( wrapAllowed )
						wrapAllowed = false;
					else
						break;

				if( !wrapAllowed && time > maxValue )
					break;
			}
			return times;
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			if( textBox == null )
				return "";
			return "$( '#" + textBox.TextBoxClientId + "' ).timepicker( { timeFormat: 'h:mmt', stepMinute: " + minuteInterval + ", showButtonPanel: false } );";
		}

		/// <summary>
		/// Validates the time and returns the nullable time. The value is expressed in time since 12AM on an arbitrary day.
		/// </summary>
		public TimeSpan? ValidateAndGetNullableTimeSpan(
			PostBackValueDictionary postBackValues, Validator validator, ValidationErrorHandler errorHandler, bool allowEmpty ) {
			if( textBox != null ) {
				var postBackValue = validator.GetNullableTimeOfDayTimeSpan(
					errorHandler,
					textBox.GetPostBackValue( postBackValues ).ToUpper(),
					DateTimeTools.HourAndMinuteFormat.ToCollection().ToArray(),
					allowEmpty );
				if( postBackValue < new TimeSpan( minValue.TickOfDay ) || postBackValue > maxValue.ToNewUnderlyingValue( v => new TimeSpan( v.TickOfDay ) ) )
					validator.NoteErrorAndAddMessage( "The time is too early or too late." );
				return postBackValue;
			}

			return selectList.ValidateAndGetSelectedItemIdInPostBack( postBackValues, validator );
		}

		/// <summary>
		/// Validates the time and returns the time. The value is expressed in time since 12AM on an arbitrary day.
		/// </summary>
		public TimeSpan ValidateAndGetTimeSpan( PostBackValueDictionary postBackValues, Validator validator, ValidationErrorHandler errorHandler ) {
			if( textBox != null ) {
				var postBackValue = validator.GetTimeOfDayTimeSpan(
					errorHandler,
					textBox.GetPostBackValue( postBackValues ).ToUpper(),
					DateTimeTools.HourAndMinuteFormat.ToCollection().ToArray() );
				if( postBackValue < new TimeSpan( minValue.TickOfDay ) || postBackValue > maxValue.ToNewUnderlyingValue( v => new TimeSpan( v.TickOfDay ) ) )
					validator.NoteErrorAndAddMessage( "The time is too early or too late." );
				return postBackValue;
			}

			var selectedItemIdInPostBack = selectList.ValidateAndGetSelectedItemIdInPostBack( postBackValues, validator );
			if( selectedItemIdInPostBack.HasValue )
				return selectedItemIdInPostBack.Value;
			errorHandler.SetValidationResult( ValidationResult.Empty() );
			errorHandler.HandleResult( validator, true );
			return default( TimeSpan );
		}

		/// <summary>
		/// Returns true if the value changed on this post back.
		/// </summary>
		public bool ValueChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return textBox != null ? textBox.ValueChangedOnPostBack( postBackValues ) : selectList.SelectionChangedOnPostBack( postBackValues );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}