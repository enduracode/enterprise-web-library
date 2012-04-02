using System;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Class containing the required pair of strings used to define a web method.
	/// NOTE: It's possible that we could implement this as a  provider and have a method that will grab the path to the Service.svc file only once instead of having to pass it every time.
	/// This would work very nicely with RLE and CUA. Unfortunately, this currently wouldn't work for Charette because it still has asmx services and this also might not work for the future
	/// if we wanted multiple service paths.
	/// </summary>
	public class WebMethodDefinition {
		/// <summary>
		/// Path to the Service.svc file.
		/// </summary>
		public string WebServicePath { get; private set; }

		/// <summary>
		/// Method name to invoke on the service. 
		/// </summary>
		public string WebMethodName { get; private set; }

		/// <summary>
		/// Used to define the two not-optional properties required to invoke a web method.
		/// </summary>
		/// <param name="webServicePath">Path to the Service.svc file.</param>
		/// <param name="webMethodName">Method name to invoke on the service. </param>
		public WebMethodDefinition( string webServicePath, string webMethodName ) {
			if( webServicePath.IsNullOrWhiteSpace() )
				throw new ApplicationException( "WebServicePath must be set." );
			if( webMethodName.IsNullOrWhiteSpace() )
				throw new ApplicationException( "WebMethodName must be set." );
			WebServicePath = webServicePath;
			WebMethodName = webMethodName;
		}
	}
}