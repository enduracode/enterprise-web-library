using System.Net;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using Microsoft.AspNetCore.Http;

namespace Tests.WebFramework;

[ TestFixture ]
class ForwardedClientIpTests {
	[ Test ]
	public void NoHeader() {
		Assert.That( getForwardedRequest( "" ).GetForwardedClientIp( 1 ), Is.Null );
	}

	[ Test ]
	public void SingleAddress() {
		Assert.That( getForwardedRequest( "203.0.113.195" ).GetForwardedClientIp( 1 ), Is.EqualTo( IPAddress.Parse( "203.0.113.195" ) ) );
	}

	[ Test ]
	public void SingleAddressV6() {
		Assert.That(
			getForwardedRequest( "2001:db8:85a3:8d3:1319:8a2e:370:7348" ).GetForwardedClientIp( 1 ),
			Is.EqualTo( IPAddress.Parse( "2001:db8:85a3:8d3:1319:8a2e:370:7348" ) ) );
	}

	[ Test ]
	public void SingleAddressBogus() {
		Assert.That( getForwardedRequest( "bogus" ).GetForwardedClientIp( 1 ), Is.Null );
	}

	[ Test ]
	public void TwoAddresses() {
		Assert.That(
			getForwardedRequest( "203.0.113.195, 2001:db8:85a3:8d3:1319:8a2e:370:7348" ).GetForwardedClientIp( 1 ),
			Is.EqualTo( IPAddress.Parse( "2001:db8:85a3:8d3:1319:8a2e:370:7348" ) ) );
	}

	[ Test ]
	public void ThreeAddresses() {
		Assert.That(
			getForwardedRequest( "203.0.113.195,2001:db8:85a3:8d3:1319:8a2e:370:7348,198.51.100.178" ).GetForwardedClientIp( 1 ),
			Is.EqualTo( IPAddress.Parse( "198.51.100.178" ) ) );
	}

	private HttpRequest getForwardedRequest( string xForwardedFor ) {
		var request = new DefaultHttpContext().Request;
		if( xForwardedFor.Length > 0 )
			request.Headers[ "X-Forwarded-For" ] = xForwardedFor;
		return request;
	}
}