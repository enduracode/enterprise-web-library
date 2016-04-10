using EnterpriseWebLibrary;
using NUnit.Framework;

[ SetUpFixture ]
public class NUnitInitializer {
	[ OneTimeSetUp ]
	public void InitStatics() {
		UnitTestingInitializationOps.InitStatics( new GlobalInitializer() );
	}

	[ OneTimeTearDown ]
	public void CleanUpStatics() {
		UnitTestingInitializationOps.CleanUpStatics();
	}
}