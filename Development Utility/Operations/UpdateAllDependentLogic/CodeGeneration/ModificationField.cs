using System;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration {
	internal class ModificationField {
		private readonly Type type;
		private readonly string typeName;
		private readonly string nullableTypeName;
		private readonly string enumerableElementTypeName;
		private readonly string propertyName;
		private readonly string pascalCasedName;
		private readonly int? size;

		internal ModificationField( Type type, string typeName, string nullableTypeName, string enumerableElementTypeName, string propertyName, string pascalCasedName,
		                            int? size ) {
			this.type = type;
			this.typeName = typeName;
			this.nullableTypeName = nullableTypeName;
			this.enumerableElementTypeName = enumerableElementTypeName;
			this.propertyName = propertyName;
			this.pascalCasedName = pascalCasedName;
			this.size = size;
		}

		internal bool TypeIs( Type type ) {
			return this.type == type;
		}

		internal string TypeName { get { return typeName; } }
		internal string NullableTypeName { get { return nullableTypeName; } }
		internal string EnumerableElementTypeName { get { return enumerableElementTypeName; } }
		internal string PropertyName { get { return propertyName; } }
		internal string PascalCasedName { get { return pascalCasedName; } }
		internal int? Size { get { return size; } }
	}
}