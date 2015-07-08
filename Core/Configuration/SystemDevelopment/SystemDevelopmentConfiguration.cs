using System.Collections.Generic;

namespace RedStapler.StandardLibrary.Configuration.SystemDevelopment {
	partial class SystemDevelopmentConfiguration {
		public IEnumerable<ServerSideConsoleProject> ServerSideConsoleProjectsNonNullable {
			get { return serverSideConsoleProjects ?? new ServerSideConsoleProject[ 0 ]; }
		}
	}
}