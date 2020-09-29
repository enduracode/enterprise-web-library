using System;
using System.Collections.Generic;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Data for a displayable element.
	/// </summary>
	public class DisplayableElementData {
		internal readonly Func<ElementContext, ElementData> BaseDataGetter;

		/// <summary>
		/// Creates a displayable-element-data object.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="localDataGetter"></param>
		/// <param name="classes"></param>
		/// <param name="clientSideIdOverride">Pass a nonempty string to override the client-side ID of the element, which is useful if you need a static value that
		/// you can reference from CSS or JavaScript files. The ID you specify should be unique on the page. Do not pass null. Use with caution.</param>
		/// <param name="children"></param>
		/// <param name="etherealChildren"></param>
		public DisplayableElementData(
			DisplaySetup displaySetup, Func<DisplayableElementLocalData> localDataGetter, ElementClassSet classes = null, string clientSideIdOverride = "",
			IReadOnlyCollection<FlowComponentOrNode> children = null, IReadOnlyCollection<EtherealComponentOrElement> etherealChildren = null ) {
			displaySetup = displaySetup ?? new DisplaySetup( true );
			BaseDataGetter = context => {
				displaySetup.AddJsShowStatements( "$( '#{0}' ).show( 200 );".FormatWith( context.Id ) );
				displaySetup.AddJsHideStatements( "$( '#{0}' ).hide( 200 );".FormatWith( context.Id ) );
				return new ElementData(
					() => localDataGetter().BaseDataGetter( displaySetup ),
					classes: classes,
					clientSideIdOverride: clientSideIdOverride,
					children: children,
					etherealChildren: etherealChildren );
			};
		}
	}
}