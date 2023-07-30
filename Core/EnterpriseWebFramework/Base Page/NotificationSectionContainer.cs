#nullable disable
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

internal class NotificationSectionContainer: FlowComponent {
	private static readonly ElementClass elementClass = new( "ewfNotification" );

	[ UsedImplicitly ]
	private class CssElementCreator: ControlCssElementCreator {
		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
			new CssElement( "NotificationSectionContainer", "{0} div.{1}".FormatWith( BasePageStatics.FormSelector, elementClass.ClassName ) ).ToCollection();
	}

	// Status messages must be retrieved after PageBase.getContent in case that method adds them.
	IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() =>
		new FlowIdContainer(
			PageBase.Current.StatusMessages.Any() && BasePageStatics.StatusMessagesDisplayAsNotification()
				? new GenericFlowContainer(
					new Section( null, SectionStyle.Box, null, "Messages", null, new StatusMessageList().ToCollection(), true, true, null ).ToCollection(),
					classes: elementClass ).ToCollection()
				: Enumerable.Empty<FlowComponent>().Materialize() ).ToCollection();
}