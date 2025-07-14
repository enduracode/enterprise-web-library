using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ElementBase.Classification;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A confirmation dialog box.
/// </summary>
public class ConfirmationDialog: EtherealComponent {
	private static readonly ElementClass buttonListContainerClass = new( "ewfCdbl" );

	[ UsedImplicitly ]
	internal class CssElementCreator: ControlCssElementCreator {
		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
			new CssElement( "ConfirmationDialogButtonListContainer", $"div.{buttonListContainerClass.ClassName}" ).ToCollection();
	}

	private readonly IReadOnlyCollection<EtherealComponent> children;

	/// <summary>
	/// Creates a confirmation dialog.
	/// </summary>
	/// <param name="id"></param>
	/// <param name="content"></param>
	/// <param name="postBack">Pass null to use the post-back corresponding to the first of the current data modifications.</param>
	public ConfirmationDialog( ConfirmationDialogId id, IReadOnlyCollection<FlowComponent> content, PostBack? postBack = null ) {
		children = new ModalBox(
			id.ModalBoxId,
			false,
			content.Append(
					new GenericFlowContainer(
						new WrappingList(
								new EwfButton(
										new StandardButtonStyle( "Cancel" ),
										behavior: new CustomButtonBehavior( () => "document.getElementById( '{0}' ).close();".FormatWith( id.ModalBoxId.ElementId.Id ) ) )
									.ToComponentListItem()
									.AppendWrappingListItem(
										new EwfButton( new StandardButtonStyle( "Continue" ), behavior: new PostBackBehavior( postBack: postBack ) ).ToComponentListItem() ) )
							.ToCollection(),
						classes: buttonListContainerClass ) )
				.Materialize() ).ToCollection();
	}

	IReadOnlyCollection<EtherealComponentOrElement> EtherealComponent.GetChildren() => children;
}