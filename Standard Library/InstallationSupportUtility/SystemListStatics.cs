using System;
using System.IO;
using System.ServiceModel;
using RedStapler.StandardLibrary.IO;
using RedStapler.StandardLibrary.InstallationSupportUtility.RsisInterface.Messages.SystemListMessage;

namespace RedStapler.StandardLibrary.InstallationSupportUtility {
	/// <summary>
	/// RSIS System List logic.
	/// </summary>
	public static class SystemListStatics {
		public static SystemList RsisSystemList { get; private set; }

		/// <summary>
		/// Gets a new system list from RSIS.
		/// </summary>
		public static void RefreshSystemList() {
			// When deserializing the system list below, do not perform schema validation since we don't want to be forced into redeploying Program Runner after every
			// schema change. We also don't have access to the schema on non-development machines.
			var cachedSystemListFilePath = StandardLibraryMethods.CombinePaths( AppTools.RedStaplerFolderPath, "RSIS System List.xml" );
			try {
				var serializedSystemList =
					ConfigurationLogic.ExecuteProgramRunnerUnstreamedServiceMethod( channel => channel.GetSystemList( ConfigurationLogic.AuthenticationKey ),
					                                                                "system list download" );
				RsisSystemList = XmlOps.DeserializeFromString<SystemList>( serializedSystemList, false );

				// Cache the system list so something is available in the future if the machine is offline.
				try {
					XmlOps.SerializeIntoFile( RsisSystemList, cachedSystemListFilePath );
				}
				catch( Exception e ) {
					const string generalMessage = "The RSIS system list cannot be cached on disk.";
					if( e is UnauthorizedAccessException )
						throw new UserCorrectableException( generalMessage + " If the program is running as a non built in administrator, you may need to disable UAC.", e );

					// An IOException probably means the file is locked. In this case we want to ignore the problem and move on.
					if( !( e is IOException ) )
						throw new UserCorrectableException( generalMessage, e );
				}
			}
			catch( UserCorrectableException e ) {
				if( e.InnerException == null || !( e.InnerException is EndpointNotFoundException ) )
					throw;

				// Use the cached version of the system list if it is available.
				if( File.Exists( cachedSystemListFilePath ) )
					RsisSystemList = XmlOps.DeserializeFromFile<SystemList>( cachedSystemListFilePath, false );
				else
					throw new UserCorrectableException( "RSIS cannot be reached to download the system list and a cached version is not available.", e );
			}
		}
	}
}