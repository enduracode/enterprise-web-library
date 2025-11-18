using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DataAccess.CommandWriting.Commands;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic.AlternativeResourceModes;
using EnterpriseWebLibrary.ExternalFunctionality;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Admin;

// EwlPage
partial class DebugLog {
	protected override ResourceParent? createParent() => new DiagnosticLog( Es );

	protected override string getResourceName() => "Two-Day Debug Log";

	protected override AlternativeResourceMode? createAlternativeMode() =>
		ExternalFunctionalityStatics.SqliteFunctionalityEnabled ? null : new DisabledResourceMode( "The SQLite provider must be available." );

	protected internal override bool IsSlow => true;

	protected override PageContent getContent() {
		var table = EwfTable.Create(
			headItems: EwfTableItem.Create( "Date/time".ToCell().Append( "Level".ToCell() ).Append( "Details".ToCell() ).Materialize() ).ToCollection() );

		var connection = new DatabaseConnection( new SqliteInfo( "Debug Log", EwfConfigurationStatics.AppConfiguration.DebugLogFilePath ) );
		connection.ExecuteWithConnectionOpen( () => {
			var command = new InlineSelect( [ "*" ], "FROM Events", false, orderByClause: "ORDER BY Id DESC" );
			command.Execute(
				connection,
				reader => {
					while( reader.Read() )
						table.AddItem(
							EwfTableItem.Create(
								( (string)reader.GetValue( 1 ) ).ToCell()
								.Append( ( (string)reader.GetValue( 2 ) ).ToCell() )
								.Append( ( (string)reader.GetValue( 4 ) ).ToCell() )
								.Materialize() ) );
				} );
		} );

		return new UiPageContent().Add( table );
	}
}