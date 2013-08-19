using System;
using System.Web.UI;

namespace RedStapler.StandardLibrary {
	internal class ExternalPostBackEventHandler: Control, IPostBackEventHandler {
		private readonly Action handler;

		internal ExternalPostBackEventHandler( Action handler ) {
			this.handler = handler;
		}

		void IPostBackEventHandler.RaisePostBackEvent( string eventArgument ) {
			handler();
		}
	}
}