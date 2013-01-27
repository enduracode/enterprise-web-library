using System;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements.Page {
	[ Obsolete( "Guaranteed through 28 February 2013." ) ]
	public interface PageWithRightButton {
		/// <summary>
		/// Returns information about the right button.
		/// </summary>
		NavButtonSetup CreateRightButtonSetup();
	}
}