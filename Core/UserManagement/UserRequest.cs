using NodaTime;

namespace EnterpriseWebLibrary.UserManagement;

/// <summary>
/// A request a user made to the system.
/// </summary>
/// <param name="UserId">The ID of the user.</param>
/// <param name="RequestTime">The time of the request.</param>
public record UserRequest( int UserId, Instant RequestTime );