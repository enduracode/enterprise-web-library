using NUnit.Framework;

namespace Tests.DoubleTools;

[ TestFixture ]
public class ToMoneyString {
	[ Test ]
	public void Test() {
		Assert.That( 1.23.ToMoneyString(), Is.EqualTo( "$1.23" ) );
		Assert.That( 2.5.ToMoneyString(), Is.EqualTo( "$2.50" ) );
		Assert.That( 3.0.ToMoneyString(), Is.EqualTo( "$3.00" ) );
		Assert.That( 4.567.ToMoneyString(), Is.EqualTo( "$4.57" ) );
	}
}