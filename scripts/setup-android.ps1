param(
    [string] $AndroidSdkDirectory = "$env:LOCALAPPDATA\Android\Sdk",
    [string] $JavaSdkDirectory = $env:JAVA_HOME
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$androidRoot = Join-Path $repoRoot "android-native"

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

Push-Location $androidRoot
try {
    & .\gradlew.bat :app:assembleDebug --no-configuration-cache
    if ($LASTEXITCODE -ne 0) {
        throw "The native Android build failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

Write-Output "Android SDK: $AndroidSdkDirectory"
Write-Output "Java SDK:    $JavaSdkDirectory"
Write-Output "APK:         $androidRoot\app\build\outputs\apk\debug\app-debug.apk"
