using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A captioned rectangular area that distinguishes itself from its surroundings.
	/// </summary>
	public class Box: WebControl, ControlTreeDataLoader {
		private const string closedClass = "ewfBoxC";
		private const string expandedClass = "ewfBoxE";
		private const string headingAndContentClass = "ewfBox";

		internal class CssElementCreator: ControlCssElementCreator {
			CssElement[] ControlCssElementCreator.CreateCssElements() {
				const string closedSelector = "div." + closedClass;
				const string expandedSelector = "div." + expandedClass;
				return new[]
				       	{
				       		new CssElement( "BoxBothStates", closedSelector, expandedSelector ), new CssElement( "BoxClosedState", closedSelector ),
				       		new CssElement( "BoxExpandedState", expandedSelector ),
				       		new CssElement( "BoxHeadingAllLevels", HeadingLevelStatics.HeadingElements.Select( i => i + "." + headingAndContentClass ).ToArray() ),
				       		new CssElement( "BoxContentBlock", "div." + headingAndContentClass )
				       	};
			}
		}

		private readonly string heading;
		private readonly HeadingLevel headingLevel;
		private readonly Control[] childControls;
		private readonly bool? expanded;

		/// <summary>
		/// Creates a box.
		/// </summary>
		/// <param name="childControls">The box content.</param>
		/// <param name="expanded">Set to true or false if you want users to be able to expand or close the box by clicking on the heading.</param>
		public Box( IEnumerable<Control> childControls, bool? expanded = null ): this( "", childControls, expanded: expanded ) {}

		/// <summary>
		/// Creates a box. Do not pass null for the heading.
		/// </summary>
		/// <param name="heading">The box heading.</param>
		/// <param name="childControls">The box content.</param>
		/// <param name="headingLevel">The level of the box heading.</param>
		/// <param name="expanded">Set to true or false if you want users to be able to expand or close the box by clicking on the heading.</param>
		public Box( string heading, IEnumerable<Control> childControls, HeadingLevel headingLevel = HeadingLevel.H2, bool? expanded = null ) {
			this.heading = heading;
			this.headingLevel = headingLevel;
			this.childControls = childControls.ToArray();
			this.expanded = expanded;
		}

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			CssClass = CssClass.ConcatenateWithSpace( !expanded.HasValue || expanded.Value ? expandedClass : closedClass );
			if( heading.Length > 0 ) {
				this.AddControlsReturnThis( new Heading( heading.GetLiteralControl() )
				                            	{ Level = headingLevel, CssClass = headingAndContentClass, ExcludesBuiltInCssClass = true } );
			}
			this.AddControlsReturnThis( new Block( childControls ) { CssClass = headingAndContentClass } );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}