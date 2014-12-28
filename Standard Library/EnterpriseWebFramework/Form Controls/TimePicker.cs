using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
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
		private SelectList<TimeSpan?> selectList;

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

		void ControlTreeDataLoader.LoadData() {
			CssClass = CssClass.ConcatenateWithSpace( CssElementCreator.CssClass );

			if( minuteInterval < 30 ) {
				textBox = new EwfTextBox( value.HasValue ? value.Value.ToTimeOfDayHourAndMinuteString() : "", disableBrowserAutoComplete: true, autoPostBack: autoPostBack );
				Controls.Add( new ControlLine( textBox, getIconButton() ) );
			}
			else {
				var minuteValues = new List<int>();
				for( var i = 0; i < 60; i += minuteInterval )
					minuteValues.Add( i );
				selectList = SelectList.CreateDropDown(
					from hour in Enumerable.Range( 0, 24 )
					from minute in minuteValues
					let timeSpan = new TimeSpan( hour, minute, 0 )
					select SelectListItem.Create<TimeSpan?>( timeSpan, timeSpan.ToTimeOfDayHourAndMinuteString() ),
					value,
					width: Unit.Percentage( 100 ),
					placeholderIsValid: true,
					placeholderText: "",
					autoPostBack: autoPostBack );
				Controls.Add( selectList );
			}

			if( ToolTip != null || ToolTipControl != null )
				new ToolTip( ToolTipControl ?? EnterpriseWebFramework.Controls.ToolTip.GetToolTipTextControl( ToolTip ), this );
		}

		private WebControl getIconButton() {
			var icon = new LiteralControl { Text = @"<i class=""{0}""></i>".FormatWith( "fa fa-clock-o timepickerIcon" ) };
			var style = new CustomActionControlStyle( control => control.AddControlsReturnThis( icon ) );
			return new CustomButton( () => "$( '#{0}' ).timepicker( 'show' )".FormatWith( textBox.TextBoxClientId ) ) { ActionControlStyle = style, CssClass = "icon" };
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			if( textBox == null )
				return "";
			return "$( '#" + textBox.TextBoxClientId + "' ).timepicker( { timeFormat: 'h:mmt', stepMinute: " + minuteInterval + ", showButtonPanel: false } );";
		}

		void ControlWithCustomFocusLogic.SetFocus() {
			if( textBox != null )
				( (ControlWithCustomFocusLogic)textBox ).SetFocus();
			else
				( (ControlWithCustomFocusLogic)selectList ).SetFocus();
		}

		/// <summary>
		/// Validates the time and returns the nullable time. The value is expressed in time since 12AM on an arbitrary day.
		/// </summary>
		public TimeSpan? ValidateAndGetNullableTimeSpan(
			PostBackValueDictionary postBackValues, Validator validator, ValidationErrorHandler errorHandler, bool allowEmpty ) {
			return textBox != null
				       ? validator.GetNullableTimeOfDayTimeSpan(
					       errorHandler,
					       textBox.GetPostBackValue( postBackValues ).ToUpper(),
					       DateTimeTools.HourAndMinuteFormat.ToSingleElementArray(),
					       allowEmpty )
				       : selectList.ValidateAndGetSelectedItemIdInPostBack( postBackValues, validator );
		}

		/// <summary>
		/// Validates the time and returns the time. The value is expressed in time since 12AM on an arbitrary day.
		/// </summary>
		public TimeSpan ValidateAndGetTimeSpan( PostBackValueDictionary postBackValues, Validator validator, ValidationErrorHandler errorHandler ) {
			if( textBox != null ) {
				return validator.GetTimeOfDayTimeSpan(
					errorHandler,
					textBox.GetPostBackValue( postBackValues ).ToUpper(),
					DateTimeTools.HourAndMinuteFormat.ToSingleElementArray() );
			}

			var selectedItemIdInPostBack = selectList.ValidateAndGetSelectedItemIdInPostBack( postBackValues, validator );
			if( selectedItemIdInPostBack.HasValue )
				return selectedItemIdInPostBack.Value;
			validator.NoteErrorAndAddMessage( "Please make a selection." );
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