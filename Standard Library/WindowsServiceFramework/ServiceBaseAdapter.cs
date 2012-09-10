using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using RedStapler.StandardLibrary.Email;

namespace RedStapler.StandardLibrary.WindowsServiceFramework {
	/// <summary>
	/// A .NET Framework service that uses an EWL service for its implementation.
	/// </summary>
	public sealed class ServiceBaseAdapter: ServiceBase {
		private const int tickFrequency = 10000;

		private DateTime lastHealthCheckDateAndTime;
		private readonly WindowsServiceBase service;
		private readonly Timer timer;

		/// <summary>
		/// Creates a ServiceBase adapter. Generated code use only.
		/// </summary>
		public ServiceBaseAdapter( WindowsServiceBase service ) {
			ServiceName = WindowsServiceMethods.GetServiceInstalledName( service );
			AutoLog = false;

			this.service = service;
			timer = new Timer( tick, null, Timeout.Infinite, Timeout.Infinite );
		}

		/// <summary>
		/// Private use only.
		/// </summary>
		protected override void OnStart( string[] args ) {
			Action method = delegate {
				lastHealthCheckDateAndTime = DateTime.Now;
				service.Init();

				timer.Change( tickFrequency, Timeout.Infinite );
			};
			if( !AppTools.ExecuteBlockWithStandardExceptionHandling( method ) )
				Stop();
		}

		/// <summary>
		/// Private use only.
		/// </summary>
		protected override void OnStop() {
			AppTools.ExecuteBlockWithStandardExceptionHandling( delegate {
				var waitHandle = new ManualResetEvent( false );
				timer.Dispose( waitHandle );
				waitHandle.WaitOne();

				service.CleanUp();
			} );
		}

		private void tick( object state ) {
			AppTools.ExecuteBlockWithStandardExceptionHandling( delegate {
				// We need to schedule the next tick even if there is an exception thrown in this one. Use try-finally instead of CallEveryMethod so we don't lose
				// exception stack traces.
				try {
					var now = DateTime.Now;
					if( AppTools.IsLiveInstallation && new[] { lastHealthCheckDateAndTime, now }.Any( dt => dt.Date.IsBetweenDateTimes( lastHealthCheckDateAndTime, now ) ) ) {
						var message = new EmailMessage();
						message.ToAddresses.AddRange( AppTools.DeveloperEmailAddresses );
						message.Subject = "Health check from " + WindowsServiceMethods.GetServiceInstalledName( service );
						AppTools.SendEmailWithDefaultFromAddress( message );
					}
					lastHealthCheckDateAndTime = now;

					service.Tick();
				}
				finally {
					try {
						timer.Change( tickFrequency, Timeout.Infinite );
					}
					catch( ObjectDisposedException ) {
						// This should not be necessary with the Timer.Dispose overload we are using, but see http://stackoverflow.com/q/12354883/35349.
					}
				}
			} );
		}
	}
}