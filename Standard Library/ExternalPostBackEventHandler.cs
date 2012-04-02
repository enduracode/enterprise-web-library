using System;
using System.Web.UI;

namespace RedStapler.StandardLibrary {
	internal class ExternalPostBackEventHandler: Control, IPostBackEventHandler {
		public event Action PostBackEvent;

		void IPostBackEventHandler.RaisePostBackEvent( string eventArgument ) {
			if( PostBackEvent != null )
				PostBackEvent();
		}
	}
}