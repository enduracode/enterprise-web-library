using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EnterpriseWebLibrary.MailMerging.DataTree;
using EnterpriseWebLibrary.MailMerging.FieldImplementation;
using EnterpriseWebLibrary.MailMerging.Fields;
using EnterpriseWebLibrary.MailMerging.PseudoTableFields;
using EnterpriseWebLibrary.MailMerging.PsuedoChildFields;
using EnterpriseWebLibrary.MailMerging.RowTree;
using Tewl.Tools;

namespace EnterpriseWebLibrary.MailMerging {
	/// <summary>
	/// A mock MergeStatics class as we have it in many systems.
	/// </summary>
	public static class MergeStatics {
		private static readonly List<MergeField<PseudoTableRow>> tableFields = new List<MergeField<PseudoTableRow>>();
		private static readonly List<MergeField<PseudoChildRow>> childFields = new List<MergeField<PseudoChildRow>>();

		/// <summary>
		/// Must be called before other methods in this class.
		/// </summary>
		public static void Init() {
			var nativeTableFields = getNativeTableFields().ToList().AsReadOnly();
			var nativeChildFields = getNativeChildFields().ToList().AsReadOnly();

			initPseudoFields( nativeTableFields );
			initPseudoChildFields( nativeChildFields );
		}

		private static void initPseudoFields( ReadOnlyCollection<MergeField<PseudoTableRow>> nativeTableFields ) {
			foreach( var field in nativeTableFields )
				tableFields.Add( field );
		}

		private static void initPseudoChildFields( ReadOnlyCollection<MergeField<PseudoChildRow>> nativeChildFields ) {
			childFields.AddRange( nativeChildFields );
		}

		private static MergeField<PseudoTableRow>[] getNativeTableFields() {
			return new BasicMergeFieldImplementation<PseudoTableRow, string>[] { new FullName(), new Test() }.Select( MergeFieldOps.CreateBasicField ).ToArray();
		}

		private static MergeField<PseudoChildRow>[] getNativeChildFields() {
			return new BasicMergeFieldImplementation<PseudoChildRow, string>[] { new TheValue(), new Another() }.Select( MergeFieldOps.CreateBasicField ).ToArray();
		}

		public static MergeRowTree CreateEmptyPseudoTableRowTree() {
			return MergeDataTreeOps.CreateRowTree(
				tableFields.AsReadOnly(),
				new PseudoTableRow[] { null },
				children: new List<MergeDataTreeChild<PseudoTableRow>>
					{
						new MergeDataTreeChild<PseudoTableRow, PseudoChildRow>(
							"Things",
							childFields.AsReadOnly(),
							pseudoTableRow => ( (PseudoChildRow)null ).ToCollection() )
					}.AsReadOnly() );
		}

		public static MergeRowTree CreatePseudoTableRowTree( IEnumerable<PseudoTableRow> rows ) {
			var rand = new Random();
			return MergeDataTreeOps.CreateRowTree(
				tableFields.AsReadOnly(),
				rows,
				new List<MergeDataTreeChild<PseudoTableRow>>
					{
						new MergeDataTreeChild<PseudoTableRow, PseudoChildRow>(
							"Things",
							childFields.AsReadOnly(),
							pseudoTableRow => new[] { new PseudoChildRow( rand.Next( 20 ) ), new PseudoChildRow( rand.Next( 20 ) ) } )
					}.AsReadOnly() );
		}
	}
}