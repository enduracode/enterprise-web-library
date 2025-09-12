namespace EnterpriseWebLibrary.EnterpriseWebFramework;

internal class ResourceNotAvailableException( string? message, Exception? innerException ): Exception( message, innerException );