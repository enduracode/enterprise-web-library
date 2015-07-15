namespace EnterpriseWebLibrary {
	/// <summary>
	/// System-specific initialization logic.
	/// </summary>
	public interface SystemInitializer {
		/// <summary>
		/// Performs system-specific initialization.
		/// </summary>
		void InitStatics();

		/// <summary>
		/// Performs cleanup activities so the application can be shut down.
		/// </summary>
		void CleanUpStatics();
	}
}