using System;
using System.ServiceProcess;

namespace RedStapler.StandardLibrary {
	public static class ServiceControllerTools {
		public static void WaitForStatusWithTimeOut( this ServiceController s, ServiceControllerStatus desiredStatus ) {
			s.WaitForStatus( desiredStatus, TimeSpan.FromMinutes( 5 ) );
		}
	}
}