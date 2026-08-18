param(
    [string] $AndroidSdkDirectory = "$env:LOCALAPPDATA\Android\Sdk",
    [string] $JavaSdkDirectory = $env:JAVA_HOME,
    [string] $DeviceId,
    [switch] $Install,
    [switch] $Launch,
    [switch] $Logcat
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$androidRoot = Join-Path $repoRoot "android"
$apkPath = Join-Path $androidRoot "app\build\outputs\apk\debug\app-debug.apk"
$installableApkPath = Join-Path $repoRoot "artifacts\Markstash-android-debug-installable.apk"
$packageName = "io.github.programmermrwang.markstash.debug"

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

$adbPath = Join-Path $AndroidSdkDirectory "platform-tools\adb.exe"
if (($Install -or $Launch -or $Logcat) -and -not (Test-Path $adbPath)) {
    throw "adb was not found at '$adbPath'. Install Android SDK platform-tools."
}

$env:JAVA_HOME = $JavaSdkDirectory
$env:ANDROID_HOME = $AndroidSdkDirectory
$env:ANDROID_SDK_ROOT = $AndroidSdkDirectory
$env:PATH = "$(Join-Path $JavaSdkDirectory 'bin');$(Join-Path $AndroidSdkDirectory 'platform-tools');$env:PATH"
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

New-Item -ItemType Directory -Force (Split-Path $installableApkPath) | Out-Null
Copy-Item $apkPath $installableApkPath -Force
Write-Output "Installable APK: $installableApkPath"

if (-not ($Install -or $Launch -or $Logcat)) {
    exit 0
}

$deviceLines = @(& $adbPath devices | Select-Object -Skip 1 | Where-Object { $_ -match "\tdevice$" })
if ([string]::IsNullOrWhiteSpace($DeviceId)) {
    if ($deviceLines.Count -eq 1) {
        $DeviceId = ($deviceLines[0] -split "\t")[0]
    } elseif ($deviceLines.Count -eq 0) {
        throw "No Android device or emulator is connected. Start one in Android Studio or run adb connect first."
    } else {
        throw "Multiple Android devices are connected. Re-run with -DeviceId <serial>."
    }
}

if ($Install -or $Launch) {
    & $adbPath -s $DeviceId install -r $installableApkPath
    if ($LASTEXITCODE -ne 0) {
        throw "APK installation failed on device '$DeviceId'."
    }
}

if ($Launch -or $Logcat) {
    & $adbPath -s $DeviceId shell am force-stop $packageName
    & $adbPath -s $DeviceId shell monkey -p $packageName 1 | Out-Null
}

if ($Logcat) {
    & $adbPath -s $DeviceId logcat --pid=$((& $adbPath -s $DeviceId shell pidof $packageName).Trim())
}
