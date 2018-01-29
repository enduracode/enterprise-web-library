using EnterpriseWebLibrary.MailMerging;

namespace EnterpriseWebLibrary {
	public class GlobalInitializer: SystemInitializer {
		void SystemInitializer.InitStatics() {
			GlobalStatics.Init();
			MergeStatics.Init();
		}

		void SystemInitializer.CleanUpStatics() {}
	}
}