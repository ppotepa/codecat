param(
    [string]$Version = "0.34",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
if ($PSVersionTable.PSVersion.Major -ge 7) {
    $PSNativeCommandUseErrorActionPreference = $true
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "src/Codecat.Cli/Codecat.Cli.csproj"
$releaseRoot = Join-Path $repoRoot "artifacts/release"
$publishDir = Join-Path $releaseRoot "codecat-$Version-$Runtime"
$zipPath = Join-Path $releaseRoot "codecat-$Version-$Runtime.zip"
$msiPath = Join-Path $releaseRoot "codecat-$Version-$Runtime.msi"
$wxsPath = Join-Path $repoRoot "installer/wix/Codecat.wxs"

New-Item -ItemType Directory -Force -Path $releaseRoot | Out-Null

if (Test-Path $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

if (Test-Path $msiPath) {
    Remove-Item -LiteralPath $msiPath -Force
}

dotnet restore (Join-Path $repoRoot "Codecat.slnx")
dotnet publish $projectPath `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    -p:Version=$Version `
    -p:PublishAot=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -p:PublishDir="$publishDir/"

Get-ChildItem -LiteralPath $publishDir -Filter "*.pdb" -File | Remove-Item -Force
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force

dotnet tool restore --tool-manifest (Join-Path $repoRoot "dotnet-tools.json")
dotnet tool run wix -- build $wxsPath `
    -arch x64 `
    -d "ProductVersion=$Version" `
    -d "PublishDir=$publishDir" `
    -d "RepoRoot=$repoRoot" `
    -out $msiPath

Get-ChildItem -LiteralPath $releaseRoot -Filter "*.wixpdb" -File | Remove-Item -Force

Write-Host "Release artifacts:"
Write-Host "  $publishDir"
Write-Host "  $zipPath"
Write-Host "  $msiPath"
