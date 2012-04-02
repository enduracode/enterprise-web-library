using System;
using System.Linq;
using System.ServiceProcess;
using System.Timers;
using RedStapler.StandardLibrary.Email;

namespace RedStapler.StandardLibrary.WindowsServiceFramework {
	/// <summary>
	/// A .NET Framework service that uses a Red Stapler service for its implementation.
	/// </summary>
	public sealed class ServiceBaseAdapter: ServiceBase {
		private readonly Timer timer = new Timer();
		private DateTime lastHealthCheckDateAndTime;
		private readonly WindowsServiceBase service;

		/// <summary>
		/// Creates a ServiceBase adapter. Generated code use only.
		/// </summary>
		public ServiceBaseAdapter( WindowsServiceBase service ) {
			ServiceName = WindowsServiceMethods.GetServiceInstalledName( service );
			AutoLog = false;

			timer.Interval = 10000;
			timer.Elapsed += tick;

			this.service = service;
		}

		/// <summary>
		/// Private use only.
		/// </summary>
		protected override void OnStart( string[] args ) {
			Action method = delegate {
				lastHealthCheckDateAndTime = DateTime.Now;
				service.Init();
				timer.Start();
			};
			if( !AppTools.ExecuteBlockWithStandardExceptionHandling( method ) )
				Stop();
		}

		/// <summary>
		/// Private use only.
		/// </summary>
		protected override void OnStop() {
			AppTools.ExecuteBlockWithStandardExceptionHandling( delegate {
				timer.Stop();
				service.CleanUp();
			} );
		}

		private void tick( object sender, ElapsedEventArgs e ) {
			AppTools.ExecuteBlockWithStandardExceptionHandling( delegate {
				var now = DateTime.Now;
				if( AppTools.IsLiveInstallation && new[] { lastHealthCheckDateAndTime, now }.Any( dt => dt.Date.IsBetweenDateTimes( lastHealthCheckDateAndTime, now ) ) ) {
					var message = new EmailMessage();
					message.ToAddresses.AddRange( AppTools.DeveloperEmailAddresses );
					message.Subject = "Health check from " + WindowsServiceMethods.GetServiceInstalledName( service );
					AppTools.SendEmailWithDefaultFromAddress( message );
				}
				lastHealthCheckDateAndTime = now;

				service.Tick();
			} );
		}
	}
}