param([Parameter(Mandatory = $true)][string]$GameRoot, [string]$Target = 'KK', [string]$CoreRoot, [string]$PoseEditorPath, [string]$ManagedRoot, [string]$PluginPath)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v3.5\csc.exe'
if ($Target -eq 'KKS') { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not $CoreRoot) { $CoreRoot = Join-Path $GameRoot 'BepInEx\core' }
if (-not $ManagedRoot) { $ManagedRoot = Join-Path $GameRoot 'CharaStudio_Data\Managed' }
if (-not $PoseEditorPath) { $PoseEditorPath = Join-Path $GameRoot ('BepInEx\plugins\' + $Target + 'PE.dll') }
if (-not $PluginPath) {
    $release = Join-Path $PSScriptRoot '..\Release'
    if ($Target -eq 'KKS') { $release = Join-Path $release 'KKS' }
    $PluginPath = Join-Path $release ($Target + '_DynamicBoneColliderAssignmentFix.dll')
}
$out = Join-Path $PSScriptRoot ('bin\' + $Target)
New-Item -ItemType Directory -Force -Path $out | Out-Null
$exe = Join-Path $out 'RegressionTests.exe'
$compileArgs = @('/nologo','/target:exe','/optimize+','/warnaserror+',('/out:' + $exe),
    ('/reference:' + (Join-Path $CoreRoot '0Harmony.dll')), ('/reference:' + (Join-Path $ManagedRoot 'UnityEngine.dll')))
if ($Target -eq 'KKS') { $compileArgs += '/reference:' + (Join-Path $ManagedRoot 'UnityEngine.CoreModule.dll') }
$compileArgs += @(Get-ChildItem (Join-Path $PSScriptRoot '..\src') -Filter '*.cs' | Where-Object Name -ne 'Plugin.cs' | ForEach-Object FullName)
$compileArgs += Join-Path $PSScriptRoot 'RegressionTests.cs'
& $compiler @compileArgs
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
# The game's MonoMod build targets Unity Mono. Its reflection helper fails on
# Microsoft's desktop CLR 2 before our patch runs; use desktop CLR 4 for smoke tests.
'<configuration><startup useLegacyV2RuntimeActivationPolicy="true"><supportedRuntime version="v4.0" /></startup></configuration>' |
    Set-Content -Encoding UTF8 -LiteralPath ($exe + '.config')
& $exe $CoreRoot $GameRoot $PoseEditorPath $ManagedRoot $PluginPath
if ($LASTEXITCODE -ne 0) { throw 'Regression tests failed.' }
