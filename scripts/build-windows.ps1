$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Split-Path -Parent $ScriptDir
$UnityVersion = "6000.3.10f1"
$AppName = "VRTApp-Trolley"

$UnityCandidates = @(
    "C:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe",
    "C:\Program Files (x86)\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe"
)
$UnityCmd = $UnityCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $UnityCmd) {
    Write-Error "Unity $UnityVersion not found in expected locations"
    exit 1
}

$BuildDir = Join-Path $ProjectDir "Build\$AppName-win"
$BuildExe = Join-Path $BuildDir "$AppName.exe"
$ZipPath  = Join-Path $ProjectDir "Build\$AppName-win.zip"
$LogPath  = Join-Path $ProjectDir "Build\buildlog-win.txt"

New-Item -ItemType Directory -Force -Path $BuildDir | Out-Null

Write-Host "Building Windows player..."
Write-Host "  Project: $ProjectDir"
Write-Host "  Output:  $BuildExe"
Write-Host "  Log:     $LogPath"

$proc = Start-Process -FilePath $UnityCmd `
    -ArgumentList @("-batchmode", "-projectPath", $ProjectDir, "-buildWindows64Player", $BuildExe, "-quit", "-logfile", $LogPath) `
    -Wait -PassThru -NoNewWindow

if ($proc.ExitCode -ne 0) {
    Write-Host "============= Unity build failed ============="
    Get-Content $LogPath
    Write-Host "=============================================="
    exit 1
}

Write-Host "Zipping to $ZipPath..."
if (Test-Path $ZipPath) { Remove-Item $ZipPath }
Compress-Archive -Path $BuildDir -DestinationPath $ZipPath
Write-Host "Done: $ZipPath"
