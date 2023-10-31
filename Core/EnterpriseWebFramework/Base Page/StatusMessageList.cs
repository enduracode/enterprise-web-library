#nullable disable
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

internal class StatusMessageList: FlowComponent {
	private static readonly ElementClass infoMessageContainerClass = new( "ewfInfoMsg" );
	private static readonly ElementClass warningMessageContainerClass = new( "ewfWarnMsg" );
	private static readonly ElementClass messageTextClass = new( "ewfStatusText" );

	[ UsedImplicitly ]
	private class CssElementCreator: ControlCssElementCreator {
		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
			new CssElement( "InfoMessageContainer", "{0} div.{1}".FormatWith( BasePageStatics.FormSelector, infoMessageContainerClass.ClassName ) ).ToCollection()
				.Append( new CssElement( "WarningMessageContainer", "{0} div.{1}".FormatWith( BasePageStatics.FormSelector, warningMessageContainerClass.ClassName ) ) )
				.Append( new CssElement( "StatusMessageText", "{0} span.{1}".FormatWith( BasePageStatics.FormSelector, messageTextClass.ClassName ) ) )
				.Materialize();
	}

	// Status messages must be retrieved after PageBase.getContent in case that method adds them.
	IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() =>
		new StackList(
			PageBase.Current.StatusMessages.Select(
				i => new GenericFlowContainer(
					new FontAwesomeIcon( i.Item1 == StatusMessageType.Info ? "fa-info-circle" : "fa-exclamation-triangle", "fa-lg", "fa-fw" )
						.Append<PhrasingComponent>( new GenericPhrasingContainer( i.Item2.ToComponents(), classes: messageTextClass ) )
						.Materialize(),
					classes: i.Item1 == StatusMessageType.Info ? infoMessageContainerClass : warningMessageContainerClass ).ToComponentListItem() ) ).ToCollection();
}