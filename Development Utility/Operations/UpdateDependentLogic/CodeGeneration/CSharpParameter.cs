using Tewl.Tools;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration {
	internal class CSharpParameter {
		private readonly string type;
		internal readonly string Name;
		private readonly string defaultValue;
		internal readonly string Description;

		/// <summary>
		/// If defaultValue happens to be a string in the compiled code, it must be passed with surrounding double quotes.
		/// </summary>
		public CSharpParameter( string type, string name, string defaultValue = "", string description = "" ) {
			this.type = type;
			Name = name;
			this.defaultValue = defaultValue;
			Description = description;
		}

		public string MethodSignatureDeclaration => StringTools.ConcatenateWithDelimiter( " = ", type.ConcatenateWithSpace( Name ), defaultValue );
	}
}