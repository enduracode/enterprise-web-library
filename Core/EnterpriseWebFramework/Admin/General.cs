using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ElementBase.Classification;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Admin;

// EwlPage
partial class General {
	private static readonly ElementClass valueFocusedClass = new( "ewfValueFocused" /* This is used by EWF CSS files. */ );

	protected override PageContent getContent() =>
		new UiPageContent(
				pageActions:
				new ButtonSetup(
						"Send Health Check",
						behavior: new PostBackBehavior( postBack: PostBack.CreateFull( id: "sendHealthCheck", modificationMethod: TelemetryStatics.SendHealthCheck ) ) )
					.Add(
						new ButtonSetup(
							"Throw Unhandled Exception",
							behavior: new PostBackBehavior( postBack: PostBack.CreateFull( id: "throwException", modificationMethod: throwException ) ) ) ),
				omitContentBox: true )
			.Add(
				new Section(
					"Information",
					new Section(
							"Logic",
							getList()
								.AddItem( ConfigurationStatics.AppName.ToFormItem( label: "Application".ToComponents() ) )
								.AddItem( ConfigurationStatics.AppAssembly.GetName().Version!.ToString().ToFormItem( label: "Version".ToComponents() ) )
								.ToCollection() )
						.Append(
							new Section(
								"Installation",
								getList()
									.AddItem( ConfigurationStatics.InstallationConfiguration.InstallationName.ToFormItem( label: "Installation".ToComponents() ) )
									.AddItems( getLogItems().Materialize() )
									.AddItem( Tewl.Tools.NetTools.GetLocalHostName().ToFormItem( label: "Machine".ToComponents() ) )
									.ToCollection() ) )
						.Materialize(),
					style: SectionStyle.Box ) )
			.Add(
				new Section(
					"Health monitoring",
					new StackList(
							new EwfHyperlink( new HealthMonitoring.SystemHealth(), new StandardHyperlinkStyle( "System health endpoint" ) ).ToComponentListItem()
								.Append(
									new EwfHyperlink( new HealthMonitoring.AppHealth(), new StandardHyperlinkStyle( "Application health endpoint" ) ).ToComponentListItem() ) )
						.ToCollection(),
					style: SectionStyle.Box ) );

	private void throwException() => throw new ApplicationException( $"This is a test from the {ResourceFullName} page." );

	private IEnumerable<FormItem> getLogItems() {
		yield return ( File.Exists( EwfConfigurationStatics.AppConfiguration.DiagnosticLogFilePath )
			               ? FormattingMethods.GetFormattedBytes( new FileInfo( EwfConfigurationStatics.AppConfiguration.DiagnosticLogFilePath ).Length )
			               : "–" ).ToFormItem( label: "Diagnostic log size".ToComponents() );
		yield return ( File.Exists( EwfConfigurationStatics.AppConfiguration.DebugLogFilePath )
			               ? FormattingMethods.GetFormattedBytes( new FileInfo( EwfConfigurationStatics.AppConfiguration.DebugLogFilePath ).Length )
			               : "–" ).ToFormItem( label: "Two-day debug log size".ToComponents() );
	}

	private FormItemList getList() =>
		FormItemList.CreateResponsiveGrid(
			generalSetup: new FormItemListSetup( classes: valueFocusedClass ),
			columnMinWidth: 20.ToEm(),
			columnMaxWidth: 20.ToEm() );
}