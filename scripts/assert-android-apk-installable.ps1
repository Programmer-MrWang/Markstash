param(
    [Parameter(Mandatory = $true)]
    [string] $ApkPath,
    [string] $AndroidSdkDirectory = $env:ANDROID_SDK_ROOT
)

$ErrorActionPreference = "Stop"
$resolvedApkPath = (Resolve-Path $ApkPath).Path

if ([string]::IsNullOrWhiteSpace($AndroidSdkDirectory)) {
    $AndroidSdkDirectory = $env:ANDROID_HOME
}
if ([string]::IsNullOrWhiteSpace($AndroidSdkDirectory) -and $env:LOCALAPPDATA) {
    $AndroidSdkDirectory = Join-Path $env:LOCALAPPDATA "Android\Sdk"
}
if ([string]::IsNullOrWhiteSpace($AndroidSdkDirectory) -or -not (Test-Path $AndroidSdkDirectory)) {
    throw "Android SDK was not found. Pass -AndroidSdkDirectory or set ANDROID_SDK_ROOT."
}

$buildToolsDirectory = Get-ChildItem (Join-Path $AndroidSdkDirectory "build-tools") -Directory |
    Sort-Object Name -Descending |
    Select-Object -First 1
if ($null -eq $buildToolsDirectory) {
    throw "Android SDK build-tools are required to inspect the APK."
}

$aapt2Name = if ($env:OS -eq "Windows_NT") { "aapt2.exe" } else { "aapt2" }
$aapt2Path = Join-Path $buildToolsDirectory.FullName $aapt2Name
if (-not (Test-Path $aapt2Path)) {
    throw "aapt2 was not found at '$aapt2Path'."
}

$manifestTree = & $aapt2Path dump xmltree $resolvedApkPath --file AndroidManifest.xml
if ($LASTEXITCODE -ne 0) {
    throw "Could not inspect AndroidManifest.xml in '$resolvedApkPath'."
}
if ($manifestTree -match 'android:testOnly.*=true') {
    throw "The APK is marked android:testOnly=true and cannot be installed from a file manager."
}

Write-Output "Verified installable APK manifest: $resolvedApkPath"
