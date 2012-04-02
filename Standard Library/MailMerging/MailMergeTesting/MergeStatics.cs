using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RedStapler.StandardLibrary.MailMerging.DataTree;
using RedStapler.StandardLibrary.MailMerging.FieldImplementation;
using RedStapler.StandardLibrary.MailMerging.Fields;
using RedStapler.StandardLibrary.MailMerging.MailMergeTesting.PseudoTableFields;
using RedStapler.StandardLibrary.MailMerging.RowTree;

namespace RedStapler.StandardLibrary.MailMerging.MailMergeTesting {
	/// <summary>
	/// A mock MergeStatics class as we have it in many systems.
	/// </summary>
	internal static class MergeStatics {
		private static readonly List<MergeField<PseudoTableRow>> tableFields = new List<MergeField<PseudoTableRow>>();

		/// <summary>
		/// Must be called before other methods in this class.
		/// </summary>
		internal static void Init() {
			var nativeTableFields = getNativeTableFields().ToList().AsReadOnly();

			initPseudoFields( nativeTableFields );
		}

		private static void initPseudoFields( ReadOnlyCollection<MergeField<PseudoTableRow>> nativeTableFields ) {
			foreach( var field in nativeTableFields )
				tableFields.Add( field );
		}

		private static MergeField<PseudoTableRow>[] getNativeTableFields() {
			return new BasicMergeFieldImplementation<PseudoTableRow, string>[] { new FullName(), new Test() }.Select( MergeFieldOps.CreateBasicField ).ToArray();
		}

		public static IEnumerable<MergeRow> CreatePseudoTableRowTree( IEnumerable<PseudoTableRow> rows ) {
			return MergeDataTreeOps.CreateRowTree( tableFields.AsReadOnly(), rows, null );
		}

		public static IEnumerable<MergeRow> CreateEmptyPseudoTableRowTree() {
			return CreatePseudoTableRowTree( new PseudoTableRow[] { } );
		}
	}
}