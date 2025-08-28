using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core;
using Newtonsoft.Json;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public sealed class TrustedParentUrl: IEquatable<TrustedParentUrl> {
	/// <summary>
	/// Generated code use only.
	/// </summary>
	[ EditorBrowsable( EditorBrowsableState.Never ) ]
	public static readonly TrustedParentUrl Invalid = new( TrustedUrl.Invalid );

	public static string Serialize( TrustedParentUrl trustedParentUrl, string serializationAppId ) =>
		TrustedUrl.Serialize( trustedParentUrl.trustedUrl, serializationAppId );

	public static TrustedParentUrl Deserialize( string serializedTrustedParentUrl, string serializationAppId ) =>
		new( TrustedUrl.Deserialize( serializedTrustedParentUrl, serializationAppId ) );

	private readonly TrustedUrl trustedUrl;

	internal TrustedParentUrl( TrustedUrl trustedUrl ) {
		this.trustedUrl = trustedUrl;
	}

	public ResourceParent GetParentOrThrow() => TryGetParent( out var parent ) ? parent : throw new InvalidOperationException( "invalid URL" );

	public bool TryGetParent( [ NotNullWhen( true ) ] out ResourceParent? parent ) {
		parent = trustedUrl.WebItem as ResourceParent;
		return parent is not null;
	}

	[ JsonProperty ]
	private EwfUrl? url => trustedUrl.GetUrl();

	public override bool Equals( object? obj ) => Equals( obj as TrustedParentUrl );
	public bool Equals( TrustedParentUrl? other ) => other is not null && EwlStatics.AreEqual( trustedUrl, other.trustedUrl );
	public override int GetHashCode() => trustedUrl.GetHashCode();
}

public static class TrustedParentUrlExtensionCreators {
	/// <summary>
	/// Creates a trusted URL for this parent.
	/// </summary>
	public static TrustedParentUrl ToTrustedParentUrl( this ResourceParent parent ) => new( new TrustedUrl( parent, null ) );
}