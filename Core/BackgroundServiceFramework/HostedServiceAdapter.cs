using System.Threading;
using System.Threading.Tasks;
using EnterpriseWebLibrary.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using NodaTime;

namespace EnterpriseWebLibrary.BackgroundServiceFramework;

/// <summary>
/// A .NET hosted service that uses an EWL background service for its implementation.
/// </summary>
internal sealed class HostedServiceAdapter: BackgroundService {
	private const int tickInterval = 10000;

	private Instant lastTickInstant;
	private readonly BackgroundServiceBase service;

	public HostedServiceAdapter( BackgroundServiceBase service ) {
		this.service = service;
	}

	public override Task StartAsync( CancellationToken cancellationToken ) {
		if( GlobalInitializationOps.SecondaryInitFailed ) {
			abortStartup( 1, 0x425 ); // Win32 error code; see http://msdn.microsoft.com/en-us/library/cc231199.aspx.
			return Task.CompletedTask;
		}

		void init() {
			lastTickInstant = Clock.GetCurrentTime();
			service.Init();
		}
		if( !TelemetryStatics.ExecuteBlockWithStandardExceptionHandling( init ) ) {
			abortStartup( 1, 0x428 ); // Win32 error code; see http://msdn.microsoft.com/en-us/library/cc231199.aspx.
			return Task.CompletedTask;
		}

		return base.StartAsync( cancellationToken );
	}

	public override async Task StopAsync( CancellationToken cancellationToken ) {
		await base.StopAsync( CancellationToken.None );

		TelemetryStatics.ExecuteBlockWithStandardExceptionHandling( service.CleanUp );
	}

	protected override async Task ExecuteAsync( CancellationToken stoppingToken ) {
		do {
			try {
				await Task.Delay( tickInterval, stoppingToken );
			}
			catch( OperationCanceledException ) when( stoppingToken.IsCancellationRequested ) {
				return;
			}
			if( stoppingToken.IsCancellationRequested ) // cancellation can occur after delay but before execution continues
				return;

			TelemetryStatics.ExecuteBlockWithStandardExceptionHandling( () => {
				BackgroundServiceStatics.TickTime = Clock.GetCurrentTime();

				// If the clock has run ahead by more than tickInterval, and then happens be synced, we cannot create an Interval.
				if( BackgroundServiceStatics.TickTime < lastTickInstant )
					return;

				var interval = new TickInterval( new Interval( lastTickInstant, BackgroundServiceStatics.TickTime ) );
				lastTickInstant = BackgroundServiceStatics.TickTime;

				if( interval.EndsWithinNormalUseHours( DateTimeZoneProviders.Tzdb.GetSystemDefault() ) || !ConfigurationStatics.IsIntermediateInstallation )
					service.Tick( interval );
			} );
		}
		while( !stoppingToken.IsCancellationRequested );
	}

	private void abortStartup( byte exitCode, int windowsExitCode ) {
		if( BackgroundServiceStatics.Services.GetRequiredService<IHostLifetime>() is WindowsServiceLifetime windowsLifetime )
			windowsLifetime.ExitCode = windowsExitCode;
		else
			Environment.ExitCode = exitCode;

		var applicationLifetime = BackgroundServiceStatics.Services.GetRequiredService<IHostApplicationLifetime>();
		applicationLifetime.ApplicationStarted.Register( applicationLifetime.StopApplication );
	}
}