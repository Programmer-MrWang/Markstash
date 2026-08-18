param(
    [string] $AndroidSdkDirectory = "$env:LOCALAPPDATA\Android\Sdk",
    [string] $JavaSdkDirectory = $env:JAVA_HOME
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$androidRoot = Join-Path $repoRoot "android"
$apkPath = Join-Path $androidRoot "app\build\outputs\apk\debug\app-debug.apk"

if ([string]::IsNullOrWhiteSpace($JavaSdkDirectory)) {
    $JavaSdkDirectory = Get-ChildItem "C:\Program Files\Microsoft" -Directory -Filter "jdk-21*" |
        Sort-Object Name -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}

if ([string]::IsNullOrWhiteSpace($JavaSdkDirectory) -or
    -not (Test-Path (Join-Path $JavaSdkDirectory "bin\java.exe"))) {
    throw "OpenJDK 21 is required. Install Microsoft.OpenJDK.21 or pass -JavaSdkDirectory."
}

if (-not (Test-Path $AndroidSdkDirectory)) {
    throw "Android SDK was not found at '$AndroidSdkDirectory'."
}

$env:JAVA_HOME = $JavaSdkDirectory
$env:ANDROID_HOME = $AndroidSdkDirectory
$env:ANDROID_SDK_ROOT = $AndroidSdkDirectory
$env:PATH = "$(Join-Path $JavaSdkDirectory 'bin');$env:PATH"
if ([string]::IsNullOrWhiteSpace($env:GRADLE_USER_HOME) -or
    -not (Test-Path $env:GRADLE_USER_HOME)) {
    $env:GRADLE_USER_HOME = Join-Path $env:USERPROFILE ".gradle"
}

Push-Location $androidRoot
try {
    & .\gradlew.bat :app:assembleDebug '-Pandroid.injected.testOnly=false' --no-configuration-cache
    if ($LASTEXITCODE -ne 0) {
        throw "The native Android build failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

& (Join-Path $PSScriptRoot "assert-android-apk-installable.ps1") `
    -ApkPath $apkPath `
    -AndroidSdkDirectory $AndroidSdkDirectory

Write-Output "Android SDK: $AndroidSdkDirectory"
Write-Output "Java SDK:    $JavaSdkDirectory"
Write-Output "APK:         $apkPath"
