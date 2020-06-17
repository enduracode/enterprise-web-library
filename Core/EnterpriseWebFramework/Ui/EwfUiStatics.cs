using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui.Entity;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Ui {
	/// <summary>
	/// EWL use only.
	/// </summary>
	public static class EwfUiStatics {
		private const string providerName = "EwfUi";
		private static AppEwfUiProvider provider;

		internal static void Init( Type globalType ) {
			var appAssembly = globalType.Assembly;
			var typeName = globalType.Namespace + ".Providers." + providerName + "Provider";

			if( appAssembly.GetType( typeName ) != null )
				provider = appAssembly.CreateInstance( typeName ) as AppEwfUiProvider;
		}

		/// <summary>
		/// EWL use only.
		/// </summary>
		public static AppEwfUiProvider AppProvider {
			get {
				if( provider == null )
					throw new ApplicationException(
						providerName + " provider not found in application. To implement, create a class named " + providerName +
						@"Provider in ""Your Web Site\Providers"" that derives from App" + providerName + "Provider." );
				return provider;
			}
		}

		/// <summary>
		/// EwfUiMaster use only. Returns the tab mode, or null for no tabs.
		/// </summary>
		public static TabMode? GetTabMode( this EntitySetupInfo esInfo ) {
			if( !esInfo.Resources.Any() )
				return null;
			var mode = ( (EwfUiEntitySetupInfo)esInfo ).GetTabMode();
			if( mode == TabMode.Automatic )
				return ( esInfo.Resources.Count == 1 && esInfo.Resources.Single().Resources.Count() < 8 ) ? TabMode.Horizontal : TabMode.Vertical;
			return mode;
		}

		/// <summary>
		/// Gets the current EWF UI master page. EWL use only.
		/// </summary>
		public static AppEwfUiMasterPage AppMasterPage {
			get { return EwfPage.Instance.Master.Master != null ? getSecondLevelMaster( EwfPage.Instance.Master ) as AppEwfUiMasterPage : null; }
		}

		private static MasterPage getSecondLevelMaster( MasterPage master ) {
			return master.Master.Master == null ? master : getSecondLevelMaster( master.Master );
		}

		/// <summary>
		/// Omits the box-style effect around the page content. Useful when all content is contained within multiple box-style sections. This must be called before
		/// EwfPage.LoadData finishes executing.
		/// </summary>
		public static void OmitContentBox() {
			AppMasterPage.OmitContentBox();
		}

		/// <summary>
		/// Sets the page actions. This must be called before EwfPage.LoadData finishes executing.
		/// </summary>
		/// <param name="actions">Do not pass null.</param>
		public static void SetPageActions( IReadOnlyCollection<ActionComponentSetup> actions ) {
			AppMasterPage.SetPageActions( actions );
		}

		/// <summary>
		/// Clears the content foot and adds the specified actions. This must be called before EwfPage.LoadData finishes executing. The first action, if it is a
		/// post-back, will produce a submit button.
		/// </summary>
		/// <param name="actions">Do not pass null.</param>
		public static void SetContentFootActions( IReadOnlyCollection<ButtonSetup> actions ) {
			AppMasterPage.SetContentFootActions( actions );
		}

		/// <summary>
		/// Clears the content foot and adds the specified components. This must be called before EwfPage.LoadData finishes executing.
		/// </summary>
		public static void SetContentFootComponents( IReadOnlyCollection<FlowComponent> components ) {
			AppMasterPage.SetContentFootComponents( components );
		}
	}
}