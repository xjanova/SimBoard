# Fetches the ngspice engine into tools/Spice64. Not committed: it is a 14 MB binary.
# ngspice is a separate process, never linked into SimBoard — see PLAN.html.
param([string]$Version = "47")
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$archive = Join-Path $PSScriptRoot "ngspice-${Version}_64.7z"
$url = "https://sourceforge.net/projects/ngspice/files/ng-spice-rework/$Version/ngspice-${Version}_64.7z/download"

if (Test-Path (Join-Path $PSScriptRoot "Spice64\bin\ngspice_con.exe")) {
    Write-Host "ngspice already present."; exit 0
}
Write-Host "Downloading ngspice $Version ..."
Invoke-WebRequest -Uri $url -OutFile $archive -MaximumRedirection 10

$sevenZip = "C:\Program Files\7-Zip\7z.exe"
if (-not (Test-Path $sevenZip)) { throw "7-Zip is required to unpack the ngspice archive." }
& $sevenZip x $archive "-o$PSScriptRoot" -y | Out-Null
Remove-Item $archive

$exe = Join-Path $PSScriptRoot "Spice64\bin\ngspice_con.exe"
if (-not (Test-Path $exe)) { throw "Extraction finished but $exe is missing." }
& $exe --version | Select-Object -First 2
