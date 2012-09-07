using System.Collections.Generic;

namespace RedStapler.StandardLibrary.MailMerging.RowTree {
	/// <summary>
	/// A tree of merge rows.
	/// </summary>
	public class MergeRowTree {
		private readonly string nodeName;
		private readonly IEnumerable<MergeRow> rows;
		private readonly string xmlRowElementName;

		internal MergeRowTree( string nodeName, IEnumerable<MergeRow> rows, string xmlRowElementName ) {
			this.nodeName = nodeName;
			this.rows = rows;
			this.xmlRowElementName = xmlRowElementName;
		}

		/// <summary>
		/// Gets the name of this node.
		/// </summary>
		public string NodeName { get { return nodeName; } }

		/// <summary>
		/// Gets the merge rows.
		/// </summary>
		public IEnumerable<MergeRow> Rows { get { return rows; } }

		/// <summary>
		/// Gets the XML row element name.
		/// </summary>
		public string XmlRowElementName { get { return xmlRowElementName; } }
	}
}