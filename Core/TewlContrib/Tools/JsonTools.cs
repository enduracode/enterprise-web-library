using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EnterpriseWebLibrary.TewlContrib;

public static class JsonTools {
	public static string ToJsonStringWithSimpleEscaping( this JsonNode node, bool writeIndented = false ) =>
		node.ToJsonString( options: new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = writeIndented } );
}