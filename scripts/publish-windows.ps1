param(
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot "..")
Set-Location $repoRoot

$distRoot = Join-Path $repoRoot "dist"
$packageRoot = Join-Path $distRoot "Fallout.Tools.$Runtime"
$uiOut = Join-Path $packageRoot "ui"
$cliOut = Join-Path $packageRoot "cli"
$zipPath = Join-Path $distRoot "Fallout.Tools.$Runtime.zip"

Write-Host "== Fallout Tools Windows publish ==" -ForegroundColor Yellow
Write-Host "Repository: $repoRoot"
Write-Host "Runtime:    $Runtime"
Write-Host "Config:     $Configuration"

if (Test-Path $packageRoot) {
    Remove-Item $packageRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $uiOut | Out-Null
New-Item -ItemType Directory -Force -Path $cliOut | Out-Null

Write-Host "\n== Restore ==" -ForegroundColor Yellow
dotnet restore

Write-Host "\n== Build ==" -ForegroundColor Yellow
dotnet build -c $Configuration --no-restore

Write-Host "\n== Test ==" -ForegroundColor Yellow
dotnet test -c $Configuration --no-build

$commonPublishArgs = @(
    "-c", $Configuration,
    "-r", $Runtime,
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:EnableCompressionInSingleFile=true",
    "-p:PublishTrimmed=false"
)

if ($FrameworkDependent) {
    $commonPublishArgs += @("--self-contained", "false")
    $zipPath = Join-Path $distRoot "Fallout.Tools.$Runtime.framework-dependent.zip"
} else {
    $commonPublishArgs += @("--self-contained", "true")
}

Write-Host "\n== Publish CLI ==" -ForegroundColor Yellow
dotnet publish "src/Fallout.Tools.CLI/Fallout.Tools.CLI.csproj" @commonPublishArgs -o $cliOut

Write-Host "\n== Publish UI ==" -ForegroundColor Yellow
dotnet publish "src/Fallout.Tools.UI/Fallout.Tools.UI.csproj" @commonPublishArgs -o $uiOut

$readmePath = Join-Path $packageRoot "README.txt"
@"
Fallout 1/2 Tools - Windows build

Folders:
- ui  : visual editor executable
- cli : command-line tools

Recommended UI workflow:
1. Open ACT palette.
2. Open static FRM or clean BMP/PNG.
3. Open AAF font if adding text.
4. Add erase patches and translated text.
5. Export BMP 8-bit or edited FRM.

Important:
- Do not overwrite original game files directly.
- Keep original FRM/AAF/ACT assets outside the repository.
- Test edited FRM files in a copy of your mod/game folder first.
"@ | Set-Content $readmePath -Encoding UTF8

if (Test-Path "README.md") {
    Copy-Item "README.md" (Join-Path $packageRoot "PROJECT_README.md") -Force
}

if (Test-Path "docs/RELEASE_CHECKLIST.md") {
    New-Item -ItemType Directory -Force -Path (Join-Path $packageRoot "docs") | Out-Null
    Copy-Item "docs/RELEASE_CHECKLIST.md" (Join-Path $packageRoot "docs/RELEASE_CHECKLIST.md") -Force
}

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Write-Host "\n== Create ZIP ==" -ForegroundColor Yellow
Compress-Archive -Path (Join-Path $packageRoot "*") -DestinationPath $zipPath -Force

Write-Host "\nDone." -ForegroundColor Green
Write-Host "Package folder: $packageRoot"
Write-Host "ZIP:            $zipPath"
