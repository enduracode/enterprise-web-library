using System;
using System.ServiceProcess;

namespace RedStapler.StandardLibrary {
	public static class ServiceControllerTools {
		internal static void WaitForStatusWithTimeOut( this ServiceController s, ServiceControllerStatus desiredStatus ) {
			s.WaitForStatus( desiredStatus, TimeSpan.FromMinutes( 5 ) );
		}
	}
}