#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The general configuration for a form-item list.
	/// </summary>
	public class FormItemListSetup {
		internal readonly DisplaySetup DisplaySetup;
		internal readonly ElementClassSet Classes;
		internal readonly Func<Func<DisplaySetup, FormItemSetup>, IReadOnlyCollection<FormItem>> ButtonItemGetter;
		internal readonly IReadOnlyCollection<EtherealComponent> EtherealContent;

		/// <summary>
		/// Creates a form-item-list setup object.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the list.</param>
		/// <param name="buttonSetup">Pass a value to have a button added as the last form item and formatted automatically.</param>
		/// <param name="enableSubmitButton">Pass true to enable the button to be a a submit button if possible.</param>
		/// <param name="etherealContent"></param>
		public FormItemListSetup(
			DisplaySetup displaySetup = null, ElementClassSet classes = null, ButtonSetup buttonSetup = null, bool enableSubmitButton = false,
			IReadOnlyCollection<EtherealComponent> etherealContent = null ) {
			DisplaySetup = displaySetup;
			Classes = classes;
			ButtonItemGetter = setupGetter => buttonSetup == null
				                                  ? Enumerable.Empty<FormItem>().Materialize()
				                                  : buttonSetup.GetActionComponent(
						                                  null,
						                                  ( text, icon ) => new StandardButtonStyle( text, icon: icon ),
						                                  enableSubmitButton: enableSubmitButton )
					                                  .ToFormItem( setup: setupGetter( buttonSetup.DisplaySetup ) )
					                                  .ToCollection();
			EtherealContent = etherealContent;
		}
	}
}