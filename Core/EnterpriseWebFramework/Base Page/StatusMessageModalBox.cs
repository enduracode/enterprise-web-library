#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework;

internal class StatusMessageModalBox: EtherealComponent {
	// Status messages must be retrieved after PageBase.getContent in case that method adds them.
	IReadOnlyCollection<EtherealComponentOrElement> EtherealComponent.GetChildren() =>
		new ModalBox(
			new ModalBoxId(),
			true,
			new FlowIdContainer(
				new Section(
					"Messages",
					PageBase.Current.StatusMessages.Any() && !BasePageStatics.StatusMessagesDisplayAsNotification()
						? new StatusMessageList().ToCollection()
						: Enumerable.Empty<FlowComponent>().Materialize() ).ToCollection() ).ToCollection(),
			open: PageBase.Current.StatusMessages.Any() && !BasePageStatics.StatusMessagesDisplayAsNotification() ).ToCollection();
}