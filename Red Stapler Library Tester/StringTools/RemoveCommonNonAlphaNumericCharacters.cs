using NUnit.Framework;
using RedStapler.StandardLibrary;

namespace RedStapler.StandardLibraryTester.StringTools {
	[ TestFixture ]
	internal class RemoveCommonNonAlphaNumericCharacters {
		[ Test ]
		public void Test() {
			Assert.AreEqual( "abcdefghijklmnopqrstuvxyz", "abcdefghijklmnopqrstuvxyz".RemoveCommonNonAlphaNumericCharacters() );
			Assert.AreEqual( "ABCDEFGHIJKLMNOPQRSTUVXYZ", "ABCDEFGHIJKLMNOPQRSTUVXYZ".RemoveCommonNonAlphaNumericCharacters() );
			Assert.AreEqual( "123415647890", "123415647890".RemoveCommonNonAlphaNumericCharacters() );
			Assert.AreEqual( "abc", "abcƒ±§╤ä".RemoveCommonNonAlphaNumericCharacters() );
			Assert.AreEqual( "abc", "abc!@#$%^&*()_+{}|:\"<>?".RemoveCommonNonAlphaNumericCharacters() );
			Assert.AreEqual( "", "".RemoveCommonNonAlphaNumericCharacters() );
			Assert.AreEqual( "   ", "   ".RemoveCommonNonAlphaNumericCharacters() );
			Assert.AreEqual( null, ( (string)null ).RemoveCommonNonAlphaNumericCharacters() );
			Assert.AreEqual( "  abcdefghijklmnopqrstuvxyz 123415647890  ", "  abcdefghijklmnopqrstuvxyz 123415647890  ".RemoveCommonNonAlphaNumericCharacters() );
		}
	}
}