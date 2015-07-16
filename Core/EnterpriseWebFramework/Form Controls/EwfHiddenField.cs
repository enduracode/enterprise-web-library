using System;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A hidden field.
	/// </summary>
	public class EwfHiddenField: WebControl, ControlTreeDataLoader, FormControl, EtherealControl {
		/// <summary>
		/// Creates a hidden field. Do not pass null for value.
		/// </summary>
		public static void Create( string value, Action<string> postBackValueHandler, ValidationList vl, out Func<PostBackValueDictionary, string> valueGetter,
		                           out Func<string> clientIdGetter ) {
			var control = new EwfHiddenField( value );
			EwfPage.Instance.AddEtherealControl( control );
			new EwfValidation( ( postBackValues, validator ) => postBackValueHandler( control.getPostBackValue( postBackValues ) ), vl );
			valueGetter = control.getPostBackValue;
			clientIdGetter = () => control.ClientID;
		}

		private readonly FormValue<string> formValue;

		private EwfHiddenField( string value ) {
			formValue = new FormValue<string>( () => value,
			                                   () => UniqueID,
			                                   v => v,
			                                   rawValue =>
			                                   rawValue != null
				                                   ? PostBackValueValidationResult<string>.CreateValidWithValue( rawValue )
				                                   : PostBackValueValidationResult<string>.CreateInvalid() );
		}

		WebControl EtherealControl.Control { get { return this; } }

		void ControlTreeDataLoader.LoadData() {
			Attributes.Add( "name", UniqueID );
			PreRender += delegate { Attributes.Add( "value", formValue.GetValue( AppRequestState.Instance.EwfPageRequestState.PostBackValues ) ); };
			Attributes.Add( "type", "hidden" );
		}

		string EtherealControl.GetJsInitStatements() {
			return "";
		}

		FormValue FormControl.FormValue { get { return formValue; } }

		/// <summary>
		/// Gets the post back value.
		/// </summary>
		private string getPostBackValue( PostBackValueDictionary postBackValues ) {
			return formValue.GetValue( postBackValues );
		}

		/// <summary>
		/// Returns true if the value changed on this post back.
		/// </summary>
		public bool ValueChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return formValue.ValueChangedOnPostBack( postBackValues );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Input; } }
	}
}