param([string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    $version = (Get-Content -LiteralPath VERSION -Raw).Trim()
    if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'VERSION must use major.minor.patch, for example 1.0.1.' }
    $manifest = Get-Content -LiteralPath manifest.json -Raw | ConvertFrom-Json
    if ($manifest.Version -ne $version) { throw "manifest version $($manifest.Version) does not match VERSION $version." }
    if ($manifest.Author -ne 'ayushua' -or $manifest.UniqueID -ne 'YogurtLid' -or @($manifest.UpdateKeys) -notcontains 'Nexus:55704') {
        throw 'Unexpected release identity: check Author, UniqueID and UpdateKeys.'
    }
    & $Dotnet build YogurtLid.csproj -c Release -p:EnableModDeploy=false
    if ($LASTEXITCODE -ne 0) { throw 'Build failed; no package was created.' }
    $dllPath = Join-Path $projectRoot 'bin/Release/YogurtLid.dll'
    $assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($dllPath).Version
    if ($assemblyVersion.ToString(3) -ne $version) { throw "DLL version $assemblyVersion does not match $version." }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $releaseDir = Join-Path $projectRoot 'releases'
    New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
    $zipPath = Join-Path $releaseDir "YogurtLid $version.zip"
    $tempPath = Join-Path $releaseDir "YogurtLid $version.building.zip"
    $stream = [IO.File]::Open($tempPath, [IO.FileMode]::Create)
    $archive = [IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create)
    try {
        $files = @('manifest.json', 'README.md', 'NOTICE.md', 'bin/Release/YogurtLid.dll')
        foreach ($file in $files) {
            $entry = 'YogurtLid/' + [IO.Path]::GetFileName($file)
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Join-Path $projectRoot $file), $entry, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    } finally { $archive.Dispose(); $stream.Dispose() }
    $verify = [IO.Compression.ZipFile]::OpenRead($tempPath)
    try {
        if ($verify.Entries.Count -ne 4) { throw 'Unexpected package file count.' }
        $reader = [IO.StreamReader]::new($verify.GetEntry('YogurtLid/manifest.json').Open())
        try { $packed = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
        if ($packed.Version -ne $version -or $packed.Author -ne $manifest.Author) { throw 'Packed manifest mismatch.' }
        $packedStream = $verify.GetEntry('YogurtLid/YogurtLid.dll').Open()
        $hasher = [Security.Cryptography.SHA256]::Create()
        try { $packedHash = [BitConverter]::ToString($hasher.ComputeHash($packedStream)).Replace('-','') }
        finally { $packedStream.Dispose(); $hasher.Dispose() }
        if ($packedHash -ne (Get-FileHash -LiteralPath $dllPath -Algorithm SHA256).Hash) { throw 'Packed DLL hash mismatch.' }
    } finally { $verify.Dispose() }
    Move-Item -LiteralPath $tempPath -Destination $zipPath -Force
    $packageHash = Get-FileHash -LiteralPath $zipPath -Algorithm SHA256
    Write-Output "Verified package: $zipPath"
    Write-Output "Version: $version (manifest, project and DLL)"
    Write-Output "SHA256: $($packageHash.Hash)"
} finally { Pop-Location }
