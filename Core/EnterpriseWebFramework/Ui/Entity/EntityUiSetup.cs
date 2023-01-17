namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for entity-level UI such as navigational components and tabs.
	/// </summary>
	public sealed class EntityUiSetup {
		/// <summary>
		/// Creates an entity-UI setup object.
		/// </summary>
		/// <param name="navActionGetter">A function that takes a post-back ID base and returns the navigational action components. Do not return null.</param>
		/// <param name="navFormControls">The navigational form controls</param>
		/// <param name="actionGetter">A function that takes a post-back ID base and returns the action components. Do not return null.</param>
		/// <param name="entitySummaryContent">Pass a value to include an entity summary in the UI.</param>
		/// <param name="tabMode">The tab mode</param>
		public EntityUiSetup(
			Func<string, IReadOnlyCollection<ActionComponentSetup>> navActionGetter = null, IReadOnlyCollection<NavFormControl> navFormControls = null,
			Func<string, IReadOnlyCollection<ActionComponentSetup>> actionGetter = null, IReadOnlyCollection<FlowComponent> entitySummaryContent = null,
			TabMode tabMode = TabMode.Automatic ) {
			NavActionGetter = navActionGetter ?? ( _ => Enumerable.Empty<ActionComponentSetup>().Materialize() );
			NavFormControls = navFormControls ?? Enumerable.Empty<NavFormControl>().Materialize();
			ActionGetter = actionGetter ?? ( _ => Enumerable.Empty<ActionComponentSetup>().Materialize() );
			EntitySummaryContent = entitySummaryContent;
			this.tabMode = tabMode;
		}

		internal Func<string, IReadOnlyCollection<ActionComponentSetup>> NavActionGetter { get; }

		internal IReadOnlyCollection<NavFormControl> NavFormControls { get; }

		internal Func<string, IReadOnlyCollection<ActionComponentSetup>> ActionGetter { get; }

		internal IReadOnlyCollection<FlowComponent> EntitySummaryContent { get; }

		private TabMode tabMode { get; }

		/// <summary>
		/// Returns the tab mode, or null for no tabs.
		/// </summary>
		internal TabMode? GetTabMode( EntitySetupBase es ) {
			if( !es.ListedResources.Any() )
				return null;
			if( tabMode == TabMode.Automatic )
				return es.ListedResources.Count == 1 && es.ListedResources.Single().Resources.Count() < 8 ? TabMode.Horizontal : TabMode.Vertical;
			return tabMode;
		}
	}
}