# Markstash Windows

This directory is the standalone Windows application root for Markstash.

## Structure

- `Markstash.slnx`: Windows solution entry point.
- `src/Markstash.Domain`: domain models.
- `src/Markstash.Application`: use cases and platform ports.
- `src/Markstash.Infrastructure`: local persistence, diagnostics, logging, and OS adapters.
- `src/Markstash.App`: Avalonia shell, features, navigation, and composition root.
- `src/Markstash.Desktop`: Windows executable host.
- `tests/Markstash.Tests`: unit, architecture, and integration tests.

## Build

Run from this directory:

```powershell
dotnet restore src/Markstash.Desktop/Markstash.Desktop.csproj --configfile NuGet.Config
dotnet build src/Markstash.Desktop/Markstash.Desktop.csproj -c Release
dotnet test tests/Markstash.Tests/Markstash.Tests.csproj -c Release
```

The Windows client is local-only. UI features call Application use cases directly; there is no
HTTP backend process.
