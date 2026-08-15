param(
    [string] $AndroidSdkDirectory = "$env:LOCALAPPDATA\Android\Sdk",
    [string] $JavaSdkDirectory = "$env:LOCALAPPDATA\Microsoft\Jdk\17"
)

$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "..\src\Markstash.Android\Markstash.Android.csproj"
$nugetConfig = Join-Path $PSScriptRoot "..\NuGet.Config"

dotnet workload install android
dotnet restore $project --configfile $nugetConfig
dotnet build $project `
    --framework net10.0-android `
    --target InstallAndroidDependencies `
    --no-restore `
    --property:AndroidSdkDirectory=$AndroidSdkDirectory `
    --property:JavaSdkDirectory=$JavaSdkDirectory `
    --property:AcceptAndroidSDKLicenses=true

[Environment]::SetEnvironmentVariable("ANDROID_HOME", $AndroidSdkDirectory, "User")
[Environment]::SetEnvironmentVariable("ANDROID_SDK_ROOT", $AndroidSdkDirectory, "User")
[Environment]::SetEnvironmentVariable("JAVA_HOME", $JavaSdkDirectory, "User")

Write-Output "Android SDK: $AndroidSdkDirectory"
Write-Output "Java SDK:    $JavaSdkDirectory"
Write-Output "Open a new terminal before building so the user environment is refreshed."
