namespace RedStapler.StandardLibrary.WindowsServiceFramework {
	/// <summary>
	/// A Windows service.
	/// </summary>
	public interface WindowsServiceBase {
		/// <summary>
		/// Gets the name of the service.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the description of the service.
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Initializes the service.
		/// </summary>
		void Init();

		/// <summary>
		/// Performs cleanup activities so the service can be shut down.
		/// </summary>
		void CleanUp();

		/// <summary>
		/// Performs tasks that have emerged since the last call to this method.
		/// </summary>
		void Tick();
	}
}