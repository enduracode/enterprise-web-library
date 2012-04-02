using System;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.JavaScriptWriting;
using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A time duration picker.
	/// </summary>
	public class DurationPicker: WebControl, ControlTreeDataLoader, ControlWithCustomFocusLogic {
		private const int maxValueLength = 6; // Also defined in JavaScript
		private readonly EwfTextBox durationPicker;

		/// <summary>
		/// Creates a duration picker.
		/// </summary>
		public DurationPicker( TimeSpan value ) {
			durationPicker = new EwfTextBox( Math.Floor( value.TotalHours ).ToString( "0000" ) + ":" + value.Minutes.ToString( "00" ) );
		}

		/// <summary>
		/// EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property.
		/// </summary>
		public override string ToolTip { get { return durationPicker.ToolTip; } set { durationPicker.ToolTip = value; } }

		/// <summary>
		/// Control to display inside the tool tip. Do not pass null. This will ignore the ToolTip property.
		/// </summary>
		public Control ToolTipControl { get { return durationPicker.ToolTipControl; } set { durationPicker.ToolTipControl = value; } }

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			durationPicker.Width = Unit.Pixel( 65 );
			durationPicker.AddJavaScriptEventScript( JsWritingMethods.onblur, "ApplyTimeSpanFormat(this)" );
			durationPicker.AddJavaScriptEventScript( JsWritingMethods.onkeypress, "return NumericalOnly(event, this)" );
			durationPicker.AddJavaScriptEventScript( JsWritingMethods.onfocus, "this.value = this.value.replace(':',''); this.select()" );
			durationPicker.ToolTip = "hours:minutes";
			base.Controls.Add( durationPicker );
		}

		/// <summary>
		/// Validates and returns the duration, a secondary validation in case javascript is disabled on the client system.
		/// </summary>
		public TimeSpan ValidateAndGetPostBackDuration( PostBackValueDictionary postBackValues, Validator validator, ValidationErrorHandler validationErrorHandler ) {
			if( tooLongOrInvalidCharacters( durationPicker.GetPostBackValue( postBackValues ) ) ) {
				validator.NoteErrorAndAddMessage( "Please enter a valid duration." );
				return TimeSpan.Zero;
			}
			return validator.GetTimeSpan( validationErrorHandler, parseTimeSpan( durationPicker.GetPostBackValue( postBackValues ) ) );
		}

		/// <summary>
		/// Do not use.
		/// </summary>
		public TimeSpan ValidateAndGetDuration( Validator validator, ValidationErrorHandler validationErrorHandler ) {
			return ValidateAndGetPostBackDuration( AppRequestState.Instance.EwfPageRequestState.PostBackValues, validator, validationErrorHandler );
		}

		/// <summary>
		/// Supports ':' being present or not.
		/// Requires a value. Must be less than maxValueLength.
		/// May only contain numbers.
		/// </summary>
		private static bool tooLongOrInvalidCharacters( string value ) {
			return ( value.Length > ( value.Contains( ":" ) ? maxValueLength + 1 : maxValueLength ) || value.Length == 0 ) ||
			       !( value.Equals( Regex.Replace( value, "[^0-9:]", "" ) ) );
		}

		/// <summary>
		/// Supports browsers with Javascript disabled.
		/// </summary>
		private static TimeSpan parseTimeSpan( string value ) {
			if( value.Contains( ":" ) ) {
				var splitPartsArray = value.Split( ':' );
				return new TimeSpan( int.Parse( splitPartsArray[ 0 ] ), int.Parse( splitPartsArray[ 1 ] ), 0 );
			}
			// This section supports browsers without script, which we typically don't actually support. However, this code already
			// supported no Javascript, and now despite no evidence to support it, I fear something may be relying on this behavior.
			var intValue = int.Parse( value );
			var hours = (int)( intValue * .01 );
			var minutes = intValue % 100;
			if( minutes > 59 )
				minutes = 59;
			return new TimeSpan( hours, minutes, 0 );
		}

		/// <summary>
		/// Returns true if the value changed on this post back.
		/// </summary>
		public bool ValueChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return durationPicker.ValueChangedOnPostBack( postBackValues );
		}

		void ControlWithCustomFocusLogic.SetFocus() {
			( durationPicker as ControlWithCustomFocusLogic ).SetFocus();
		}
	}
}