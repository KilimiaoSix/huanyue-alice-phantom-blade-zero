[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$PackagePath,
    [Parameter(Mandatory)] [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$expectedHash = '624bf932fcd8a18519e8492fa27607f58fab35a0fd59ed87fe335023eec49220'
$package = (Resolve-Path -LiteralPath $PackagePath).Path
$target = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
if ($target -eq $package -or (Test-Path -LiteralPath $target -PathType Container)) {
    throw 'Choose a distinct file path for the installer output.'
}
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'The .NET Framework compiler is required.' }

$tempRoot = (Resolve-Path -LiteralPath ([IO.Path]::GetTempPath())).Path
$stageName = 'HuanyueAliceBuild-' + [guid]::NewGuid().ToString('N')
$stage = Join-Path $tempRoot $stageName
New-Item -ItemType Directory -Path $stage | Out-Null
try {
    $stagedMod = Join-Path $stage 'AliceR13.zip'
    Copy-Item -LiteralPath $package -Destination $stagedMod
    if ((Get-FileHash -Algorithm SHA256 -LiteralPath $stagedMod).Hash -ne $expectedHash) {
        throw 'The package is not the validated R13 ZIP.'
    }

    $compilerArgs = @('/nologo', '/codepage:65001', '/warnaserror', '/optimize+', '/target:winexe', '/platform:anycpu',
        '/r:System.IO.Compression.dll', '/r:System.Drawing.dll', '/r:System.Windows.Forms.dll',
        '/resource:AliceR13.zip,AliceR13.zip', '/out:Installer.exe',
        (Join-Path $PSScriptRoot 'Program.cs'), (Join-Path $PSScriptRoot 'SteamDiscovery.cs'),
        (Join-Path $PSScriptRoot 'ModInstaller.cs'), (Join-Path $PSScriptRoot 'PinnedLoaderManifest.cs'),
        (Join-Path $PSScriptRoot 'LoaderPackage.cs'))
    Push-Location -LiteralPath $stage
    try {
        & $compiler @compilerArgs
        if ($LASTEXITCODE -ne 0) { throw "C# compilation failed ($LASTEXITCODE)." }
    } finally { Pop-Location }

    New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
    Move-Item -LiteralPath (Join-Path $stage 'Installer.exe') -Destination $target -Force
} finally {
    $resolvedStage = (Resolve-Path -LiteralPath $stage).Path
    if ((Split-Path $resolvedStage) -eq $tempRoot -and (Split-Path $resolvedStage -Leaf) -eq $stageName) {
        Remove-Item -LiteralPath $resolvedStage -Recurse -Force
    }
}
Get-Item -LiteralPath $target
