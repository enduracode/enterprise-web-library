using System.Collections.Generic;

namespace EnterpriseWebLibrary.MailMerging.DataTree {
	/// <summary>
	/// A remapping configuration for a merge data tree.
	/// </summary>
	public class MergeDataTreeRemapping {
		private readonly string nodeNameOverride;
		private readonly Dictionary<string, string> oldToNewFieldNames;
		private readonly Dictionary<string, MergeDataTreeRemapping> childRemappingsByChildName;
		private readonly string xmlRowElementName;

		/// <summary>
		/// Creates a merge data tree remapping.
		/// </summary>
		public MergeDataTreeRemapping( string nodeNameOverride = "", Dictionary<string, string> oldToNewFieldNames = null,
		                               Dictionary<string, MergeDataTreeRemapping> childRemappingsByChildName = null, string xmlRowElementName = "" ) {
			this.nodeNameOverride = nodeNameOverride;
			this.oldToNewFieldNames = oldToNewFieldNames ?? new Dictionary<string, string>();
			this.childRemappingsByChildName = childRemappingsByChildName ?? new Dictionary<string, MergeDataTreeRemapping>();
			this.xmlRowElementName = xmlRowElementName;
		}

		internal string NodeNameOverride { get { return nodeNameOverride; } }

		internal string GetFieldName( string fieldName ) {
			return oldToNewFieldNames.ContainsKey( fieldName ) ? oldToNewFieldNames[ fieldName ] : fieldName;
		}

		internal Dictionary<string, MergeDataTreeRemapping> ChildRemappingsByChildName { get { return childRemappingsByChildName; } }
		internal string XmlRowElementName { get { return xmlRowElementName; } }
	}
}