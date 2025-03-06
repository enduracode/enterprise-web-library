using Humanizer;
using Serilog.Core;
using Serilog.Events;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Admin;

// EwlPage
partial class DiagnosticLog {
	private const LogEventLevel debugEnabledLevel = LogEventLevel.Debug;

	private static LoggingLevelSwitch levelSwitch = null!;
	private static LogEventLevel debugDisabledLevel;

	internal static void Init( LoggingLevelSwitch levelSwitch ) {
		DiagnosticLog.levelSwitch = levelSwitch;
		debugDisabledLevel = levelSwitch.MinimumLevel;
	}

	protected internal override bool IsSlow => true;

	protected override PageContent getContent() {
		var logText = "";
		if( File.Exists( EwfConfigurationStatics.AppConfiguration.DiagnosticLogFilePath ) )
			using( var reader = new StreamReader(
				      File.Open( EwfConfigurationStatics.AppConfiguration.DiagnosticLogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ) ) )
				logText = reader.ReadToEnd();

		const int tailLength = 10000;
		return new UiPageContent(
			bodyClasses: new ElementClass( "ewfDiagnosticLog" /* This is used by EWF CSS files. */ ),
			pageActions: new ButtonSetup(
				"Download Full Log",
				behavior: new PostBackBehavior(
					postBack: PostBack.CreateIntermediate(
						null,
						id: "download",
						reloadBehaviorGetter:
						() => new PageReloadBehavior(
							secondaryResponse:
							new SecondaryResponse(
								() => EwfResponse.Create(
									ContentTypes.PlainText,
									new EwfResponseBodyCreator( () => logText ),
									fileNameCreator: () => "Log" + FileExtensions.Txt ) ) ) ) ) ).Add(
				new ButtonSetup(
					"{0} Debug Logging".FormatWith( levelSwitch.MinimumLevel is debugEnabledLevel ? "Disable" : "Enable" ),
					behavior: new PostBackBehavior(
						postBack: PostBack.CreateFull(
							id: "debug",
							modificationMethod: () => {
								levelSwitch.MinimumLevel = levelSwitch.MinimumLevel is debugEnabledLevel ? debugDisabledLevel : debugEnabledLevel;
							} ) ) ) ) ).Add(
			new Section(
				$"Tail of log (last {tailLength.ToWords()} characters)",
				new DisplayableElement(
					_ => new DisplayableElementData(
						null,
						() => new DisplayableElementLocalData( "pre" ),
						children: new DisplayableElement(
							_ => {
								var minIndex = Math.Max( logText.Length - tailLength, 0 );

								var newline = Environment.NewLine;
								var index = logText.IndexOf( newline, minIndex, StringComparison.Ordinal );
								if( index == -1 )
									index = minIndex;
								else
									index += newline.Length;

								return new DisplayableElementData( null, () => new DisplayableElementLocalData( "samp" ), children: logText[ index.. ].ToComponents() );
							} ).ToCollection() ) ).ToCollection() ) );
	}
}