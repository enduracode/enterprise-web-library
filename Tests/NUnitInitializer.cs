using NUnit.Framework;
using RedStapler.StandardLibrary;

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