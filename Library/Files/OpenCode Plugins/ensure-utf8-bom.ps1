param([Parameter(Mandatory=$true)][string]$FilePath)

$utf8 = New-Object System.Text.UTF8Encoding $false
$content = [System.IO.File]::ReadAllText($FilePath, $utf8)

$utf8WithBom = New-Object System.Text.UTF8Encoding $true
[System.IO.File]::WriteAllText($FilePath, $content, $utf8WithBom)
