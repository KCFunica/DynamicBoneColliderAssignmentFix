param(
    [ValidateSet('KK','KKS')][string]$Target = 'KK',
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$match = [regex]::Match((Get-Content -LiteralPath (Join-Path $project 'src\Plugin.cs') -Raw), 'BepInPlugin\(Guid, "[^"]+", "([0-9.]+)"\)')
if (-not $match.Success) { throw 'Plugin version not found.' }
$version = $match.Groups[1].Value
$license = Join-Path $project 'LICENSE'
if (-not (Test-Path -LiteralPath $license)) {
    throw 'LICENSE is required before distribution.'
}
$release = Join-Path $project 'Release'
if ($Target -eq 'KKS') { $release = Join-Path $release 'KKS' }
$dllName = $Target + '_DynamicBoneColliderAssignmentFix.dll'
$dll = Join-Path $release $dllName
if (-not (Test-Path -LiteralPath $dll)) { throw 'Build the target successfully before packaging.' }
$assembly = [Reflection.AssemblyName]::GetAssemblyName($dll)
if ($assembly.Version.ToString() -ne ($version + '.0')) { throw 'DLL version does not match the source.' }
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $project ('dist\' + $version) }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path
$suffix = if ($Target -eq 'KKS') { '_KKS_unverified' } else { '_KK' }
$zipPath = Join-Path $OutputDirectory ('DynamicBoneColliderAssignmentFix_v' + $version + $suffix + '.zip')
$entries = [ordered]@{
    ('BepInEx/plugins/' + $dllName) = $dll
    'README.txt' = (Join-Path $project 'docs\README.txt')
    'THIRD_PARTY_NOTICES.md' = (Join-Path $project 'THIRD_PARTY_NOTICES.md')
    'RELEASE_NOTES.md' = (Join-Path $project ('docs\RELEASE_NOTES_' + $version + '.md'))
}
$entries['LICENSE'] = $license
foreach ($source in $entries.Values) {
    if (-not (Test-Path -LiteralPath $source)) { throw "Missing distribution file: $source" }
}
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$stream = [IO.File]::Open($zipPath, [IO.FileMode]::Create, [IO.FileAccess]::ReadWrite)
try {
    $zip = New-Object IO.Compression.ZipArchive($stream, [IO.Compression.ZipArchiveMode]::Create, $true)
    try {
        foreach ($name in $entries.Keys) {
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $entries[$name], $name, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    } finally { $zip.Dispose() }
} finally { $stream.Dispose() }
# Verify every entry byte-for-byte against its explicitly listed source.
$zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    if ($zip.Entries.Count -ne $entries.Count) { throw 'Unexpected ZIP entry count.' }
    foreach ($entry in $zip.Entries) {
        if (-not $entries.Contains($entry.FullName)) { throw "Unexpected ZIP entry: $($entry.FullName)" }
        $algorithm = [Security.Cryptography.SHA256]::Create()
        $entryStream = $entry.Open()
        try { $actual = [BitConverter]::ToString($algorithm.ComputeHash($entryStream)).Replace('-', '') }
        finally { $entryStream.Dispose(); $algorithm.Dispose() }
        $expected = (Get-FileHash -LiteralPath $entries[$entry.FullName] -Algorithm SHA256).Hash
        if ($actual -ne $expected) { throw "ZIP hash mismatch: $($entry.FullName)" }
        Write-Host ('Verified ZIP entry: ' + $entry.FullName)
    }
} finally { $zip.Dispose() }
$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
$hash + '  ' + [IO.Path]::GetFileName($zipPath) | Set-Content -LiteralPath ($zipPath + '.sha256') -Encoding ASCII
Write-Host "Package verified: $zipPath"
