using System;
using System.ServiceProcess;

namespace EnterpriseWebLibrary {
	public static class ServiceControllerTools {
		public static void WaitForStatusWithTimeOut( this ServiceController s, ServiceControllerStatus desiredStatus ) {
			s.WaitForStatus( desiredStatus, TimeSpan.FromMinutes( 5 ) );
		}
	}
}