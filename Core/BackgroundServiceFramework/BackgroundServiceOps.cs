using System.ComponentModel;
using System.Threading;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;

namespace EnterpriseWebLibrary.BackgroundServiceFramework;

/// <summary>
/// Generated code use only.
/// </summary>
[ PublicAPI ]
[ EditorBrowsable( EditorBrowsableState.Never ) ]
public static class BackgroundServiceOps {
	/// <summary>
	/// Generated code use only.
	/// </summary>
	[ EditorBrowsable( EditorBrowsableState.Never ) ]
	public static void RunApplication( SystemInitializer globalInitializer, BackgroundServiceBase service ) {
		var dataAccessState = new Lazy<DataAccessState>( () => new DataAccessState() );
		GlobalInitializationOps.InitStatics(
			globalInitializer,
			service.Name,
			false,
			mainDataAccessStateGetter: () => dataAccessState.Value,
			useLongDatabaseTimeouts: true );
		try {
			TelemetryStatics.ExecuteBlockWithStandardExceptionHandling( () => {
				try {
					Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

					var builder = Host.CreateApplicationBuilder(
						new HostApplicationBuilderSettings
							{
								EnvironmentName = ConfigurationStatics.IsDevelopmentInstallation ? Environments.Development : Environments.Production,
								ContentRootPath = EwlStatics.CombinePaths( ConfigurationStatics.InstallationConfiguration.InstallationPath, service.Name ),
								DisableDefaults = true
							} );

					builder.Services.Configure<HostOptions>( options => options.ShutdownTimeout = Timeout.InfiniteTimeSpan );

					builder.Services.AddWindowsService( options => options.ServiceName = ConfigurationStatics.InstallationConfiguration.BackgroundServices
						                                               .Single( i => i.Name.Equals( service.Name, StringComparison.Ordinal ) )
						                                               .InstalledName );

					builder.Services.AddSerilog();

					using var hostedService = new HostedServiceAdapter( service );
					builder.Services.AddSingleton<IHostedService>( hostedService );

					var host = builder.Build();

					if( host.Services.GetRequiredService<IHostLifetime>() is WindowsServiceLifetime windowsLifetime )
						windowsLifetime.AutoLog = false;

					BackgroundServiceStatics.Init( host.Services );

					host.Run();
					if( hostedService.ExecuteTask?.Exception is {} exception )
						throw exception;
				}
				finally {
					Log.CloseAndFlush();
				}
			} );
		}
		finally {
			GlobalInitializationOps.CleanUpStatics();
		}
	}
}