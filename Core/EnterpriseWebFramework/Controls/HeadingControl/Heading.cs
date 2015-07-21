using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A heading. This is currently only meant to be used from the codebehind because using it in the markup
	/// requires asp:Literals.
	/// </summary>
	[ Obsolete( "Guaranteed through 30 April 2015. Please use the Section control instead." ) ]
	[ ParseChildren( ChildrenAsProperties = true, DefaultProperty = "MarkupChildControls" ) ]
	public class Heading: WebControl, ControlTreeDataLoader {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfHead";

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				return HeadingLevelStatics.HeadingElements.Select( i => new CssElement( i.ToUpper(), i + "." + CssClass ) ).ToArray();
			}
		}

		private readonly List<Control> markupControls = new List<Control>();
		private readonly List<Control> codeControls = new List<Control>();

		/// <summary>
		/// The level of the heading.
		/// </summary>
		[ Obsolete( "Guaranteed through 30 April 2015. Please use the Section control instead." ) ]
		public HeadingLevel Level { get; set; }

		/// <summary>
		/// Creates a heading with no child controls.
		/// </summary>
		[ Obsolete( "Guaranteed through 30 April 2015. Please use the Section control instead." ) ]
		public Heading() {
			Level = HeadingLevel.H2;
		}

		/// <summary>
		/// Creates a heading with the specified child controls.
		/// </summary>
		[ Obsolete( "Guaranteed through 30 April 2015. Please use the Section control instead." ) ]
		public Heading( params Control[] childControls ): this() {
			codeControls.AddRange( childControls );
		}

		/// <summary>
		/// Markup use only.
		/// </summary>
		public List<Control> MarkupChildControls { get { return markupControls; } }

		/// <summary>
		/// Adds the specified child controls to the heading.
		/// </summary>
		[ Obsolete( "Guaranteed through 30 April 2015. Please use the Section control instead." ) ]
		public void AddChildControls( params Control[] controls ) {
			codeControls.AddRange( controls );
		}

		void ControlTreeDataLoader.LoadData() {
			CssClass = CssClass.ConcatenateWithSpace( CssElementCreator.CssClass );
			this.AddControlsReturnThis( markupControls.Concat( codeControls ) );
		}

		/// <summary>
		/// Returns a heading tag. The number depends on the level of this heading.
		/// </summary>
		protected override HtmlTextWriterTag TagKey {
			get {
				switch( Level ) {
					case HeadingLevel.H2:
						return HtmlTextWriterTag.H2;
					case HeadingLevel.H3:
						return HtmlTextWriterTag.H3;
					default:
						return HtmlTextWriterTag.H4;
				}
			}
		}
	}
}