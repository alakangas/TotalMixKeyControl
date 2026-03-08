param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [switch]$Upload
)

$ErrorActionPreference = "Stop"

$publishDir = ".\publish"
$releasesDir = ".\Releases"
$packId = "TotalMixKeyControl"
$mainExe = "TotalMixKeyControl.exe"
$framework = "net8.0-x64-desktop"
$repoUrl = "https://github.com/alakangas/TotalMixKeyControl"
$iconPath = ".\icon.ico"

Write-Host "=== Publishing $packId v$Version ===" -ForegroundColor Cyan
dotnet publish -c Release -r win-x64 --self-contained false -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Write-Host "`n=== Downloading previous release for delta generation ===" -ForegroundColor Cyan
$ghToken = gh auth token
vpk download github --repoUrl $repoUrl --token $ghToken -o $releasesDir
# Non-fatal if no previous release exists yet

Write-Host "`n=== Packing Velopack release ===" -ForegroundColor Cyan
vpk pack `
    --packId $packId `
    --packVersion $Version `
    --packDir $publishDir `
    --mainExe $mainExe `
    --framework $framework `
    --icon $iconPath `
    --outputDir $releasesDir
if ($LASTEXITCODE -ne 0) { throw "vpk pack failed" }

Write-Host "`n=== Done. Release artifacts are in $releasesDir ===" -ForegroundColor Green

if ($Upload) {
    Write-Host "`n=== Uploading to GitHub Releases ===" -ForegroundColor Cyan
    $ghToken = gh auth token
    if ($LASTEXITCODE -ne 0) { throw "Failed to get GitHub token. Run 'gh auth login' first." }
    vpk upload github `
        --repoUrl $repoUrl `
        --publish `
        --releaseName "$packId v$Version" `
        --tag "v$Version" `
        --token $ghToken `
        -o $releasesDir
    if ($LASTEXITCODE -ne 0) { throw "vpk upload failed" }
    Write-Host "`n=== Uploaded to GitHub Releases ===" -ForegroundColor Green
}
else {
    Write-Host "`nTo upload to GitHub Releases, re-run with -Upload:" -ForegroundColor Yellow
    Write-Host "  .\build.ps1 -Version $Version -Upload"
}
