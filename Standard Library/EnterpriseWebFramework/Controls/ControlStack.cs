using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Used to stack inline controls vertically. Implemented with div blocks.
	/// </summary>
	[ ParseChildren( ChildrenAsProperties = true, DefaultProperty = "MarkupControls" ) ]
	public class ControlStack: WebControl, ControlTreeDataLoader {
		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfControlStack";
			internal const string ItemCssClass = "ewfControlStackItem";

			/// <summary>
			/// Standard Library use only.
			/// </summary>
			public static readonly string[] Selectors = new[] { "div." + CssClass, "div." + CssClass + ".ewfStandard" };

			/// <summary>
			/// Standard Library use only.
			/// </summary>
			public const string ItemSelector = "div." + ItemCssClass;

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				return new[]
				       	{
				       		new CssElement( "ControlStack" /*NOTE: Rename to ControlStackAllStyles.*/, Selectors ),
				       		new CssElement( "StandardControlStack" /*NOTE: Rename to ControlStackStandardStyle.*/, "div." + CssClass + ".ewfStandard" ),
				       		new CssElement( "ControlStackItem", ItemSelector )
				       	};
			}
		}

		private readonly List<Control> markupControls = new List<Control>();
		private readonly List<Tuple<Func<IEnumerable<Control>>, bool>> codeControls = new List<Tuple<Func<IEnumerable<Control>>, bool>>();
		private bool? isStandard;
		private int modErrorDisplayKeySuffix;

		/// <summary>
		/// Creates a blank vertical stack of controls.
		/// </summary>
		public static ControlStack Create( bool isStandard ) {
			return new ControlStack { isStandard = isStandard };
		}

		/// <summary>
		/// Creates a vertical stack of text controls out of the given list of strings.
		/// </summary>
		public static ControlStack CreateWithText( bool isStandard, params string[] text ) {
			var cs = Create( isStandard );
			cs.AddText( text );
			return cs;
		}

		/// <summary>
		/// Creates a control stack and adds the specified controls to it.
		/// </summary>
		public static ControlStack CreateWithControls( bool isStandard, params Control[] controls ) {
			var cs = Create( isStandard );
			cs.AddControls( controls );
			return cs;
		}

		/// <summary>
		/// Do not use.
		/// </summary>
		public ControlStack() {}

		/// <summary>
		/// Markup use only.
		/// </summary>
		public List<Control> MarkupControls { get { return markupControls; } }

		/// <summary>
		/// Sets whether or not this control stack will have standard styling.
		/// </summary>
		public bool IsStandard {
			internal get {
				if( !isStandard.HasValue )
					throw new ApplicationException( "Please explicitly specify either true or false for the IsStandard attribute." );
				return isStandard.Value;
			}
			set { isStandard = value; }
		}

		/// <summary>
		/// Add the given list of strings to the control stack. Do not pass null for any of the strings. If you do, it will be converted to the empty string.
		/// </summary>
		public void AddText( params string[] text ) {
			foreach( var s in text )
				AddControls( new Literal { Text = ( s ?? "" ).GetTextAsEncodedHtml() } );
		}

		/// <summary>
		/// Adds the specified controls to the stack.
		/// </summary>
		public void AddControls( params Control[] controls ) {
			codeControls.AddRange( controls.Select( i => Tuple.Create( new Func<IEnumerable<Control>>( i.ToSingleElementArray ), false ) ) );
		}

		/// <summary>
		/// Adds an item for the error messages from the specified validation. If there aren't any error messages, the control getter is not called and no item is
		/// added.
		/// </summary>
		public void AddModificationErrorItem( Validation validation, Func<IEnumerable<string>, IEnumerable<Control>> controlGetter ) {
			codeControls.Add( Tuple.Create( new Func<IEnumerable<Control>>( () => {
				var errors = EwfPage.Instance.AddModificationErrorDisplayAndGetErrors( this, modErrorDisplayKeySuffix++.ToString(), validation );
				return errors.Any() ? controlGetter( errors ) : new Control[ 0 ];
			} ),
			                                true ) );
		}

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			this.AddControlsReturnThis( markupControls.SelectMany( i => getItemControl( i.ToSingleElementArray() ) ) );
			this.AddControlsReturnThis(
				codeControls.SelectMany( i => i.Item2 ? new NamingPlaceholder( getItemControl( i.Item1() ).ToArray() ).ToSingleElementArray() : getItemControl( i.Item1() ) ) );
		}

		private IEnumerable<Control> getItemControl( IEnumerable<Control> controls ) {
			if( !controls.Any() )
				yield break;
			yield return new Block( controls.ToArray() ) { CssClass = CssElementCreator.ItemCssClass };
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }

		/// <summary>
		/// Renders this control after applying the appropriate CSS classes.
		/// </summary>
		protected override void Render( HtmlTextWriter writer ) {
			CssClass = CssClass.ConcatenateWithSpace( CssElementCreator.CssClass );

			// We use the property here so we get the informative exception if isStandard is null.
			if( IsStandard )
				CssClass = CssClass.ConcatenateWithSpace( "ewfStandard" );

			base.Render( writer );
		}
	}
}