# SHA-256 of a full file or inclusive line range; never stages or writes VCS objects.
param(
	[Parameter( Mandatory = $true )]
	[string]$Repository,
	[Parameter( Mandatory = $true )]
	[string]$RelativePath,
	[ValidateSet( 'WorkingTree', 'Index' )]
	[string]$Source = 'WorkingTree',
	[int]$StartLine = 1,
	[int]$EndLine = 0
)

$ErrorActionPreference = 'Stop'
$root = ( Resolve-Path -LiteralPath $Repository ).ProviderPath.TrimEnd( '\', '/' )
if( [System.IO.Path]::IsPathRooted( $RelativePath ) ) { throw 'RelativePath must be relative to Repository.' }
$path = [System.IO.Path]::GetFullPath( ( Join-Path $root $RelativePath ) )
if( !$path.StartsWith( $root + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase ) ) {
	throw 'RelativePath must remain inside Repository.'
}
$relative = $path.Substring( $root.Length + 1 ).Replace( '\', '/' )
if( $Source -eq 'Index' ) {
	# Explicit UTF-8 avoids Windows PowerShell 5.1 native-output decoding differences.
	$info = New-Object System.Diagnostics.ProcessStartInfo
	$info.FileName = 'git'
	$info.Arguments = 'show ":' + $relative + '"'
	$info.WorkingDirectory = $root
	$info.UseShellExecute = $false
	$info.CreateNoWindow = $true
	$info.RedirectStandardOutput = $true
	$info.RedirectStandardError = $true
	$info.StandardOutputEncoding = New-Object System.Text.UTF8Encoding( $false, $true )
	$process = New-Object System.Diagnostics.Process
	$process.StartInfo = $info
	try {
		[void]$process.Start()
		$output = $process.StandardOutput.ReadToEndAsync()
		$errorOutput = $process.StandardError.ReadToEndAsync()
		if( !$process.WaitForExit( 10000 ) ) { $process.Kill(); throw 'git show timed out.' }
		if( $process.ExitCode -ne 0 ) { throw $errorOutput.GetAwaiter().GetResult() }
		$text = $output.GetAwaiter().GetResult()
	}
	finally { $process.Dispose() }
}
else {
	$text = [System.IO.File]::ReadAllText( $path )
}

# Ignore encoding BOM and newline conventions, but preserve other whitespace and content.
$text = $text.TrimStart( [char]0xfeff ).Replace( "`r`n", "`n" ).Replace( "`r", "`n" )
$lines = $text.Split( [char]10 )
if( $EndLine -eq 0 ) { $EndLine = $lines.Length }
if( $StartLine -lt 1 -or $EndLine -lt $StartLine -or $EndLine -gt $lines.Length ) {
	throw "Invalid inclusive range $StartLine-$EndLine for $($lines.Length) lines."
}
$content = [string]::Join( "`n", [string[]]$lines[ ($StartLine - 1)..($EndLine - 1) ] )
$hash = [System.Security.Cryptography.SHA256]::Create()
try {
	$fingerprint = [BitConverter]::ToString( $hash.ComputeHash( [Text.Encoding]::UTF8.GetBytes( $content ) ) ).Replace( '-', '' ).ToLowerInvariant()
}
finally { $hash.Dispose() }
[pscustomobject]@{
	Path = $relative
	Source = $Source
	StartLine = $StartLine
	EndLine = $EndLine
	Sha256 = $fingerprint
} | ConvertTo-Json
