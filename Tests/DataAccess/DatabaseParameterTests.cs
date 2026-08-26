using EnterpriseWebLibrary.DataAccess.CommandWriting;
using EnterpriseWebLibrary.DataAccess.RetrievalCaching;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;

namespace Tests.DataAccess;

[ TestFixture ]
class DatabaseParameterTests {
	[ Test ]
	public void NullValueBecomesDatabaseNull() {
		var databaseInfo = new SqlServerInfo( "", null, null, null, "test", false, null );
		var parameter = new DbCommandParameter( "value", new DbParameterValue( null ) ).GetAdoDotNetParameter( databaseInfo );

		Assert.That( parameter.Value, Is.SameAs( DBNull.Value ) );
	}

	[ Test ]
	public void QueryCacheSupportsNullParameterValues() {
		var cache = new QueryRetrievalQueryCache<int>();
		var resultSetCreations = 0;

		IEnumerable<int> createResultSet() => [ ++resultSetCreations ];

		var firstResult = cache.GetResultSet( [ null, "value" ], createResultSet );
		var secondResult = cache.GetResultSet( [ null, "value" ], createResultSet );
		var differentResult = cache.GetResultSet( [ "value", null ], createResultSet );

		Assert.Multiple( () => {
			Assert.That( secondResult, Is.SameAs( firstResult ) );
			Assert.That( differentResult, Is.Not.SameAs( firstResult ) );
			Assert.That( resultSetCreations, Is.EqualTo( 2 ) );
		} );
	}
}