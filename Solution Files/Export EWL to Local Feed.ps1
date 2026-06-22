$ErrorActionPreference = 'Stop'

Set-Location ( Join-Path $PSScriptRoot '..' )
& "Latest DU Package\ewl" export-ewl-to-local-feed