using System;
using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A behavior that shows a menu.
	/// </summary>
	public class MenuButtonBehavior: ButtonBehavior {
		private readonly ToolTip toolTip;
		private readonly Func<string, string> toolTipInitStatementGetter;

		/// <summary>
		/// Creates a menu behavior.
		/// </summary>
		/// <param name="menuContent"></param>
		/// <param name="menuTitle">A title to be displayed in the the menu.</param>
		public MenuButtonBehavior( IReadOnlyCollection<FlowComponent> menuContent, string menuTitle = "" ) {
			toolTip = new ToolTip( menuContent, out toolTipInitStatementGetter, title: menuTitle );
		}

		IEnumerable<Tuple<string, string>> ButtonBehavior.GetAttributes() {
			return Enumerable.Empty<Tuple<string, string>>();
		}

		bool ButtonBehavior.IncludesIdAttribute() {
			return true;
		}

		IReadOnlyCollection<EtherealComponent> ButtonBehavior.GetEtherealChildren() {
			return toolTip.ToCollection();
		}

		string ButtonBehavior.GetJsInitStatements( string id ) {
			return toolTipInitStatementGetter( id );
		}

		void ButtonBehavior.AddPostBack() {}
	}
}