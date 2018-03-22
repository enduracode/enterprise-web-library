using System;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal sealed class ElementNode: WebControl, FlowComponentOrNode, EtherealComponentOrElement, ControlTreeDataLoader, FormValueControl,
		ControlWithJsInitLogic, EtherealControl {
		internal readonly Func<ElementContext, ElementNodeData> ElementDataGetter;
		internal readonly FormValue FormValue;

		// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
		private Func<ElementNodeLocalData> webFormsLocalDataGetter;
		private ElementNodeLocalData webFormsLocalData;
		private bool isFocused;
		private ElementNodeFocusDependentData webFormsFocusDependentData;

		public ElementNode( Func<ElementContext, ElementNodeData> elementDataGetter, FormValue formValue = null ): base( HtmlTextWriterTag.Unknown ) {
			ElementDataGetter = elementDataGetter;
			FormValue = formValue;
		}


		// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.

		void ControlTreeDataLoader.LoadData() {
			var elementData = ElementDataGetter( new ElementContext( ClientID ) );
			this.AddControlsReturnThis( elementData.Children.GetControls() );
			elementData.EtherealChildren.AddEtherealControls( this );
			webFormsLocalDataGetter = elementData.LocalDataGetter;
		}

		WebControl EtherealControl.Control => this;

		internal bool IsFocusable {
			get {
				if( webFormsLocalDataGetter == null )
					throw new ApplicationException( "webFormsLocalDataGetter not set" );
				webFormsLocalData = webFormsLocalDataGetter();
				return webFormsLocalData.IsFocusable;
			}
		}

		internal void SetIsFocused() {
			isFocused = true;
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			return getJsInitStatements();
		}

		string EtherealControl.GetJsInitStatements() {
			return getJsInitStatements();
		}

		private string getJsInitStatements() {
			if( webFormsLocalData == null )
				throw new ApplicationException( "webFormsLocalData not set" );
			webFormsFocusDependentData = webFormsLocalData.FocusDependentDataGetter( isFocused );
			return webFormsFocusDependentData.JsInitStatements;
		}

		FormValue FormValueControl.FormValue => FormValue;

		protected override void Render( HtmlTextWriter writer ) {
			if( webFormsLocalData == null )
				throw new ApplicationException( "webFormsLocalData not set" );
			if( webFormsLocalData.ElementName == "br" )
				writer.WriteBreak();
			else
				base.Render( writer );
		}

		protected override void AddAttributesToRender( HtmlTextWriter writer ) {
			if( webFormsFocusDependentData == null )
				throw new ApplicationException( "webFormsFocusDependentData not set" );
			foreach( var i in webFormsFocusDependentData.Attributes )
				writer.AddAttribute( i.Item1, i.Item2 );
			if( webFormsFocusDependentData.Id != null )
				writer.AddAttribute( HtmlTextWriterAttribute.Id, webFormsFocusDependentData.Id.Any() ? webFormsFocusDependentData.Id : ClientID );
		}

		protected override string TagName {
			get {
				if( webFormsLocalData == null )
					throw new ApplicationException( "webFormsLocalData not set" );
				return webFormsLocalData.ElementName;
			}
		}
	}
}