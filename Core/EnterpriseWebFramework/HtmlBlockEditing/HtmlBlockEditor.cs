using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A control for editing an HTML block.
	/// </summary>
	public class HtmlBlockEditor: FormControl<FlowComponent> {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfHtmlBlockEditor";

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				return new[] { new CssElement( "HtmlBlockEditor", "div." + CssClass ) };
			}
		}

		private readonly HtmlBlockEditorModification mod;
		private readonly FlowComponent component;
		private readonly EwfValidation validation;

		/// <summary>
		/// Creates an HTML block editor.
		/// </summary>
		/// <param name="htmlBlockId"></param>
		/// <param name="idSetter"></param>
		/// <param name="mod"></param>
		/// <param name="setup">The setup object for the HTML block editor.</param>
		public HtmlBlockEditor( int? htmlBlockId, Action<int> idSetter, out HtmlBlockEditorModification mod, HtmlBlockEditorSetup setup = null ) {
			setup = setup ?? new HtmlBlockEditorSetup();

			this.mod = mod = new HtmlBlockEditorModification( htmlBlockId, htmlBlockId.HasValue ? HtmlBlockStatics.GetHtml( htmlBlockId.Value ) : "", idSetter );

			var wysiwygEditor = new WysiwygHtmlEditor(
				mod.Html,
				true,
				( postBackValue, validator ) => {
					this.mod.Html = postBackValue;
					if( setup.AdditionalValidationMethod != null )
						setup.AdditionalValidationMethod( validator );
				},
				setup: setup.WysiwygSetup );

			component = new PageElement(
				context => {
					var displaySetup = setup.DisplaySetup ?? new DisplaySetup( true );
					displaySetup.AddJsShowStatements( "$( '#{0}' ).show( 200 );".FormatWith( context.Id ) );
					displaySetup.AddJsHideStatements( "$( '#{0}' ).hide( 200 );".FormatWith( context.Id ) );

					return new ElementData(
						() => {
							var attributes = new List<Tuple<string, string>>();
							attributes.Add( Tuple.Create( "class", CssElementCreator.CssClass ) );
							if( !displaySetup.ComponentsDisplayed )
								attributes.Add( Tuple.Create( "style", "display: none" ) );

							return new ElementLocalData( "div", attributes, displaySetup.UsesJsStatements, "" );
						},
						children: wysiwygEditor.PageComponent.ToSingleElementArray() );
				} );

			validation = wysiwygEditor.Validation;
		}

		public FlowComponent PageComponent { get { return component; } }
		public EwfValidation Validation { get { return validation; } }

		/// <summary>
		/// Gets whether this HTML block has HTML (i.e. is not empty).
		/// </summary>
		public bool HasHtml { get { return mod.Html.Any(); } }
	}
}