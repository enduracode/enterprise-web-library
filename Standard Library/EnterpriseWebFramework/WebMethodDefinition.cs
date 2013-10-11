using System;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Defines a web service.
	/// </summary>
	public class WebMethodDefinition {
		/// <summary>
		/// Path to the service.
		/// </summary>
		public readonly string WebService;

		/// <summary>
		/// Configures a web service.
		/// </summary>
		/// <param name="webService">URL to the web service.</param>
		public WebMethodDefinition( PageInfo webService ) {
			var url = webService.GetUrl();
			if( url.IsNullOrWhiteSpace() )
				throw new ApplicationException( "webService must be set." );
			WebService = url;
		}
	}
}