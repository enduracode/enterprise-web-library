namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Application-specific configuration for the EWF UI.
	/// </summary>
	public abstract class AppEwfUiProvider {
		/// <summary>
		/// Creates and returns a list of custom style sheets that should be used on EWF UI pages.
		/// </summary>
		public virtual List<ResourceInfo> GetStyleSheets() => new List<ResourceInfo>();

		/// <summary>
		/// Gets the logo to be shown at the top of the EWF user interface. Returns null if the application display name should be used instead.
		/// </summary>
		public virtual IReadOnlyCollection<FlowComponent> GetLogoComponent() => null;

		/// <summary>
		/// Returns the components that identify the authenticated user and let them log out, change their password, etc. Returns null for the framework’s built-in
		/// components.
		/// </summary>
		public virtual IReadOnlyCollection<FlowComponent> GetUserInfoComponents() => null;

		/// <summary>
		/// Gets the global navigational action components.
		/// </summary>
		public virtual IReadOnlyCollection<ActionComponentSetup> GetGlobalNavActions() => Enumerable.Empty<ActionComponentSetup>().Materialize();

		/// <summary>
		/// Gets the global navigational form controls.
		/// </summary>
		public virtual IReadOnlyCollection<NavFormControl> GetGlobalNavFormControls() => Enumerable.Empty<NavFormControl>().Materialize();

		/// <summary>
		/// Gets whether items in the page action control list are separated with a pipe character.
		/// </summary>
		public virtual bool PageActionItemsSeparatedWithPipe() => true;

		/// <summary>
		/// Gets the components to be shown at the bottom of the log-in page for systems with forms authentication.
		/// </summary>
		public virtual IReadOnlyCollection<FlowComponent> GetSpecialInstructionsForLogInPage() => Enumerable.Empty<FlowComponent>().Materialize();

		/// <summary>
		/// Gets the global foot components.
		/// </summary>
		public virtual IReadOnlyCollection<FlowComponent> GetGlobalFootComponents() => Enumerable.Empty<FlowComponent>().Materialize();

		/// <summary>
		/// Gets whether the "Powered by the Enterprise Web Library" footer is disabled.
		/// </summary>
		public virtual bool PoweredByEwlFooterDisabled() => false;
	}
}