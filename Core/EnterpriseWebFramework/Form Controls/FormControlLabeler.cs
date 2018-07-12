using System;
using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An object that creates labels for a form control.
	/// </summary>
	public class FormControlLabeler {
		private readonly ElementId controlId;

		/// <summary>
		/// Creates a labeler.
		/// </summary>
		public FormControlLabeler() {
			controlId = new ElementId();
		}

		/// <summary>
		/// Returns a new label that will be associated with this labeler’s form control.
		/// </summary>
		/// <param name="content"></param>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the element.</param>
		public IReadOnlyCollection<PhrasingComponent> CreateLabel(
			IReadOnlyCollection<PhrasingComponent> content, DisplaySetup displaySetup = null, ElementClassSet classes = null ) {
			return new CustomPhrasingComponent(
				new DisplayableElement(
					context => new DisplayableElementData(
						displaySetup,
						() => {
							if( !controlId.Id.Any() )
								throw new ApplicationException( "The labeler must be associated with a form control that is on the page." );
							return new DisplayableElementLocalData(
								"label",
								focusDependentData: new DisplayableElementFocusDependentData( attributes: Tuple.Create( "for", controlId.Id ).ToCollection() ) );
						},
						classes: classes,
						children: content ) ).ToCollection() ).ToCollection();
		}

		internal void AddControlId( string id ) {
			controlId.AddId( id );
		}
	}
}