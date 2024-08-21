using EnterpriseWebLibrary.Configuration;

// EwlResource

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Admin;

partial class ErrorLog {
	protected override EwfSafeRequestHandler getOrHead() =>
		new EwfSafeResponseWriter(
			EwfResponse.Create(
				ContentTypes.PlainText,
				new EwfResponseBodyCreator(
					writer => {
						if( !File.Exists( ConfigurationStatics.InstallationConfiguration.ErrorLogFilePath ) )
							return;

						using var reader = new StreamReader(
							ConfigurationStatics.InstallationConfiguration.ErrorLogFilePath,
							new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read, Share = FileShare.ReadWrite } );
						var buffer = new char[ 4096 ];
						int count;
						while( ( count = reader.Read( buffer, 0, buffer.Length ) ) > 0 )
							writer.Write( buffer, 0, count );
					} ) ) );
}