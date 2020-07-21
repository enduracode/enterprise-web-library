using System;
using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for an action component.
	/// </summary>
	public interface ActionComponentSetup {
		/// <summary>
		/// EWF use only.
		/// </summary>
		DisplaySetup DisplaySetup { get; }

		/// <summary>
		/// EWF use only.
		/// </summary>
		PhrasingComponent GetActionComponent(
			Func<string, ActionComponentIcon, HyperlinkStyle> hyperlinkStyleSelector, Func<string, ActionComponentIcon, ButtonStyle> buttonStyleSelector,
			bool enableSubmitButton = false );
	}

	public static class ActionComponentSetupExtensionCreators {
		/// <summary>
		/// Concatenates action-component setup objects.
		/// </summary>
		public static IEnumerable<SetupType> Concat<SetupType>( this SetupType first, IEnumerable<SetupType> second ) where SetupType: ActionComponentSetup =>
			second.Prepend( first );

		/// <summary>
		/// Returns a sequence of two action-component setup objects.
		/// </summary>
		public static IEnumerable<SetupType> Append<SetupType>( this SetupType first, SetupType second ) where SetupType: ActionComponentSetup =>
			Enumerable.Empty<SetupType>().Append( first ).Append( second );
	}
}