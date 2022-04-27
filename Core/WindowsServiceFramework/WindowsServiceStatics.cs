using NodaTime;

namespace EnterpriseWebLibrary.WindowsServiceFramework {
	public static class WindowsServiceStatics {
		/// <summary>
		/// Gets the time instant for the current call of <see cref="WindowsServiceBase.Tick(TickInterval)"/>.
		/// </summary>
		public static Instant TickTime { get; internal set; }
	}
}