using System.ComponentModel;
using System.Threading.Tasks;
using EnterpriseWebLibrary.Configuration;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary;

/// <summary>
/// Generated code use only.
/// </summary>
[ PublicAPI ]
[ EditorBrowsable( EditorBrowsableState.Never ) ]
public static class CustomSyncOps {
	/// <summary>
	/// Generated code use only.
	/// </summary>
	[ EditorBrowsable( EditorBrowsableState.Never ) ]
	public static async Task<int> RunAsync( Func<Task> method ) {
		var initializationLog = "";
		ConfigurationStatics.Init( AppContext.BaseDirectory, "Custom Sync Runner", false, ref initializationLog );
		try {
			await method();
		}
		catch( Exception e ) {
			await Console.Error.WriteLineAsync( e.ToString() );
			return 1;
		}

		return 0;
	}
}