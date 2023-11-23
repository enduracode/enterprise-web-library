namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess;

internal static class RetrievalStatics {
	public static string GetColumnTupleTypeName( IReadOnlyCollection<Column> columns ) =>
		columns.Count < 2
			? columns.Single().DataTypeName
			: "( {0} )".FormatWith( StringTools.ConcatenateWithDelimiter( ", ", columns.Select( i => i.DataTypeName ) ) );
}