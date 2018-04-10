using System;
using System.Linq;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration {
	internal class ModificationField {
		private readonly string name;
		private readonly string pascalCasedName;
		private readonly string camelCasedName;
		private readonly Type type;
		private readonly string typeName;
		private readonly string nullableTypeName;
		private readonly string enumerableElementTypeName;
		private readonly int? size;
		private readonly string privateFieldNameOverride;

		internal ModificationField(
			string name, string pascalCasedName, string camelCasedName, Type type, string typeName, string nullableTypeName, string enumerableElementTypeName, int? size,
			string privateFieldNameOverride = "" ) {
			this.name = name;
			this.pascalCasedName = pascalCasedName;
			this.camelCasedName = camelCasedName;
			this.type = type;
			this.typeName = typeName;
			this.nullableTypeName = nullableTypeName;
			this.enumerableElementTypeName = enumerableElementTypeName;
			this.size = size;
			this.privateFieldNameOverride = privateFieldNameOverride;
		}

		internal string Name => name;
		internal string PascalCasedName => pascalCasedName;
		internal string CamelCasedName => camelCasedName;

		internal bool TypeIs( Type type ) {
			return this.type == type;
		}

		internal string TypeName => typeName;
		internal string NullableTypeName => nullableTypeName;
		internal string EnumerableElementTypeName => enumerableElementTypeName;
		internal int? Size => size;

		internal string PrivateFieldName => privateFieldNameOverride.Any() ? privateFieldNameOverride : camelCasedName;
	}
}