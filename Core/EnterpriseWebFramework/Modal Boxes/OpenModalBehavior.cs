using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A behavior that opens a modal box.
	/// </summary>
	public class OpenModalBehavior: ButtonBehavior {
		private readonly ModalBoxId modalBoxId;

		/// <summary>
		/// Creates an open-modal behavior.
		/// </summary>
		/// <param name="modalBoxId">Do not pass null.</param>
		public OpenModalBehavior( ModalBoxId modalBoxId ) {
			this.modalBoxId = modalBoxId;
		}

		IEnumerable<Tuple<string, string>> ButtonBehavior.GetAttributes() {
			return Enumerable.Empty<Tuple<string, string>>();
		}

		bool ButtonBehavior.IncludesIdAttribute() {
			return true;
		}

		IEnumerable<EtherealComponent> ButtonBehavior.GetEtherealChildren() {
			return Enumerable.Empty<EtherealComponent>();
		}

		string ButtonBehavior.GetJsInitStatements( string id ) {
			if( !modalBoxId.ElementId.Id.Any() )
				throw new ApplicationException( "The modal box must be on the page." );
			return "$( '#{0}' ).click( function() {{ document.getElementById( '{1}' ).showModal(); }} );".FormatWith( id, modalBoxId.ElementId.Id );
		}

		void ButtonBehavior.AddPostBack() {}
	}
}