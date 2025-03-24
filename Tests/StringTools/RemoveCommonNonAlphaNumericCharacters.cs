using NUnit.Framework;
using Tewl.Tools;

namespace EnterpriseWebLibrary.Tests.StringTools;

[ TestFixture ]
internal class RemoveCommonNonAlphaNumericCharacters {
	[ Test ]
	public void Test() {
		Assert.That( "abcdefghijklmnopqrstuvxyz".RemoveNonAlphanumericCharacters(), Is.EqualTo( "abcdefghijklmnopqrstuvxyz" ) );
		Assert.That( "ABCDEFGHIJKLMNOPQRSTUVXYZ".RemoveNonAlphanumericCharacters(), Is.EqualTo( "ABCDEFGHIJKLMNOPQRSTUVXYZ" ) );
		Assert.That( "123415647890".RemoveNonAlphanumericCharacters(), Is.EqualTo( "123415647890" ) );
		Assert.That( "abcƒ±§╤ä".RemoveNonAlphanumericCharacters(), Is.EqualTo( "abc" ) );
		Assert.That( "abc!@#$%^&*()_+{}|:\"<>?".RemoveNonAlphanumericCharacters(), Is.EqualTo( "abc" ) );
		Assert.That( "".RemoveNonAlphanumericCharacters(), Is.Empty );
		Assert.That( "   ".RemoveNonAlphanumericCharacters( preserveWhiteSpace: true ), Is.EqualTo( "   " ) );
		Assert.That(
			"  abcdefghijklmnopqrstuvxyz 123415647890  ".RemoveNonAlphanumericCharacters( preserveWhiteSpace: true ),
			Is.EqualTo( "  abcdefghijklmnopqrstuvxyz 123415647890  " ) );
		Assert.That(
			"  abcdefghijklmnopqrstuvxyz 123415647890  \r\r\n".RemoveNonAlphanumericCharacters( preserveWhiteSpace: true ),
			Is.EqualTo( "  abcdefghijklmnopqrstuvxyz 123415647890  \r\r\n" ) );
	}
}