using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A modal box.
/// </summary>
public class ModalBox: EtherealComponent {
	private static readonly ElementClass boxClass = new( "ewfMdl" );
	private static readonly ElementClass closeButtonContainerClass = new( "ewfMdlB" );
	private static readonly ElementClass contentContainerClass = new( "ewfMdlC" );

	private static Func<ModalBoxId> browsingModalBoxIdGetter;

	[ UsedImplicitly ]
	internal class CssElementCreator: ControlCssElementCreator {
		internal static IReadOnlyCollection<string> GetContainerSelectors( string additionalSelector ) =>
			"dialog.{0}".FormatWith( boxClass.ClassName + additionalSelector ).ToCollection();

		internal static IReadOnlyCollection<string> GetBackdropSelectors( string additionalSelector ) =>
			"dialog.{0}::backdrop".FormatWith( boxClass.ClassName + additionalSelector )
				.ToCollection()
				.Append( "dialog.{0} + .backdrop".FormatWith( boxClass.ClassName ) )
				.Materialize();

		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
			new CssElement( "ModalBox", "div.{0}".FormatWith( boxClass.ClassName ) ).ToCollection()
				.Append( new CssElement( "ModalBoxCloseButtonContainer", "div.{0}".FormatWith( closeButtonContainerClass.ClassName ) ) )
				.Append( new CssElement( "ModalBoxContentContainer", "div.{0}".FormatWith( contentContainerClass.ClassName ) ) )
				.Append( new CssElement( "ModalBoxContainer", GetContainerSelectors( "" ).ToArray() ) )
				.Append( new CssElement( "ModalBoxBackdrop", GetBackdropSelectors( "" ).ToArray() ) )
				.Materialize();
	}

	internal static void Init( Func<ModalBoxId> browsingModalBoxIdGetter ) {
		ModalBox.browsingModalBoxIdGetter = browsingModalBoxIdGetter;
	}

	internal static EtherealComponent CreateBrowsingModalBox( ModalBoxId id ) => new ModalBox( id, true, Enumerable.Empty<FlowComponent>().Materialize() );

	internal static string GetBrowsingModalBoxOpenStatements( BrowsingContextSetup browsingContextSetup, string url ) {
		browsingContextSetup ??= new BrowsingContextSetup();

		// As of February 2018, iOS ignores iframe width and height styles, and sizes them to fit their content. See
		// http://andyshora.com/iframes-responsive-web-apps-tips.html.
		return "if( !!navigator.platform && /iPad|iPhone|iPod/.test( navigator.platform ) ) window.location = '{0}'; else {{ {1} }}".FormatWith(
			url,
			StringTools.ConcatenateWithDelimiter(
				" ",
				"var dl = document.getElementById( '{0}' );".FormatWith( browsingModalBoxIdGetter().ElementId.Id ),
				"var dv = dl.firstElementChild;",
				"$( dv ).children( 'iframe' ).remove();",
				"var fr = document.createElement( 'iframe' );",
				"fr.src = '{0}';".FormatWith( url ),
				"fr.style.width = '{0}';".FormatWith( browsingContextSetup.Width?.Value ?? "" ),
				"fr.style.height = '{0}';".FormatWith( browsingContextSetup.Height?.Value ?? "" ),
				"dv.insertAdjacentElement( 'beforeend', fr );",
				"dl.showModal();" ) );
	}

	private readonly IReadOnlyCollection<EtherealComponent> children;

	/// <summary>
	/// Creates a modal box.
	/// </summary>
	/// <param name="id"></param>
	/// <param name="includeCloseButton"></param>
	/// <param name="content"></param>
	/// <param name="classes">The classes on the dialog element.</param>
	/// <param name="open"></param>
	public ModalBox( ModalBoxId id, bool includeCloseButton, IReadOnlyCollection<FlowComponent> content, ElementClassSet classes = null, bool open = false ) {
		children = new ElementComponent(
			context => new ElementData(
				() => new ElementLocalData(
					"dialog",
					focusDependentData: new ElementFocusDependentData(
						includeIdAttribute: true,
						jsInitStatements: ( includeCloseButton
							                    ? "$( '#{0}' ).click( function( e ) {{ if( e.target.id === '{0}' ) e.target.close(); }} );".FormatWith( context.Id )
							                    : "" ).ConcatenateWithSpace( open ? "document.getElementById( '{0}' ).showModal();".FormatWith( context.Id ) : "" ) ) ),
				classes: boxClass.Add( classes ?? ElementClassSet.Empty ),
				clientSideIdReferences: id.ElementId.ToCollection(),
				children: new GenericFlowContainer(
					( includeCloseButton
						  ? new GenericFlowContainer(
							  new EwfButton(
								  new StandardButtonStyle( "Close", icon: new ActionComponentIcon( new FontAwesomeIcon( "fa-times" ) ) ),
								  behavior: new CustomButtonBehavior( () => "document.getElementById( '{0}' ).close();".FormatWith( context.Id ) ) ).ToCollection(),
							  classes: closeButtonContainerClass ).ToCollection<FlowComponent>()
						  : Enumerable.Empty<FlowComponent>() ).Concat(
						id == browsingModalBoxIdGetter() ? content : new GenericFlowContainer( content, classes: contentContainerClass ).ToCollection() )
					.Materialize(),
					classes: boxClass ).ToCollection() ) ).ToCollection();
	}

	IReadOnlyCollection<EtherealComponentOrElement> EtherealComponent.GetChildren() => children;
}