using System;
using System.Collections.Generic;
using System.Linq;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An object that creates labels for a form control.
	/// </summary>
	public class FormControlLabeler {
		internal readonly ElementId ControlId;

		/// <summary>
		/// Creates a labeler.
		/// </summary>
		public FormControlLabeler() {
			ControlId = new ElementId();
		}

		/// <summary>
		/// Returns a new label that will be associated with this labeler’s form control.
		/// </summary>
		/// <param name="content"></param>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the element.</param>
		public IReadOnlyCollection<PhrasingComponent> CreateLabel(
			IReadOnlyCollection<PhrasingComponent> content, DisplaySetup displaySetup = null, ElementClassSet classes = null ) =>
			new CustomPhrasingComponent(
				new DisplayableElement(
					context => new DisplayableElementData(
						displaySetup,
						() => {
							if( !ControlId.Id.Any() )
								throw new ApplicationException( "The labeler must be associated with a form control that is on the page." );
							return new DisplayableElementLocalData(
								"label",
								focusDependentData: new DisplayableElementFocusDependentData( attributes: Tuple.Create( "for", ControlId.Id ).ToCollection() ) );
						},
						classes: classes,
						children: content ) ).ToCollection() ).ToCollection();
	}
}