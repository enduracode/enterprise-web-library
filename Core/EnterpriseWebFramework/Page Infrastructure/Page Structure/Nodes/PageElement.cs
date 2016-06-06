using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class PageElement: WebControl, EtherealElement, FlowComponent, EtherealComponent, ControlTreeDataLoader, FormValueControl, ControlWithJsInitLogic,
		EtherealControl {
		internal readonly Func<ElementContext, ElementData> ElementDataGetter;
		internal readonly FormValue FormValue;

		// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
		private Func<ElementLocalData> webFormsLocalDataGetter;
		private IEnumerable<Tuple<string, string>> webFormsAttributes;
		private string webFormsElementName;

		public PageElement( Func<ElementContext, ElementData> elementDataGetter, FormValue formValue = null ) {
			ElementDataGetter = elementDataGetter;
			FormValue = formValue;

			PreRender += handlePreRender;
		}

		IEnumerable<PageNode> FlowComponent.GetNodes() {
			return this.ToSingleElementArray();
		}

		IEnumerable<EtherealElement> EtherealComponent.GetElements() {
			return this.ToSingleElementArray();
		}


		// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.

		void ControlTreeDataLoader.LoadData() {
			var elementData = ElementDataGetter( new ElementContext( UniqueID ) );
			this.AddControlsReturnThis( elementData.Children.GetControls() );
			elementData.EtherealChildren.AddEtherealControls( this );
			webFormsLocalDataGetter = elementData.LocalDataGetter;
		}

		WebControl EtherealControl.Control { get { return this; } }

		string ControlWithJsInitLogic.GetJsInitStatements() {
			return getJsInitStatements();
		}

		string EtherealControl.GetJsInitStatements() {
			return getJsInitStatements();
		}

		private string getJsInitStatements() {
			if( webFormsLocalDataGetter == null )
				throw new ApplicationException( "webFormsLocalDataGetter not set" );
			var localData = webFormsLocalDataGetter();
			webFormsAttributes = localData.Attributes;
			webFormsElementName = localData.ElementName;
			return localData.JsInitStatements;
		}

		FormValue FormValueControl.FormValue { get { return FormValue; } }

		private void handlePreRender( object sender, EventArgs e ) {
			if( webFormsAttributes == null )
				throw new ApplicationException( "webFormsAttributes not set" );
			foreach( var i in webFormsAttributes )
				Attributes.Add( i.Item1, i.Item2 );
		}

		protected override string TagName {
			get {
				if( webFormsElementName == null )
					throw new ApplicationException( "webFormsElementName not set" );
				return webFormsElementName;
			}
		}
	}
}