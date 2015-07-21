using NUnit.Framework;
using EnterpriseWebLibrary;

namespace EnterpriseWebLibrary.Tests.StringTools {
	[ TestFixture ]
	internal class RemoveCommonNonAlphaNumericCharacters {
		[ Test ]
		public void Test() {
			Assert.AreEqual( "abcdefghijklmnopqrstuvxyz", "abcdefghijklmnopqrstuvxyz".RemoveNonAlphanumericCharacters() );
			Assert.AreEqual( "ABCDEFGHIJKLMNOPQRSTUVXYZ", "ABCDEFGHIJKLMNOPQRSTUVXYZ".RemoveNonAlphanumericCharacters() );
			Assert.AreEqual( "123415647890", "123415647890".RemoveNonAlphanumericCharacters() );
			Assert.AreEqual( "abc", "abcƒ±§╤ä".RemoveNonAlphanumericCharacters() );
			Assert.AreEqual( "abc", "abc!@#$%^&*()_+{}|:\"<>?".RemoveNonAlphanumericCharacters() );
			Assert.AreEqual( "", "".RemoveNonAlphanumericCharacters() );
			Assert.AreEqual( "   ", "   ".RemoveNonAlphanumericCharacters( preserveWhiteSpace: true ) );
			Assert.AreEqual(
				"  abcdefghijklmnopqrstuvxyz 123415647890  ",
				"  abcdefghijklmnopqrstuvxyz 123415647890  ".RemoveNonAlphanumericCharacters( preserveWhiteSpace: true ) );
			Assert.AreEqual(
				"  abcdefghijklmnopqrstuvxyz 123415647890  \r\r\n",
				"  abcdefghijklmnopqrstuvxyz 123415647890  \r\r\n".RemoveNonAlphanumericCharacters( preserveWhiteSpace: true ) );
		}
	}
}