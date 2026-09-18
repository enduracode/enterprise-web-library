param(
	[Parameter( Mandatory = $true )][string]$Repository,
	[Parameter( Mandatory = $true )][string]$Item,
	[ValidateSet( 'working', 'index' )][string]$Source = 'working',
	[switch]$PrepareOnly
)
$ErrorActionPreference = 'Stop'
$json = & node ( Join-Path $PSScriptRoot 'review.mjs' ) prepare-diffs --repo $Repository --item $Item --source $Source
if( $LASTEXITCODE -ne 0 ) { throw 'Could not prepare review diffs.' }
$report = $json | ConvertFrom-Json
if( !$PrepareOnly ) {
	# Windows installations contain both code.cmd and an extensionless shell script.
	$code = Get-Command code.cmd -CommandType Application -ErrorAction Stop
	$diffs = @($report.navigation.diffs)
	if( !$diffs.Count ) { throw 'No diffs in this review item.' }
	if( !( 'MigrationReviewWindow' -as [type] ) ) {
		Add-Type @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class MigrationReviewWindow {
	private delegate bool Callback(IntPtr window, IntPtr state);
	[DllImport("user32.dll")] private static extern bool EnumWindows(Callback callback, IntPtr state);
	[DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
	[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
	[DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
	public static IntPtr Find(string title) {
		IntPtr result = IntPtr.Zero;
		EnumWindows((window, state) => {
			var text = new StringBuilder(4096);
			GetWindowText(window, text, text.Capacity);
			if (text.ToString() == title) { result = window; return false; }
			return true;
		}, IntPtr.Zero);
		return result;
	}
}
'@
	}
	# Opening is intentional UI navigation, unlike the passive VS editor probe.
	& $code.Source --new-window $report.navigation.workspace
	if( $LASTEXITCODE -ne 0 ) { throw 'VS Code could not open the review window.' }
	$window = [IntPtr]::Zero
	for( $attempt = 0; $attempt -lt 80; $attempt++ ) {
		$window = [MigrationReviewWindow]::Find( $report.navigation.windowTitle )
		if( $window -ne [IntPtr]::Zero ) { break }
		Start-Sleep -Milliseconds 250
	}
	if( $window -eq [IntPtr]::Zero ) { throw 'Review workspace did not become ready. Check VS Code workspace trust/startup; no diffs were sent to another window.' }
	for( $i = 0; $i -lt $diffs.Count; $i++ ) {
		[void][MigrationReviewWindow]::SetForegroundWindow( $window )
		Start-Sleep -Milliseconds 150
		if( [MigrationReviewWindow]::GetForegroundWindow() -ne $window ) { throw 'Could not target the review window. Focus it and retry; no diff was sent to another window.' }
		& $code.Source --reuse-window --diff $diffs[$i].left $diffs[$i].right
		if( $LASTEXITCODE -ne 0 ) { throw 'VS Code could not open a review tab.' }
	}
}
$json
