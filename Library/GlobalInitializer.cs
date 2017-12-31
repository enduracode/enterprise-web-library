namespace EnterpriseWebLibrary {
	public class GlobalInitializer: SystemInitializer {
		void SystemInitializer.InitStatics() {
			GlobalStatics.Init();
		}

		void SystemInitializer.CleanUpStatics() {}
	}
}