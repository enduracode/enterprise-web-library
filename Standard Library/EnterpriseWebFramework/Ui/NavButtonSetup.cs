using System;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements {
	[ Obsolete( "Guaranteed through 28 February 2013." ) ]
	public class NavButtonSetup {
		private readonly string text;
		private readonly PageInfo pageInfo;
		private readonly string url;

		[ Obsolete( "Guaranteed through 28 February 2013." ) ]
		public NavButtonSetup( string text, PageInfo pageInfo ) {
			this.text = text;
			this.pageInfo = pageInfo;
		}

		[ Obsolete( "Guaranteed through 28 February 2013." ) ]
		public NavButtonSetup( string text, string url ) {
			this.text = text;
			this.url = url;
		}

		[ Obsolete( "Guaranteed through 28 February 2013." ) ]
		public string Text { get { return text; } }

		[ Obsolete( "Guaranteed through 28 February 2013." ) ]
		public string Url { get { return url ?? pageInfo.GetUrl(); } }
	}
}