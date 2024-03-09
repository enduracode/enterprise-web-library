using Microsoft.AspNetCore.Http;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// An object that handles a safe HTTP request (e.g. GET, HEAD).
/// </summary>
public sealed class EwfSafeRequestHandler {
	private static Action<Action>? dataModificationMethodExecutor;

	internal static void Init( Action<Action> dataModificationMethodExecutor ) {
		EwfSafeRequestHandler.dataModificationMethodExecutor = dataModificationMethodExecutor;
	}

	public static implicit operator EwfSafeRequestHandler( EwfSafeResponseWriter responseWriter ) => new( responseWriter );

	private readonly EwfSafeResponseWriter responseWriter;

	/// <summary>
	/// Creates a request handler from the result of the specified data-modification method. If you do not need to modify data, do not call this constructor;
	/// use <see cref="EwfSafeResponseWriter"/>, which can be implicitly converted to a request handler.
	/// </summary>
	public EwfSafeRequestHandler( Func<EwfSafeResponseWriter> dataModificationMethod ) {
		EwfSafeResponseWriter? writer = null;
		dataModificationMethodExecutor!( () => writer = dataModificationMethod() );
		responseWriter = writer!;
	}

	private EwfSafeRequestHandler( EwfSafeResponseWriter responseWriter ) {
		this.responseWriter = responseWriter;
	}

	internal void WriteResponse( HttpContext context, bool forceImmediateResponseExpiration ) {
		responseWriter.WriteResponse( context, forceImmediateResponseExpiration );
	}
}