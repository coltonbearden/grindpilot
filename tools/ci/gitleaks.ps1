# Runs gitleaks (MIT) over the repo's whole history. The newest release is downloaded each run and checked against the
# checksum file published with it, so nothing here is held at an old version.
# gitleaks-action is not used: its license changed from MIT at v2.
$ErrorActionPreference = 'Stop'
$headers = @{ 'User-Agent' = 'grindpilot-ci' }
if ($env:GH_TOKEN) { $headers.Authorization = "Bearer $env:GH_TOKEN" }

$release = Invoke-RestMethod -Uri 'https://api.github.com/repos/gitleaks/gitleaks/releases/latest' -Headers $headers
$version = $release.tag_name.TrimStart('v')
$zipName = "gitleaks_${version}_windows_x64.zip"
$sumName = "gitleaks_${version}_checksums.txt"
$zipUrl = ($release.assets | Where-Object name -eq $zipName).browser_download_url
$sumUrl = ($release.assets | Where-Object name -eq $sumName).browser_download_url
if (-not $zipUrl -or -not $sumUrl) { throw "gitleaks $version has no $zipName or $sumName" }

$dir = Join-Path ($env:RUNNER_TEMP ?? [IO.Path]::GetTempPath()) "gitleaks-$version"
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$zip = Join-Path $dir $zipName
$sums = Join-Path $dir $sumName
Invoke-WebRequest -Uri $zipUrl -OutFile $zip -Headers $headers
Invoke-WebRequest -Uri $sumUrl -OutFile $sums -Headers $headers

$expected = (Get-Content $sums | Where-Object { $_ -match "\s$([regex]::Escape($zipName))$" }) -split '\s+' | Select-Object -First 1
$actual = (Get-FileHash -Algorithm SHA256 $zip).Hash.ToLowerInvariant()
if (-not $expected -or $expected.ToLowerInvariant() -ne $actual) { throw "gitleaks $version checksum mismatch" }
Expand-Archive -Path $zip -DestinationPath $dir -Force

Write-Host "gitleaks $version (checksum verified)"
& (Join-Path $dir 'gitleaks.exe') git --redact --no-banner --verbose .
exit $LASTEXITCODE
