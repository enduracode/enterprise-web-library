param([Parameter(Mandatory=$true)][string]$FilePath)

# Detect if file has BOM
$bytes = [System.IO.File]::ReadAllBytes($FilePath)
$hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF

# Read and normalize line endings
$content = [System.IO.File]::ReadAllText($FilePath)
$content = $content -replace "`r`n", "`n" -replace "`r", "`n" -replace "`n", "`r`n"

# Write back preserving BOM if present
$encoding = if ($hasBom) { New-Object System.Text.UTF8Encoding $true } else { New-Object System.Text.UTF8Encoding $false }
[System.IO.File]::WriteAllText($FilePath, $content, $encoding)
