using System.Security.Cryptography;
using System.Text;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ElementBase.Classification;
using EnterpriseWebLibrary.UserManagement;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;

[ PublicAPI ]
public static class UserDisplayStatics {
	private static readonly ElementClass avatarClass = new( "ewfUa" );

	[ UsedImplicitly ]
	private class CssElementCreator: ControlCssElementCreator {
		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
			new CssElement( "UserAvatar", $"span.{avatarClass.ClassName}" ).ToCollection();
	}

	/// <summary>
	/// Returns the user’s avatar, labeled with their friendly name (if available) or email address.
	/// </summary>
	/// <param name="user"></param>
	/// <param name="size">The width and height of the avatar, in CSS pixels.</param>
	/// <param name="fallbackInitials">The initials to use for an avatar if an image is not available for the user. If you don’t have initials, pass a name and
	/// the initials will be extracted. If you pass the empty string, the user’s friendly name will be used if available.</param>
	/// <param name="labelOverride">Pass a nonempty collection to override the label, and an empty collection for no label.</param>
	public static PhrasingComponent GetAvatar(
		this SystemUser user, decimal size, string fallbackInitials = "", IReadOnlyCollection<PhrasingComponent>? labelOverride = null ) {
		var components = new List<PhrasingComponent>();

		var emailHash = Convert.ToHexString( SHA256.HashData( Encoding.UTF8.GetBytes( user.Email ) ) ).ToLowerInvariant();

		string fallbackName;
		if( fallbackInitials.Any( char.IsLower ) ) {
			fallbackName = fallbackInitials;
			fallbackInitials = "";
		}
		else
			fallbackName = user.FriendlyName;
		var initialsParameter = fallbackInitials.Length > 0 ? $"&initials={fallbackInitials}" : fallbackName.Length > 0 ? $"&name={fallbackName}" : "";

		components.Add( new EwfImage( new ImageSetup( "avatar" ), new FixedSizeImageSourceSet( 2048, size, getAvatarUrl ) ) );

		var label = labelOverride ?? ( user.FriendlyName.Length > 0 ? user.FriendlyName.ToComponents() : user.Email.ToComponents() );
		if( label.Any() )
			components.Add( new GenericPhrasingContainer( label ) );

		return new GenericPhrasingContainer( components, classes: avatarClass );

		ExternalResource getAvatarUrl( uint width ) =>
			// see https://docs.gravatar.com/sdk/images/
			new( $"https://gravatar.com/avatar/{emailHash}?s={width}&d={( initialsParameter.Length > 0 ? $"initials{initialsParameter}" : "identicon" )}" );
	}
}