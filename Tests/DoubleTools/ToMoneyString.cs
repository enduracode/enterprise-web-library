using NUnit.Framework;
using EnterpriseWebLibrary;

namespace EnterpriseWebLibrary.Tests.DoubleTools {
	[ TestFixture ]
	public class ToMoneyString {
		[ Test ]
		public void Test() {
			Assert.AreEqual( "$1.23", 1.23.ToMoneyString() );
			Assert.AreEqual( "$2.50", 2.5.ToMoneyString() );
			Assert.AreEqual( "$3.00", 3.0.ToMoneyString() );
			Assert.AreEqual( "$4.57", 4.567.ToMoneyString() );
		}
	}
}