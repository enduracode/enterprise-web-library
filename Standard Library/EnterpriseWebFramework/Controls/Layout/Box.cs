using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A captioned rectangular area that distinguishes itself from its surroundings.
	/// </summary>
	public class Box: WebControl, ControlTreeDataLoader {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfBox";

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				return new[]
				       	{
				       		new CssElement( "Box", "div." + CssClass ),
				       		new CssElement( "BoxHeadingAllLevels", HeadingLevelStatics.HeadingElements.Select( i => i + "." + CssClass ).ToArray() )
				       	};
			}
		}

		private readonly string heading;
		private readonly HeadingLevel headingLevel;
		private readonly Control[] childControls;

		/// <summary>
		/// Creates a box.
		/// </summary>
		public Box( params Control[] childControls ): this( "", childControls ) {}

		/// <summary>
		/// Creates a box. Do not pass null for the heading.
		/// </summary>
		public Box( string heading, params Control[] childControls ): this( heading, HeadingLevel.H2, childControls ) {}

		/// <summary>
		/// Creates a box. Do not pass null for the heading.
		/// </summary>
		public Box( string heading, HeadingLevel headingLevel, params Control[] childControls ) {
			this.heading = heading;
			this.headingLevel = headingLevel;
			this.childControls = childControls;
		}

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			CssClass = CssClass.ConcatenateWithSpace( CssElementCreator.CssClass );
			if( heading.Length > 0 ) {
				this.AddControlsReturnThis( new Heading( heading.GetLiteralControl() )
				                            	{ Level = headingLevel, CssClass = CssElementCreator.CssClass, ExcludesBuiltInCssClass = true } );
			}
			this.AddControlsReturnThis( childControls );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}