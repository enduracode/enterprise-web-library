using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

internal sealed class EwfHttpContext: HttpContext {
	private readonly HttpContext aspNetContext;

	internal EwfHttpContext( HttpContext aspNetContext ) {
		this.aspNetContext = aspNetContext;
		Response = new EwfResponse.AspNetAdapter( aspNetContext.Response.Cookies );
	}

	public override IFeatureCollection Features => throw new NotImplementedException();
	public override HttpRequest Request => aspNetContext.Request;
	public override HttpResponse Response { get; }
	public override ConnectionInfo Connection => throw new NotImplementedException();
	public override WebSocketManager WebSockets => throw new NotImplementedException();
	public override ClaimsPrincipal User { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public override IDictionary<object, object> Items { get => aspNetContext.Items; set => throw new NotImplementedException(); }
	public override IServiceProvider RequestServices { get => aspNetContext.RequestServices; set => throw new NotImplementedException(); }
	public override CancellationToken RequestAborted { get => aspNetContext.RequestAborted; set => throw new NotImplementedException(); }
	public override string TraceIdentifier { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public override ISession Session { get => aspNetContext.Session; set => throw new NotImplementedException(); }
	public override void Abort() => throw new NotImplementedException();
}