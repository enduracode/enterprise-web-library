using Microsoft.AspNetCore.Http;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

internal sealed class EwfHttpContextAccessor: IHttpContextAccessor {
	private static readonly HttpContextAccessor aspNetAccessor = new();
	private static readonly string frameworkContextKey = "{0}HttpContext".FormatWith( EwlStatics.EwlInitialism.ToLowerInvariant() );

	internal bool UseFrameworkContext {
		set {
			var aspNetContext = aspNetAccessor.HttpContext;
			if( value )
				aspNetContext.Items.Add( frameworkContextKey, new EwfHttpContext( aspNetContext ) );
			else
				aspNetContext.Items.Remove( frameworkContextKey );
		}
	}

	HttpContext IHttpContextAccessor.HttpContext {
		get {
			var aspNetContext = aspNetAccessor.HttpContext;
			return aspNetContext is not null && aspNetContext.Items.TryGetValue( frameworkContextKey, out var frameworkContext )
				       ? (EwfHttpContext)frameworkContext
				       : aspNetContext;
		}
		set {
			var aspNetContext = aspNetAccessor.HttpContext;
			if( aspNetContext is not null && aspNetContext.Items.ContainsKey( frameworkContextKey ) )
				throw new Exception( "The framework context is in use." );
			aspNetAccessor.HttpContext = value;
		}
	}
}