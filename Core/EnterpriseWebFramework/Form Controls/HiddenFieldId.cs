using System;
using System.Linq;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class HiddenFieldId {
		private readonly ElementId id;

		/// <summary>
		/// Creates a hidden-field ID.
		/// </summary>
		public HiddenFieldId() {
			id = new ElementId();
		}

		internal void AddId( string id ) {
			this.id.AddId( id );
		}

		/// <summary>
		/// Returns the JavaScript statements that should be executed to change the field value. Not available until after the page tree has been built.
		/// </summary>
		public string GetJsValueModificationStatements( string valueExpression ) {
			if( !id.Id.Any() )
				throw new ApplicationException( "The hidden field must be on the page." );
			return "$( '#{0}' ).val( {1} ).change();".FormatWith( id.Id, valueExpression );
		}

		/// <summary>
		/// Section use only.
		/// </summary>
		internal ElementId ElementId => id;
	}
}