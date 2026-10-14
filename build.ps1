param([string]$GameRoot, [ValidateSet('KK','KKS')][string]$Target = 'KK', [string]$CoreRoot, [string]$PoseEditorPath,
    [ValidateSet('Release')][string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'tools\Find-GameRoot.ps1')
if ($Target -eq 'KK') { $GameRoot = Find-GameRoot $GameRoot }
else {
    if (-not $GameRoot) { throw 'Supply the Sunshine game folder with -GameRoot.' }
    $GameRoot = (Resolve-Path -LiteralPath $GameRoot).Path
}
if (-not $CoreRoot) {
    $CoreRoot = Join-Path $GameRoot 'BepInEx\core'
    if (-not (Test-Path (Join-Path $CoreRoot 'BepInEx.dll'))) {
        $CoreRoot = Join-Path (Find-GameRoot '') 'BepInEx\core'
        Write-Host "Using BepInEx reference-only core: $CoreRoot"
    }
}
$managed = Join-Path $GameRoot 'CharaStudio_Data\Managed'
if ($Target -eq 'KKS' -and -not (Test-Path $managed)) { $managed = Join-Path $GameRoot 'KoikatsuSunshine_Data\Managed' }
if (-not $PoseEditorPath) { $PoseEditorPath = Join-Path $GameRoot ('BepInEx\plugins\' + $Target + 'PE.dll') }
Write-Host "Configuration: $Configuration (optimized, no debug symbols)"
Write-Host "Reference game: $GameRoot"
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v3.5\csc.exe'
if ($Target -eq 'KKS') { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path -LiteralPath $compiler)) { throw "Required Windows .NET C# compiler is not available: $compiler" }
$out = Join-Path $PSScriptRoot 'Release'
if ($Target -eq 'KKS') { $out = Join-Path $out 'KKS' }
New-Item -ItemType Directory -Force -Path $out | Out-Null
$refs = @(
    (Join-Path $CoreRoot 'BepInEx.dll'),
    (Join-Path $CoreRoot '0Harmony.dll'),
    (Join-Path $managed 'UnityEngine.dll')
)
if ($Target -eq 'KKS') { $refs += Join-Path $managed 'UnityEngine.CoreModule.dll' }
$dll = Join-Path $out ($Target + '_DynamicBoneColliderAssignmentFix.dll')
$argsForCompiler = @('/nologo', '/target:library', '/optimize+', '/debug-', '/warnaserror+',
    ('/out:' + $dll))
if ($Target -eq 'KKS') { $argsForCompiler += '/define:KKS' }
foreach ($reference in $refs) {
    if (-not (Test-Path -LiteralPath $reference)) { throw "Missing reference: $reference" }
    $argsForCompiler += '/reference:' + $reference
}
$argsForCompiler += @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object { $_.FullName })
& $compiler @argsForCompiler
if ($LASTEXITCODE -ne 0) { throw "Compilation failed: $LASTEXITCODE" }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'docs\README.txt') -Destination (Join-Path $out 'README.txt')
& (Join-Path $PSScriptRoot 'tools\Verify-Assembly.ps1') -GameRoot $GameRoot -Target $Target -CoreRoot $CoreRoot -PoseEditorPath $PoseEditorPath -PluginPath $dll
& (Join-Path $PSScriptRoot 'tests\Run-Tests.ps1') -GameRoot $GameRoot -Target $Target -CoreRoot $CoreRoot -PoseEditorPath $PoseEditorPath -ManagedRoot $managed
Write-Host "Build and automated checks passed: $out"
