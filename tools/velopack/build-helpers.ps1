# Builds Velopack's Windows helpers (update, setup, stub) from Velopack's own source with no-process-scan.patch
# applied, and puts them into a private copy of the vpk tool, which packs GrindPilot with them. CI runs this; nothing
# is installed. See docs/build.md, "Velopack's helpers".
#
#   ./tools/velopack/build-helpers.ps1 [-Out <folder>]
# Prints the path of the patched vpk.dll on its last line.
param(
    [string]$Out = (Join-Path (Get-Location) '.scratch/velopack'),
    [string]$Repo = (Get-Location).Path
)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

[xml]$props = Get-Content (Join-Path $Repo 'Directory.Packages.props')
$version = ($props.Project.ItemGroup.PackageVersion | Where-Object Include -eq 'Velopack').Version
$source = Get-Content (Join-Path $Repo 'tools/velopack/source.json') -Raw | ConvertFrom-Json
if ($source.version -ne $version) {
    throw "Velopack is $version in Directory.Packages.props, but tools/velopack/source.json names $($source.version). Read the new version's source again before taking it (docs/build.md)."
}
$patch = Join-Path $Repo 'tools/velopack/no-process-scan.patch'
$patchHash = (Get-FileHash -Algorithm SHA256 $patch).Hash.ToLowerInvariant()

$vpkDir = Join-Path $Out "vpk-$version"
$stamp = Join-Path $vpkDir 'patched.json'
if ((Test-Path $stamp) -and ((Get-Content $stamp -Raw | ConvertFrom-Json).patch -eq $patchHash)) {
    Write-Host "Patched vpk $version already built ($stamp)."
    Join-Path $vpkDir 'tools/net10.0/any/vpk.dll'
    return
}

# 1. Velopack's source at the reviewed tag, checked against the reviewed commit.
New-Item -ItemType Directory -Force -Path $Out | Out-Null
$src = Join-Path $Out "src-$version"
if (Test-Path $src) { Remove-Item -Recurse -Force $src }
git -c core.autocrlf=false -c advice.detachedHead=false clone --quiet --depth 1 --branch $source.tag https://github.com/velopack/velopack.git $src
$head = (git -C $src rev-parse HEAD).Trim()
if ($head -ne $source.commit) { throw "Velopack's tag $($source.tag) is at $head, not the reviewed $($source.commit)." }

# 2. The patch, and the version stamp (Velopack's own CI sets it; the source says 0.0.0-local).
git -C $src apply --check $patch
git -C $src apply $patch
$cargoToml = Join-Path $src 'Cargo.toml'
$toml = [IO.File]::ReadAllText($cargoToml)
if (-not $toml.Contains('version = "0.0.0-local"')) { throw 'Cargo.toml has no 0.0.0-local version to stamp.' }
[IO.File]::WriteAllText($cargoToml, $toml.Replace('version = "0.0.0-local"', "version = `"$version`""))

# 3. Build with a static C runtime, as Velopack's own builds do, so the helpers need no VC++ runtime.
$env:RUSTFLAGS = '-C target-feature=+crt-static'
cargo build --release --features windows -p velopack_bins --bin update --bin setup --bin stub --manifest-path $cargoToml
$bin = Join-Path $src 'target/release'

# 4. A private copy of vpk at the same version, with the patched helpers in its vendor folder.
if (Test-Path $vpkDir) { Remove-Item -Recurse -Force $vpkDir }
$nupkg = Join-Path $Out "vpk.$version.nupkg"
Invoke-WebRequest -Uri "https://api.nuget.org/v3-flatcontainer/vpk/$version/vpk.$version.nupkg" -OutFile $nupkg
Expand-Archive -Path $nupkg -DestinationPath $vpkDir -Force
foreach ($name in 'update', 'setup', 'stub') {
    Copy-Item (Join-Path $bin "$name.exe") (Join-Path $vpkDir "vendor/${name}_x64.exe") -Force
}
[ordered]@{
    version = $version
    commit = $head
    patch = $patchHash
    built = (Get-Date).ToString('o')
    helpers = [ordered]@{
        update = (Get-FileHash -Algorithm SHA256 (Join-Path $vpkDir 'vendor/update_x64.exe')).Hash.ToLowerInvariant()
        setup = (Get-FileHash -Algorithm SHA256 (Join-Path $vpkDir 'vendor/setup_x64.exe')).Hash.ToLowerInvariant()
        stub = (Get-FileHash -Algorithm SHA256 (Join-Path $vpkDir 'vendor/stub_x64.exe')).Hash.ToLowerInvariant()
    }
} | ConvertTo-Json | Set-Content -Path $stamp
Write-Host "Built Velopack $version helpers from $head with the patch; vpk is in $vpkDir."
Join-Path $vpkDir 'tools/net10.0/any/vpk.dll'
