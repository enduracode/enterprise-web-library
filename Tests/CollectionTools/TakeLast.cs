using System;
using System.Linq;
using NUnit.Framework;
using RedStapler.StandardLibrary;

namespace EnterpriseWebLibrary.Tests.CollectionTools {
	[ TestFixture ]
	public class TakeLast {
		[ Test ]
		public void Test() {
			var a = new int[] { };
			Assert.IsEmpty( a.TakeLast( 1 ) );
			Assert.IsEmpty( a.TakeLast( 0 ) );
			Assert.Throws<ApplicationException>( () => a.TakeLast( -1 ) );

			var b = new[] { 1 };
			Assert.True( new[] { 1 }.SequenceEqual( b.TakeLast( 1 ) ) );
			Assert.True( new[] { 1 }.SequenceEqual( b.TakeLast( 2 ) ) );
			Assert.IsEmpty( b.TakeLast( 0 ) );

			var c = new[] { 1, 2, 3, 4 };
			Assert.True( new[] { 4 }.SequenceEqual( c.TakeLast( 1 ) ) );
			Assert.True( new[] { 3, 4 }.SequenceEqual( c.TakeLast( 2 ) ) );
			Assert.True( new[] { 1, 2, 3, 4 }.SequenceEqual( c.TakeLast( 4 ) ) );
			Assert.True( new[] { 1, 2, 3, 4 }.SequenceEqual( c.TakeLast( 5 ) ) );
			Assert.IsEmpty( b.TakeLast( 0 ) );
		}
	}
}