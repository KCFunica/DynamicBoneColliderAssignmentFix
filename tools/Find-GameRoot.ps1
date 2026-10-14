function Find-GameRoot([string]$RequestedRoot) {
    if ($RequestedRoot) {
        $resolved = (Resolve-Path -LiteralPath $RequestedRoot -ErrorAction Stop).Path
        if (-not (Test-Path -LiteralPath (Join-Path $resolved 'BepInEx\plugins\KKPE.dll'))) {
            throw "KKPE.dll was not found in $resolved"
        }
        return $resolved
    }
    if ($env:KOIKATSU_ROOT) { return Find-GameRoot $env:KOIKATSU_ROOT }
    $candidates = New-Object 'System.Collections.Generic.List[string]'
    $ancestor = Get-Item $PSScriptRoot
    while ($ancestor) { $candidates.Add($ancestor.FullName); $ancestor = $ancestor.Parent }
    foreach ($drive in Get-PSDrive -PSProvider FileSystem) {
        if ($drive.Name -eq 'Temp') { continue }
        foreach ($directory in Get-ChildItem -LiteralPath $drive.Root -Directory -ErrorAction SilentlyContinue) {
            if ($directory.Name -match 'Windows|Program Files|ProgramData|\$|Recovery|Users|System Volume') { continue }
            $candidates.Add($directory.FullName)
            foreach ($child in Get-ChildItem -LiteralPath $directory.FullName -Directory -ErrorAction SilentlyContinue) {
                $candidates.Add($child.FullName)
            }
        }
    }
    $matches = foreach ($path in ($candidates | Select-Object -Unique)) {
        if ((Test-Path -LiteralPath (Join-Path $path 'CharaStudio.exe')) -and
            (Test-Path -LiteralPath (Join-Path $path 'BepInEx\plugins\KKPE.dll'))) {
            $log = Get-Item -LiteralPath (Join-Path $path 'output_log.txt') -ErrorAction SilentlyContinue
            [pscustomobject]@{ Path = $path; LastStudioLog = if ($log) { $log.LastWriteTimeUtc } else { [datetime]::MinValue } }
        }
    }
    $best = $matches | Sort-Object LastStudioLog -Descending | Select-Object -First 1
    if (-not $best) { throw 'Game folder not found. Supply -GameRoot or KOIKATSU_ROOT.' }
    return $best.Path
}
