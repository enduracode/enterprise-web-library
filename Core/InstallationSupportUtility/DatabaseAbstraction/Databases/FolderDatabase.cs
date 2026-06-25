using System.Data;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DatabaseSpecification;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.IO;
using Tewl.IO;

namespace EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction.Databases;

internal class FolderDatabase: Database {
	private readonly FolderDatabaseInfo info;

	public FolderDatabase( FolderDatabaseInfo info ) {
		this.info = info;
	}

	DatabaseInfo Database.Info => info;

	string Database.SecondaryDatabaseName => ( (DatabaseInfo)info ).SecondaryDatabaseName;

	void Database.ExecuteSqlScriptInTransaction( string script ) {
		throw new NotSupportedException();
	}

	int Database.GetLineMarker() {
		throw new NotSupportedException();
	}

	void Database.UpdateLineMarker( int value ) {
		throw new NotSupportedException();
	}

	void Database.ExportToFile( ExportFile file ) {
		if( file.IsAzureBlob ) {
			file.TryGetAzureBlob( out var containerUrl, out var blobName );
			var blobClient = new BlockBlobClient( new Uri( $"{containerUrl}/{blobName}" ), new ManagedIdentityCredential( ManagedIdentityId.SystemAssigned ) );
			using var stream = blobClient.OpenWrite( true );
			TarOps.ArchiveFolderToStream( info.FolderPath, stream );
		}
		else {
			file.TryGetFilePath( out var filePath );
			TarOps.ArchiveFolderToFile( info.FolderPath, filePath );
		}
	}

	void Database.DeleteAndReCreateFromFile(
		ExportFile file, IReadOnlyCollection<string> dataMigrationUsers, IReadOnlyCollection<string> dataModificationUsers ) {
		if( file.IsAzureBlob ) {
			if( file.TryGetAzureBlob( out var containerUrl, out var blobName ) ) {
				var blobClient = new BlobClient( new Uri( $"{containerUrl}/{blobName}" ), new ManagedIdentityCredential( ManagedIdentityId.SystemAssigned ) );
				using var stream = blobClient.OpenRead();
				TarOps.ExtractStreamToFolder( stream, info.FolderPath );
			}
			else {
				IoMethods.DeleteFolder( info.FolderPath );
				Directory.CreateDirectory( info.FolderPath );
			}
		}
		else {
			if( file.TryGetFilePath( out var filePath ) )
				TarOps.ExtractFileToFolder( filePath, info.FolderPath );
			else {
				IoMethods.DeleteFolder( info.FolderPath );
				Directory.CreateDirectory( info.FolderPath );
			}
		}
	}

	IEnumerable<DataRow> Database.GetDataTypes() => throw new NotSupportedException();
	IEnumerable<DatabaseTable> Database.GetTables() => [ ];
	IEnumerable<string> Database.GetProcedures() => throw new NotSupportedException();
	IEnumerable<DataRow> Database.GetProcedureParameters( string procedure ) => throw new NotSupportedException();
	void Database.PerformMaintenance() {}
	void Database.ShrinkAfterPostUpdateDataCommands( bool databaseInAzure ) {}

	void Database.ExecuteDbMethod( Action<DatabaseConnection> method ) {
		throw new NotSupportedException();
	}
}