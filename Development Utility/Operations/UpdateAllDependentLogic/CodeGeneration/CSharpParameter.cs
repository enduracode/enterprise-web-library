using EnterpriseWebLibrary;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration {
	internal class CSharpParameter {
		private readonly string type;
		private readonly string name;
		private readonly string defaultValue;

		/// <summary>
		/// If defaultValue happens to be a string in the compiled code, it must be passed with surrounding double quotes.
		/// </summary>
		public CSharpParameter( string type, string name, string defaultValue = "" ) {
			this.type = type;
			this.name = name;
			this.defaultValue = defaultValue;
		}

		public string Name { get { return name; } }
		public string MethodSignatureDeclaration { get { return StringTools.ConcatenateWithDelimiter( " = ", type.ConcatenateWithSpace( name ), defaultValue ); } }
	}
}