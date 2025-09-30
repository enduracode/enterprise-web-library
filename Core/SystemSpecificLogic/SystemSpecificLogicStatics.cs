using System.Security.Cryptography;
using System.Threading;
using EnterpriseWebLibrary.Configuration;
using Newtonsoft.Json;
using NodaTime;

namespace EnterpriseWebLibrary.SystemSpecificLogic;

public static class SystemSpecificLogicStatics {
	/// <summary>
	/// Development Utility and private use only.
	/// </summary>
	public const string ProvidersFolderAndNamespaceName = "Providers";

	private static Type globalInitializerType { get; set; } = null!;
	internal static SystemGeneralProvider GeneralProvider { get; private set; } = null!;
	private static IReadOnlyDictionary<LocalDate, byte[]> bestEffortDataHasherKeys = new Dictionary<LocalDate, byte[]>();

	internal static void Init( Type globalInitializerType ) {
		SystemSpecificLogicStatics.globalInitializerType = globalInitializerType;
		GeneralProvider = GetLibraryProvider<SystemGeneralProvider>( "General" ).GetProvider()!;
	}


	/// <summary>
	/// Gets the display name of the system.
	/// </summary>
	public static string SystemDisplayName =>
		GeneralProvider.SystemDisplayName.Length > 0 ? GeneralProvider.SystemDisplayName : ConfigurationStatics.InstallationConfiguration.SystemName;

	// The duration here should probably come from the provider.
	private static LocalDate bestEffortCutoffDate => Clock.TransactionTime.Minus( Duration.FromDays( 14 ) ).InUtc().Date;

	internal static bool BestEffortDateIsValid( LocalDate date ) => date.IsBetween( bestEffortCutoffDate, Clock.TransactionTime.InUtc().Date );

	/// <summary>
	/// Assumes <see cref="BestEffortDateIsValid"/> returns true and <see cref="Clock.TransactionTime"/> remains constant across both calls.
	/// </summary>
	internal static HMAC GetBestEffortDataHasher( LocalDate date ) {
		var hasher = GeneralProvider.GetBestEffortDataHasher( date );
		if( hasher is not null )
			return hasher;

		if( !bestEffortDataHasherKeys.TryGetValue( date, out var key ) )
			SynchronizationTools.ExecuteWithMachineExclusiveAccess(
				$"{EwlStatics.EwlInitialism.EnglishToPascal()}{ConfigurationStatics.InstallationConfiguration.FullShortName}HmacKeys",
				null,
				_ => {
					var filePath = EwlStatics.CombinePaths(
						ConfigurationStatics.EwlFolderPath,
						"Secrets",
						ConfigurationStatics.InstallationConfiguration.FullName,
						"HMAC Keys.json" );

					if( File.Exists( filePath ) ) {
						reloadKeys();
						if( bestEffortDataHasherKeys.TryGetValue( date, out key ) )
							return;
					}
					else
						Directory.CreateDirectory( Path.GetDirectoryName( filePath )! );

					var newKeys = new SortedList<LocalDate, byte[]>();
					for( var d = bestEffortCutoffDate; d <= Clock.TransactionTime.InUtc().Date; d = d.PlusDays( 1 ) )
						newKeys.Add( d, bestEffortDataHasherKeys.TryGetValue( d, out var k ) ? k : RandomNumberGenerator.GetBytes( 64 ) );
					File.WriteAllText( filePath, JsonConvert.SerializeObject( newKeys, Formatting.Indented ) );

					reloadKeys();
					key = bestEffortDataHasherKeys[ date ];
					return;

					void reloadKeys() =>
						Interlocked.Exchange( ref bestEffortDataHasherKeys, JsonConvert.DeserializeObject<Dictionary<LocalDate, byte[]>>( File.ReadAllText( filePath ) )! );
				} );

		return new HMACSHA256( key! );
	}

	internal static SystemProviderReference<ProviderType>
		GetLibraryProvider<ProviderType>( string providerName, SpecifiedValue<ProviderType?>? specifiedProvider = null ) where ProviderType: class =>
		specifiedProvider is null
			? new SystemProviderGetter(
				globalInitializerType.Assembly,
				$"{globalInitializerType.Namespace}.{ProvidersFolderAndNamespaceName}",
				getProviderNotFoundErrorMessage ).GetProvider<ProviderType>( providerName )
			: new SystemProviderReference<ProviderType>( specifiedProvider.Value, getProviderNotFoundErrorMessage( providerName ) );

	private static string getProviderNotFoundErrorMessage( string providerName ) =>
		"""{0} provider not found in system. To implement, create a class named {0} in Library\{1} and implement the System{0}Provider interface.""".FormatWith(
			providerName,
			ProvidersFolderAndNamespaceName );
}