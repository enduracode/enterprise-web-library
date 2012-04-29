namespace RedStapler.StandardLibrary.Configuration {
	partial class MachineConfiguration {
		// NOTE: The name of this method isn't very good. How can anyone know when to use it and when to use the underlying IsStandbyServer property?
		public static bool GetIsStandbyServer() {
			return AppTools.MachineConfiguration != null && AppTools.MachineConfiguration.IsStandbyServerSpecified && AppTools.MachineConfiguration.IsStandbyServer;
		}
	}
}