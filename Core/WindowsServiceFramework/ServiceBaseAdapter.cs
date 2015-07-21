using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Email;

namespace EnterpriseWebLibrary.WindowsServiceFramework {
	/// <summary>
	/// A .NET Framework service that uses an EWL service for its implementation.
	/// </summary>
	public sealed class ServiceBaseAdapter: ServiceBase {
		private const int tickInterval = 10000;

		private DateTime lastHealthCheckDateAndTime;
		private readonly WindowsServiceBase service;
		private Timer timer;

		/// <summary>
		/// Creates a ServiceBase adapter. Generated code use only.
		/// </summary>
		public ServiceBaseAdapter( WindowsServiceBase service ) {
			ServiceName = WindowsServiceMethods.GetServiceInstalledName( service );
			AutoLog = false;

			this.service = service;
		}

		/// <summary>
		/// Private use only.
		/// </summary>
		protected override void OnStart( string[] args ) {
			if( GlobalInitializationOps.SecondaryInitFailed ) {
				ExitCode = 0x425; // Win32 error code; see http://msdn.microsoft.com/en-us/library/cc231199.aspx.
				Stop();
				return;
			}

			Action method = () => {
				lastHealthCheckDateAndTime = DateTime.Now;
				service.Init();

				timer = new Timer( tick, null, tickInterval, Timeout.Infinite );
			};
			if( !TelemetryStatics.ExecuteBlockWithStandardExceptionHandling( method ) ) {
				ExitCode = 0x428; // Win32 error code; see http://msdn.microsoft.com/en-us/library/cc231199.aspx.
				Stop();
			}
		}

		/// <summary>
		/// Private use only.
		/// </summary>
		protected override void OnStop() {
			TelemetryStatics.ExecuteBlockWithStandardExceptionHandling(
				() => {
					if( timer != null ) {
						var waitHandle = new ManualResetEvent( false );
						timer.Dispose( waitHandle );
						waitHandle.WaitOne();
					}

					service.CleanUp();
				} );
		}

		private void tick( object state ) {
			TelemetryStatics.ExecuteBlockWithStandardExceptionHandling(
				() => {
					// We need to schedule the next tick even if there is an exception thrown in this one. Use try-finally instead of CallEveryMethod so we don't lose
					// exception stack traces.
					try {
						var now = DateTime.Now;
						if( ConfigurationStatics.IsLiveInstallation && !ConfigurationStatics.MachineIsStandbyServer &&
						    new[] { lastHealthCheckDateAndTime, now }.Any( dt => dt.Date.IsBetweenDateTimes( lastHealthCheckDateAndTime, now ) ) )
							EmailStatics.SendHealthCheckEmail( WindowsServiceMethods.GetServiceInstalledName( service ) );
						lastHealthCheckDateAndTime = now;

						service.Tick();
					}
					finally {
						try {
							timer.Change( tickInterval, Timeout.Infinite );
						}
						catch( ObjectDisposedException ) {
							// This should not be necessary with the Timer.Dispose overload we are using, but see http://stackoverflow.com/q/12354883/35349.
						}
					}
				} );
		}
	}
}