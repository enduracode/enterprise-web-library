using System;
using System.Collections.Specialized;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A hidden field.
	/// </summary>
	public class EwfHiddenField: WebControl, IPostBackDataHandler, ControlTreeDataLoader, FormControl<string>, EtherealControl {
		/// <summary>
		/// Creates a hidden field. Do not pass null for value.
		/// </summary>
		public static void Create( string value, Action<string> postBackValueHandler, ValidationList vl, out Func<PostBackValueDictionary, string> valueGetter,
		                           out Func<string> clientIdGetter ) {
			var control = new EwfHiddenField( value );
			EwfPage.Instance.AddEtherealControl( control );
			new Validation( ( postBackValues, validator ) => postBackValueHandler( control.getPostBackValue( postBackValues ) ), vl );
			valueGetter = control.getPostBackValue;
			clientIdGetter = () => control.ClientID;
		}

		private readonly string durableValue;
		private string postValue;

		private EwfHiddenField( string value ) {
			durableValue = value;
		}

		string FormControl<string>.DurableValue { get { return durableValue; } }
		string FormControl.DurableValueAsString { get { return durableValue; } }

		WebControl EtherealControl.Control { get { return this; } }

		void ControlTreeDataLoader.LoadData() {
			Attributes.Add( "name", UniqueID );
			Attributes.Add( "value", AppRequestState.Instance.EwfPageRequestState.PostBackValues.GetValue( this ) );
			Attributes.Add( "type", "hidden" );
		}

		string EtherealControl.GetJsInitStatements() {
			return "";
		}

		bool IPostBackDataHandler.LoadPostData( string postDataKey, NameValueCollection postCollection ) {
			postValue = postCollection[ postDataKey ];
			return false;
		}

		void FormControl.AddPostBackValueToDictionary( PostBackValueDictionary postBackValues ) {
			postBackValues.Add( this, postValue );
		}

		/// <summary>
		/// Gets the post back value.
		/// </summary>
		private string getPostBackValue( PostBackValueDictionary postBackValues ) {
			return postBackValues.GetValue( this );
		}

		/// <summary>
		/// Returns true if the value changed on this post back.
		/// </summary>
		public bool ValueChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return postBackValues.ValueChangedOnPostBack( this );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Input; } }

		void IPostBackDataHandler.RaisePostDataChangedEvent() {}
	}
}