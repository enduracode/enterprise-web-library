#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Data for an element.
	/// </summary>
	public class ElementData {
		internal readonly Func<ElementContext, ElementNodeData> NodeDataGetter;

		/// <summary>
		/// Creates an element-data object.
		/// </summary>
		/// <param name="localDataGetter"></param>
		/// <param name="classes"></param>
		/// <param name="clientSideIdReferences">The <see cref="ElementId"/> and <see cref="ElementIdSet"/> objects to which you’d like to add this element’s
		/// client-side ID.</param>
		/// <param name="clientSideIdOverride">Pass a nonempty string to override the client-side ID of the element, which is useful if you need a static value that
		/// you can reference from CSS or JavaScript files. The ID you specify should be unique on the page. Do not pass null. Use with caution.</param>
		/// <param name="children"></param>
		/// <param name="etherealChildren"></param>
		public ElementData(
			Func<ElementLocalData> localDataGetter, ElementClassSet classes = null, IEnumerable<ElementIdReference> clientSideIdReferences = null,
			string clientSideIdOverride = "", IReadOnlyCollection<FlowComponentOrNode> children = null,
			IReadOnlyCollection<EtherealComponentOrElement> etherealChildren = null ) {
			classes = classes ?? ElementClassSet.Empty;
			NodeDataGetter = context => {
				var clientSideId = clientSideIdOverride.Any() ? clientSideIdOverride : context.Id;
				classes.AddElementId( clientSideId );
				foreach( var i in clientSideIdReferences ?? Enumerable.Empty<ElementIdReference>() )
					i.AddId( clientSideId );

				return new ElementNodeData(
					clientSideIdOverride,
					() => localDataGetter().NodeDataGetter( classes ),
					children: children,
					etherealChildren: etherealChildren );
			};
		}
	}
}