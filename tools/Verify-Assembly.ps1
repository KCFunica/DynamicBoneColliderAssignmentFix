param([Parameter(Mandatory = $true)][string]$GameRoot, [string]$Target = 'KK', [string]$CoreRoot, [string]$PoseEditorPath, [string]$PluginPath)
$ErrorActionPreference = 'Stop'
if (-not $CoreRoot) { $CoreRoot = Join-Path $GameRoot 'BepInEx\core' }
[Reflection.Assembly]::LoadFrom((Join-Path $CoreRoot 'Mono.Cecil.dll')) | Out-Null
$path = Join-Path $PSScriptRoot '..\Release\KK_DynamicBoneColliderAssignmentFix.dll'
if ($PluginPath) { $path = $PluginPath }
$plugin = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path $path).Path)
try {
    $runtime = if ($Target -eq 'KKS') { 'v4.*' } else { 'v2.*' }
    if ($plugin.MainModule.RuntimeVersion -notlike $runtime) { throw 'Unexpected target CLR.' }
    $allowed = @('mscorlib', 'System', 'System.Core', 'BepInEx', '0Harmony', 'UnityEngine', 'UnityEngine.CoreModule')
    foreach ($reference in $plugin.MainModule.AssemblyReferences) {
        if ($reference.Name -notin $allowed) { throw "Unexpected DLL dependency: $reference" }
    }
    $type = $plugin.MainModule.Types | Where-Object FullName -eq 'DynamicBoneColliderAssignmentFix.Plugin'
    foreach ($attribute in @('BepInEx.BepInPlugin', 'BepInEx.BepInProcess', 'BepInEx.BepInDependency')) {
        if (-not ($type.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq $attribute })) { throw "Missing $attribute" }
    }
    if ($type.Methods.Name -contains 'Update') { throw 'Unexpected polling Update.' }
    Write-Host ('Artifact verified: CLR ' + $plugin.MainModule.RuntimeVersion + '; dependencies=' + ($plugin.MainModule.AssemblyReferences.Name -join ', '))
} finally { $plugin.Dispose() }
if (-not $PoseEditorPath) { $PoseEditorPath = Join-Path $GameRoot ('BepInEx\plugins\' + $Target + 'PE.dll') }
$kkpe = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($PoseEditorPath)
try {
    $types = @{}
    foreach ($type in $kkpe.MainModule.Types) { $types[$type.FullName] = $type }
    $requirements = @{
        'HSPE.PoseController' = @('_dynamicBonesEditor')
        'HSPE.CharaPoseController' = @('_boobsEditor')
        'HSPE.AMModules.AdvancedModeModule' = @('_parent')
        'HSPE.AMModules.DynamicBonesEditor' = @('_dynamicBones')
        'HSPE.AMModules.BoobsEditor' = @('_dynamicBones')
        'HSPE.AMModules.CollidersEditor' = @('_loneColliders', '_dirtyColliders', '_addNewDynamicBonesAsDefault')
    }
    foreach ($name in $requirements.Keys) {
        if (-not $types.ContainsKey($name)) { throw "Missing type: $name" }
        foreach ($field in $requirements[$name]) {
            if ($types[$name].Fields.Name -notcontains $field) { throw "Missing $name.$field" }
        }
    }
    $refresh = $types['HSPE.AMModules.DynamicBonesEditor'].Methods | Where-Object Name -eq 'RefreshDynamicBoneList'
    if (@($refresh).Count -ne 1 -or $refresh.Parameters.Count -ne 0) { throw 'Refresh signature changed.' }
    $policyReads = @($refresh.Body.Instructions | Where-Object { $_.OpCode.Name -eq 'ldfld' -and $_.Operand.Name -eq '_addNewDynamicBonesAsDefault' })
    if ($policyReads.Count -lt 1) { throw 'Refresh no longer reads the known default flag.' }
    $setter = $types['HSPE.AMModules.CollidersEditor'].Methods | Where-Object Name -eq 'SetIgnoreDynamicBone'
    $signature = ($setter.Parameters | ForEach-Object { $_.ParameterType.FullName }) -join ','
    if ($signature -ne 'DynamicBoneCollider,HSPE.PoseController,System.Object,System.Boolean') { throw 'Assignment API signature changed.' }
    Write-Host 'Installed KKPE metadata and policy-read callsite verified.'
} finally { $kkpe.Dispose() }
