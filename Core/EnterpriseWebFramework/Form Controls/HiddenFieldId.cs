using System;
using System.Linq;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class HiddenFieldId {
		internal readonly ElementId ElementId;

		/// <summary>
		/// Creates a hidden-field ID.
		/// </summary>
		public HiddenFieldId() {
			ElementId = new ElementId();
		}

		/// <summary>
		/// Returns the JavaScript statements that should be executed to change the field value. Not available until after the page tree has been built.
		/// </summary>
		public string GetJsValueModificationStatements( string valueExpression ) {
			if( !ElementId.Id.Any() )
				throw new ApplicationException( "The hidden field must be on the page." );
			return "$( '#{0}' ).val( {1} ).change();".FormatWith( ElementId.Id, valueExpression );
		}
	}
}