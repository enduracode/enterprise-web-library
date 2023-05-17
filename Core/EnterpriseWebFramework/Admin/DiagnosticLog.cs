using Serilog.Core;
using Serilog.Events;

// EwlPage

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Admin;

partial class DiagnosticLog {
	private const LogEventLevel debugDisabledLevel = LogEventLevel.Warning;
	private const LogEventLevel debugEnabledLevel = LogEventLevel.Debug;
	internal static readonly LoggingLevelSwitch LevelSwitch = new( initialMinimumLevel: debugDisabledLevel );

	protected override PageContent getContent() {
		var logText = "";
		if( File.Exists( EwfConfigurationStatics.AppConfiguration.DiagnosticLogFilePath ) )
			using( var reader = new StreamReader(
				      File.Open( EwfConfigurationStatics.AppConfiguration.DiagnosticLogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ) ) )
				logText = reader.ReadToEnd();

		return new UiPageContent(
			bodyClasses: new ElementClass( "ewfDiagnosticLog" /* This is used by EWF CSS files. */ ),
			pageActions: new ButtonSetup(
				"{0} Debug Logging".FormatWith( LevelSwitch.MinimumLevel is debugEnabledLevel ? "Disable" : "Enable" ),
				behavior: new PostBackBehavior(
					postBack: PostBack.CreateFull(
						id: "debug",
						modificationMethod: () => {
							LevelSwitch.MinimumLevel = LevelSwitch.MinimumLevel is debugEnabledLevel ? debugDisabledLevel : debugEnabledLevel;
						} ) ) ).ToCollection() ).Add(
			(FlowComponent)new DisplayableElement(
				_ => new DisplayableElementData(
					null,
					() => new DisplayableElementLocalData( "pre" ),
					children: new DisplayableElement(
						_ => new DisplayableElementData( null, () => new DisplayableElementLocalData( "samp" ), children: logText.ToComponents() ) ).ToCollection() ) ) );
	}
}