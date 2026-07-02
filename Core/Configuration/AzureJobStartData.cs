using System.Text.Json.Serialization;

namespace EnterpriseWebLibrary.Configuration;

internal sealed record AzureJobStartData(
	[ property: JsonPropertyName( "env" ) ]
	IReadOnlyDictionary<string, string> EnvironmentVariables,
	[ property: JsonPropertyName( "args" ) ]
	IReadOnlyList<string> Arguments,
	[ property: JsonPropertyName( "input" ) ]
	string Input );