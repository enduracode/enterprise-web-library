using System;
using System.Linq;
using System.Web.UI;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui.Entity;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Ui {
	/// <summary>
	/// Standard Library use only.
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
		/// Standard Library use only.
		/// </summary>
		public static AppEwfUiProvider AppProvider {
			get {
				if( provider == null ) {
					throw new ApplicationException( providerName + " provider not found in application. To implement, create a class named " + providerName +
					                                @"Provider in ""Your Web Site\Providers"" that derives from App" + providerName + "Provider." );
				}
				return provider;
			}
		}

		/// <summary>
		/// EwfUiMaster use only. Returns the tab mode, or null for no tabs. NOTE: Doesn't return null for no tabs yet.
		/// </summary>
		public static TabMode? GetTabMode( this EntitySetupInfo esInfo ) {
			var modeOverrider = esInfo as TabModeOverrider;
			if( modeOverrider == null )
				return TabMode.Vertical;
			var mode = modeOverrider.GetTabMode();
			if( mode == TabMode.Automatic )
				return ( esInfo.Resources.Count == 1 && esInfo.Resources.Single().Resources.Count() < 8 ) ? TabMode.Horizontal : TabMode.Vertical;
			return mode;
		}

		/// <summary>
		/// Gets the current EWF UI master page. Standard Library use only.
		/// </summary>
		public static AppEwfUiMasterPage AppMasterPage { get { return EwfPage.Instance.Master.Master != null ? getSecondLevelMaster( EwfPage.Instance.Master ) as AppEwfUiMasterPage : null; } }

		private static MasterPage getSecondLevelMaster( MasterPage master ) {
			return master.Master.Master == null ? master : getSecondLevelMaster( master.Master );
		}

		/// <summary>
		/// Sets the page actions. This must be called before EwfPage.LoadData finishes executing.
		/// </summary>
		public static void SetPageActions( params ActionButtonSetup[] actions ) {
			AppMasterPage.SetPageActions( actions );
		}

		/// <summary>
		/// Clears the content foot and adds the specified actions. This must be called before EwfPage.LoadData finishes executing. The first action, if it is a
		/// post back button, will use submit behavior.
		/// </summary>
		public static void SetContentFootActions( params ActionButtonSetup[] actions ) {
			AppMasterPage.SetContentFootActions( actions );
		}

		/// <summary>
		/// Clears the content foot and adds the specified controls. This must be called before EwfPage.LoadData finishes executing.
		/// </summary>
		public static void SetContentFootControls( params Control[] controls ) {
			AppMasterPage.SetContentFootControls( controls );
		}
	}
}