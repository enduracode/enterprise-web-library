using System.Web.Script.Serialization;

namespace EnterpriseWebLibrary.IO {
	public static class JsonOps {
		/// <summary>
		/// Serializes the given object into json.
		/// </summary>
		public static string SerializeObject( object o ) => new JavaScriptSerializer().Serialize( o );

		/// <summary>
		/// Converts the given json into the given type.
		/// </summary>
		public static T DeserializeObject<T>( string json ) => new JavaScriptSerializer().Deserialize<T>( json );
	}
}