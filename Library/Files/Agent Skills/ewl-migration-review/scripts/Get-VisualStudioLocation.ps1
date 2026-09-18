# Read-only, on-demand query. Run with Windows PowerShell 5.1 and a bounded tool timeout.
param(
	[Parameter( Mandatory = $true )]
	[string]$SolutionPath,
	[int]$ProcessId = 0
)

$ErrorActionPreference = 'Stop'
if( !( Test-Path -LiteralPath $SolutionPath -PathType Leaf ) ) {
	throw "Solution file does not exist: $SolutionPath"
}
$expectedSolution = ( Resolve-Path -LiteralPath $SolutionPath ).ProviderPath

if( !( 'MigrationReviewRunningObjects' -as [type] ) ) {
	Add-Type @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

public static class MigrationReviewRunningObjects {
	[DllImport("ole32.dll")]
	private static extern int CreateBindCtx(uint reserved, out IBindCtx bindContext);

	public static IDictionary<string, object> GetObjects() {
		IBindCtx context = null;
		IRunningObjectTable table = null;
		IEnumMoniker enumerator = null;
		try {
			Marshal.ThrowExceptionForHR(CreateBindCtx(0, out context));
			context.GetRunningObjectTable(out table);
			table.EnumRunning(out enumerator);
			var objects = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			var monikers = new IMoniker[1];
			while (enumerator.Next(1, monikers, IntPtr.Zero) == 0) {
				try {
					string name;
					monikers[0].GetDisplayName(context, null, out name);
					if (!name.TrimStart('!').StartsWith("VisualStudio.DTE.", StringComparison.OrdinalIgnoreCase))
						continue;
					object value;
					table.GetObject(monikers[0], out value);
					if (value != null)
						objects[name] = value;
				}
				catch (UnauthorizedAccessException) {
					// A ROT entry belonging to another security context may be inaccessible.
				}
				finally { Marshal.ReleaseComObject(monikers[0]); }
			}
			return objects;
		}
		finally {
			if (enumerator != null) Marshal.ReleaseComObject(enumerator);
			if (table != null) Marshal.ReleaseComObject(table);
			if (context != null) Marshal.ReleaseComObject(context);
		}
	}
}
'@
}

$instances = @()
$errors = @()
foreach( $entry in [MigrationReviewRunningObjects]::GetObjects().GetEnumerator() ) {
	$instanceId = 0
	if( $entry.Key -match ':(\d+)$' ) { $instanceId = [int]$Matches[ 1 ] }
	if( $ProcessId -ne 0 -and $ProcessId -ne $instanceId ) { continue }
	for( $attempt = 0; $attempt -lt 3; $attempt++ ) {
		try {
			$dte = $entry.Value
			$solution = $dte.Solution.FullName
			if( [string]::IsNullOrWhiteSpace( $solution ) -or
				![string]::Equals( [System.IO.Path]::GetFullPath( $solution ), $expectedSolution, [System.StringComparison]::OrdinalIgnoreCase ) ) {
				break
			}
			$document = $dte.ActiveDocument
			$result = [ordered]@{
				ProcessId = $instanceId
				Solution = $solution
				Document = $null
				EditorState = 'NoDocument'
			}
			if( $null -ne $document ) {
				$result.Document = $document.FullName
				$result.Saved = $document.Saved
				$result.EditorState = 'NoTextSelection'
				$selection = $document.Selection
				if( $null -ne $selection -and $null -ne $selection.ActivePoint ) {
					$result.EditorState = 'Text'
					$result.CaretLine = $selection.ActivePoint.Line
					$result.CaretColumn = $selection.ActivePoint.LineCharOffset
					$result.StartLine = $selection.TopPoint.Line
					$result.StartColumn = $selection.TopPoint.LineCharOffset
					$result.EndLine = $selection.BottomPoint.Line
					$result.EndColumn = $selection.BottomPoint.LineCharOffset
					$result.SelectionIsEmpty = $selection.IsEmpty
					$result.SelectionMode = [int]$selection.Mode
					$text = [string]$selection.Text
					$result.SelectedText = $text.Substring( 0, [Math]::Min( 2000, $text.Length ) )
					$result.SelectedTextTruncated = $text.Length -gt 2000
					$result.FirstDisplayedLine = $selection.TextPane.StartPoint.Line
					# Height is in display rows: folding and wrapping prevent a reliable last source line.
					$result.CaretVisible = $null
					try { $result.CaretVisible = $selection.TextPane.IsVisible( $selection.ActivePoint ) } catch { }
				}
			}
			$instances += [pscustomobject]$result
			break
		}
		catch {
			if( $attempt -eq 2 ) { $errors += "$($entry.Key): $($_.Exception.Message)" }
			else { Start-Sleep -Milliseconds 200 }
		}
	}
}

[pscustomobject]@{
	Solution = $expectedSolution
	Instances = @($instances)
	Errors = @($errors)
} | ConvertTo-Json -Depth 4
