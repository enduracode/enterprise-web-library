using System;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A level (h2, h3, or h4) for a heading control.
	/// </summary>
	[ Obsolete( "Guaranteed through 30 April 2015. Please use the Section control instead." ) ]
	public enum HeadingLevel {
		/// <summary>
		/// A second-level heading, which is the highest level that should be used within the content of a page since first-level headings are reserved for page
		/// titles.
		/// </summary>
		[ Obsolete( "Guaranteed through 30 April 2015. Please use the Section control instead." ) ]
		H2,

		/// <summary>
		/// A third-level heading.
		/// </summary>
		[ Obsolete( "Guaranteed through 30 April 2015. Please use the Section control instead." ) ]
		H3,

		/// <summary>
		/// A fourth-level heading.
		/// </summary>
		[ Obsolete( "Guaranteed through 30 April 2015. Please use the Section control instead." ) ]
		H4
	}

	internal static class HeadingLevelStatics {
		internal static string[] HeadingElements { get { return new[] { "h2", "h3", "h4" }; } }
	}
}