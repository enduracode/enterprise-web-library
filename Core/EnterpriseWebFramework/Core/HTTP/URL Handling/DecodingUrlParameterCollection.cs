using System.ComponentModel;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public sealed class DecodingUrlParameterCollection {
	private readonly ILookup<string, string> parameters;
	private readonly HashSet<string> accessedParameters;

	/// <summary>
	/// Generated code use only.
	/// </summary>
	[ EditorBrowsable( EditorBrowsableState.Never ) ]
	public string AppId { get; }

	internal DecodingUrlParameterCollection(
		IEnumerable<( string name, string value )> segmentParameters, IEnumerable<( string name, string value )> queryParameters, string appId ) {
		parameters = segmentParameters.Concat( queryParameters ).ToLookup( i => i.name, i => i.value, StringComparer.OrdinalIgnoreCase );
		accessedParameters = new HashSet<string>( parameters.Count );
		AppId = appId;
	}

	/// <summary>
	/// Returns the value of the parameter with the specified name, or null if the parameter is not present.
	/// </summary>
	/// <param name="name">Do not pass null or the empty string.</param>
	public string? Get( string name ) {
		accessedParameters.Add( name );
		return get( name );
	}

	/// <summary>
	/// Generated code use only.
	/// </summary>
	[ EditorBrowsable( EditorBrowsableState.Never ) ]
	public string? GetRemainingParameter( string name ) {
		if( accessedParameters.Contains( name ) )
			throw new Exception( $"The {name} parameter was already accessed by the parser." );
		return get( name );
	}

	private string? get( string name ) {
		var matches = parameters[ name ].Materialize();
		return matches.Count > 1 ? throw new UnresolvableUrlException( $"Multiple {name} parameters exist.", null ) : matches.SingleOrDefault();
	}

	internal void ResetState() {
		accessedParameters.Clear();
	}
}