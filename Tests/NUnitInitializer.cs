using NUnit.Framework;
using EnterpriseWebLibrary;

[ SetUpFixture ]
public class NUnitInitializer {
	[ SetUp ]
	public void InitStatics() {
		UnitTestingInitializationOps.InitStatics( new GlobalInitializer() );
	}

	[ TearDown ]
	public void CleanUpStatics() {
		UnitTestingInitializationOps.CleanUpStatics();
	}
}