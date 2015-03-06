using NUnit.Framework;

[ SetUpFixture ]
public class NUnitInitializer {
	[ SetUp ]
	public void InitStatics() {}

	[ TearDown ]
	public void CleanUpStatics() {}
}