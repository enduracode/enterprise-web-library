using System.Collections.Generic;
using System.Linq;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for entity-level UI such as navigational components and tabs.
	/// </summary>
	public sealed class EntityUiSetup {
		/// <summary>
		/// Creates an entity-UI setup object.
		/// </summary>
		/// <param name="navActions">The navigational action components</param>
		/// <param name="navFormControls">The navigational form controls</param>
		/// <param name="actions">The action components</param>
		/// <param name="entitySummaryContent">Pass a value to include an entity summary in the UI.</param>
		/// <param name="tabMode">The tab mode</param>
		public EntityUiSetup(
			IReadOnlyCollection<ActionComponentSetup> navActions = null, IReadOnlyCollection<NavFormControl> navFormControls = null,
			IReadOnlyCollection<ActionComponentSetup> actions = null, IReadOnlyCollection<FlowComponent> entitySummaryContent = null,
			TabMode tabMode = TabMode.Automatic ) {
			NavActions = navActions ?? Enumerable.Empty<ActionComponentSetup>().Materialize();
			NavFormControls = navFormControls ?? Enumerable.Empty<NavFormControl>().Materialize();
			Actions = actions ?? Enumerable.Empty<ActionComponentSetup>().Materialize();
			EntitySummaryContent = entitySummaryContent;
			this.tabMode = tabMode;
		}

		internal IReadOnlyCollection<ActionComponentSetup> NavActions { get; }

		internal IReadOnlyCollection<NavFormControl> NavFormControls { get; }

		internal IReadOnlyCollection<ActionComponentSetup> Actions { get; }

		internal IReadOnlyCollection<FlowComponent> EntitySummaryContent { get; }

		private TabMode tabMode { get; }

		/// <summary>
		/// Returns the tab mode, or null for no tabs.
		/// </summary>
		internal TabMode? GetTabMode( EntitySetupBase es ) {
			if( !es.Resources.Any() )
				return null;
			if( tabMode == TabMode.Automatic )
				return es.Resources.Count == 1 && es.Resources.Single().Resources.Count() < 8 ? TabMode.Horizontal : TabMode.Vertical;
			return tabMode;
		}
	}
}