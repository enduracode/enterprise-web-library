using System;
using System.Collections.Generic;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A style that displays errors in a custom way.
	/// </summary>
	public class CustomErrorDisplayStyle: ErrorDisplayStyle {
		private readonly Func<IEnumerable<string>, IEnumerable<Control>> controlGetter;

		/// <summary>
		/// Creates a custom error-display style.
		/// </summary>
		/// <param name="controlGetter"></param>
		public CustomErrorDisplayStyle( Func<IEnumerable<string>, IEnumerable<Control>> controlGetter ) {
			this.controlGetter = controlGetter;
		}

		IEnumerable<Control> ErrorDisplayStyle.GetControls( IEnumerable<string> errors ) {
			return controlGetter( errors );
		}
	}
}