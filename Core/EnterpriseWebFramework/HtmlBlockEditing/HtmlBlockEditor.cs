using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A control for editing an HTML block.
/// </summary>
[ PublicAPI ]
public class HtmlBlockEditor: FormControl<FlowComponent> {
	internal class CssElementCreator: ControlCssElementCreator {
		internal const string CssClass = "ewfHtmlBlockEditor";

		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
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
	public HtmlBlockEditor( int? htmlBlockId, Action<int> idSetter, out HtmlBlockEditorModification mod, HtmlBlockEditorSetup? setup = null ) {
		setup ??= new HtmlBlockEditorSetup();

		this.mod = mod = new HtmlBlockEditorModification( htmlBlockId, htmlBlockId.HasValue ? HtmlBlockStatics.GetHtml( htmlBlockId.Value ) : "", idSetter );

		var wysiwygEditor = new WysiwygHtmlEditor(
			mod.Html,
			true,
			( postBackValue, validator ) => {
				this.mod.Html = postBackValue;
				setup.AdditionalValidationMethod?.Invoke( validator );
			},
			setup: setup.WysiwygSetup );

		component = new DisplayableElement(
			_ => {
				return new DisplayableElementData(
					setup.DisplaySetup,
					() => new DisplayableElementLocalData( "div" ),
					classes: new ElementClass( CssElementCreator.CssClass ),
					children: wysiwygEditor.PageComponent.ToCollection() );
			} );

		validation = wysiwygEditor.Validation;
	}

	FormControlLabeler FormControl<FlowComponent>.Labeler => null;
	public FlowComponent PageComponent => component;
	public EwfValidation Validation => validation;

	/// <summary>
	/// Gets whether this HTML block has HTML (i.e. is not empty).
	/// </summary>
	public bool HasHtml => mod.Html.Any();
}