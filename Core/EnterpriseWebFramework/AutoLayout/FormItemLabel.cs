using System;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A label for a form item.
	/// </summary>
	public class FormItemLabel {
		public static implicit operator FormItemLabel( string text ) {
			return new FormItemLabel( text );
		}

		public static implicit operator FormItemLabel( Control control ) {
			return new FormItemLabel( control );
		}

		internal readonly string Text;
		internal readonly Control Control;

		private FormItemLabel( string text ) {
			if( text == null )
				throw new ApplicationException( "Text cannot be null." );
			Text = text;
		}

		private FormItemLabel( Control control ) {
			Control = control;
		}
	}
}