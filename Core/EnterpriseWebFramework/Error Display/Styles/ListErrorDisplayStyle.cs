using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A style that displays errors as a list.
	/// </summary>
	public class ListErrorDisplayStyle: ErrorDisplayStyle<FlowComponent> {
		private static readonly ElementClass containerClass = new ElementClass( "ewfEml" );

		/// <summary>
		/// EWL use only.
		/// </summary>
		public static readonly IReadOnlyCollection<string> CssSelectors = "div.{0}".FormatWith( containerClass.ClassName ).ToCollection();

		[ UsedImplicitly ]
		private class CssElementCreator: ControlCssElementCreator {
			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
				return new CssElement( "ErrorMessageListContainer", CssSelectors.ToArray() ).ToCollection();
			}
		}

		private readonly Func<IEnumerable<string>, IReadOnlyCollection<FlowComponent>> componentGetter;

		/// <summary>
		/// Creates a list error-display style.
		/// </summary>
		/// <param name="classes">The classes on the list container.</param>
		public ListErrorDisplayStyle( ElementClassSet classes = null ) {
			componentGetter = errors => new GenericFlowContainer(
				new StackList(
					from i in errors
					select new FontAwesomeIcon( "fa-times-circle", "fa-lg" ).ToCollection<PhrasingComponent>()
						.Concat( " {0}".FormatWith( i ).ToComponents() )
						.ToComponentListItem() ).ToCollection(),
				classes: containerClass.Add( classes ?? ElementClassSet.Empty ) ).ToCollection();
		}

		IReadOnlyCollection<FlowComponent> ErrorDisplayStyle<FlowComponent>.GetComponents( IEnumerable<string> errors ) => componentGetter( errors );
	}
}