using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayLinking {
	/// <summary>
	/// A link between a free-form radio button list and an array of dependent controls.
	/// </summary>
	public static class FreeFormRadioListToControlArrayDisplayLink {
		/// <summary>
		/// Creates a new display link and adds it to the current EwfPage.
		/// </summary>
		public static void AddToPage<ItemIdType>( FreeFormRadioList<ItemIdType> radioList, ItemIdType selectedValue, bool controlsVisibleOnValue,
		                                          params WebControl[] controls ) {
			EwfPage.Instance.AddDisplayLink( new FreeFormRadioListToControlArrayDisplayLink<ItemIdType>( radioList, selectedValue, controlsVisibleOnValue, controls ) );
		}
	}

	/// <summary>
	/// A link between a free-form radio button list and an array of dependent controls.
	/// </summary>
	public class FreeFormRadioListToControlArrayDisplayLink<ItemIdType>: DisplayLink {
		private readonly FreeFormRadioList<ItemIdType> radioList;
		private readonly ItemIdType selectedValue;
		private readonly bool controlsVisibleOnValue;
		private readonly IEnumerable<WebControl> controls;

		internal FreeFormRadioListToControlArrayDisplayLink( FreeFormRadioList<ItemIdType> radioList, ItemIdType selectedValue, bool controlsVisibleOnValue,
		                                                     IEnumerable<WebControl> controls ) {
			this.radioList = radioList;
			this.selectedValue = selectedValue;
			this.controlsVisibleOnValue = controlsVisibleOnValue;
			this.controls = controls.ToArray();
		}

		void DisplayLink.AddJavaScript() {
			foreach( var pair in radioList.ItemIdsAndCheckBoxes ) {
				if( StandardLibraryMethods.AreEqual( pair.Item1, selectedValue ) )
					DisplayLinkingOps.AddDisplayJavaScriptToCheckBox( pair.Item2, controlsVisibleOnValue, controls.ToArray() );
				else
					DisplayLinkingOps.AddDisplayJavaScriptToCheckBox( pair.Item2, !controlsVisibleOnValue, controls.ToArray() );
			}
		}

		void DisplayLink.SetInitialDisplay( PostBackValueDictionary formControlValues ) {
			var itemIdsMatch = StandardLibraryMethods.AreEqual( radioList.GetSelectedItemIdInPostBack( formControlValues ), selectedValue );
			var visible = ( controlsVisibleOnValue && itemIdsMatch ) || ( !controlsVisibleOnValue && !itemIdsMatch );
			foreach( var c in controls )
				DisplayLinkingOps.SetControlDisplay( c, visible );
		}
	}
}