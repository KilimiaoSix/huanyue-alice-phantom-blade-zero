[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$PackagePath,
    [Parameter(Mandatory)] [string]$OutputPath
)
$ErrorActionPreference = 'Stop'
$expectedHash = '624bf932fcd8a18519e8492fa27607f58fab35a0fd59ed87fe335023eec49220'
$package = (Resolve-Path -LiteralPath $PackagePath).Path
if ((Get-FileHash -Algorithm SHA256 -LiteralPath $package).Hash -ne $expectedHash) {
    throw 'The package is not the validated R13 ZIP.'
}
$target = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'The .NET Framework compiler is required.' }
New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
$compilerArgs = @('/nologo', '/codepage:65001', '/warnaserror', '/optimize+', '/target:winexe', '/platform:anycpu',
    '/r:System.IO.Compression.dll', '/r:System.Drawing.dll', '/r:System.Windows.Forms.dll',
    "/resource:$package,AliceR13.zip", "/out:$target", 'Program.cs', 'SteamDiscovery.cs', 'ModInstaller.cs')
Push-Location -LiteralPath $PSScriptRoot
try {
    & $compiler @compilerArgs
    if ($LASTEXITCODE -ne 0) { throw "C# compilation failed ($LASTEXITCODE)." }
} finally { Pop-Location }
Get-Item -LiteralPath $target
