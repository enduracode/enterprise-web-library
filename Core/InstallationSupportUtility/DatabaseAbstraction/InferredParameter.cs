namespace EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;

/// <summary>
/// Metadata for a database statement parameter whose type was inferred.
/// </summary>
public sealed record InferredParameter( string Name, Type DataType, string DbTypeString, int Size, short? NumericScale );