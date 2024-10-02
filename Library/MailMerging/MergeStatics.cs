using EnterpriseWebLibrary.MailMerging.DataTree;
using EnterpriseWebLibrary.MailMerging.FieldImplementation;
using EnterpriseWebLibrary.MailMerging.Fields;
using EnterpriseWebLibrary.MailMerging.PseudoTableFields;
using EnterpriseWebLibrary.MailMerging.PsuedoChildFields;
using EnterpriseWebLibrary.MailMerging.RowTree;

namespace EnterpriseWebLibrary.MailMerging;

/// <summary>
/// A mock MergeStatics class as we have it in many systems.
/// </summary>
public static class MergeStatics {
	private static readonly List<MergeField<PseudoTableRow>> tableFields = new();
	private static readonly List<MergeField<PseudoChildRow>> childFields = new();

	/// <summary>
	/// Must be called before other methods in this class.
	/// </summary>
	public static void Init() {
		var nativeTableFields = getNativeTableFields().ToList();
		var nativeChildFields = getNativeChildFields().ToList();

		initPseudoFields( nativeTableFields );
		initPseudoChildFields( nativeChildFields );
	}

	private static void initPseudoFields( IReadOnlyCollection<MergeField<PseudoTableRow>> nativeTableFields ) {
		foreach( var field in nativeTableFields )
			tableFields.Add( field );
	}

	private static void initPseudoChildFields( IReadOnlyCollection<MergeField<PseudoChildRow>> nativeChildFields ) {
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
			tableFields,
			new PseudoTableRow?[] { null },
			children: new List<MergeDataTreeChild<PseudoTableRow>>
				{
					new MergeDataTreeChild<PseudoTableRow, PseudoChildRow>( "Things", childFields, _ => [ null ] )
				} );
	}

	public static MergeRowTree CreatePseudoTableRowTree( IEnumerable<PseudoTableRow> rows ) {
		var rand = new Random();
		return MergeDataTreeOps.CreateRowTree(
			tableFields,
			rows,
			new List<MergeDataTreeChild<PseudoTableRow>>
				{
					new MergeDataTreeChild<PseudoTableRow, PseudoChildRow>(
						"Things",
						childFields,
						pseudoTableRow => new[] { new PseudoChildRow( rand.Next( 20 ) ), new PseudoChildRow( rand.Next( 20 ) ) } )
				} );
	}
}