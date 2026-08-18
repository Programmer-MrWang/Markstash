using System.IO.Compression;
using Markstash.Application;
using Markstash.Application.Packages;
using Markstash.Application.Resources;
using Markstash.Domain.Resources;
using Markstash.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Markstash.Tests;

[Collection(EnvironmentVariableTestGroup.Name)]
public sealed class ResourceAndPackageIntegrationTests
{
    [Fact]
    public void ResourceIdsRemainOpaqueWhileNewIdsAreUuidStrings()
    {
        var opaque = new ResourceId("device-local-resource-1");

        Assert.Equal("device-local-resource-1", opaque.Value);
        Assert.Contains('-', ResourceId.New().Value);
        Assert.Throws<ArgumentException>(() => new ResourceId("  "));
    }

    [Fact]
    public void ResourceRecordsNormalizeTagsIntoAnImmutableUniqueSet()
    {
        var mutableTags = new List<string> { " reading ", "Reading", "" };
        var resource = CreateResource(tags: mutableTags);
        mutableTags[0] = "changed";

        var tag = Assert.Single(resource.Tags);
        Assert.Equal("reading", tag);
    }

    [Fact]
    public async Task ResourcesPersistAndListUsesSharedFiltersAndPagination()
    {
        using var scope = new DataDirectoryScope();
        using var provider = CreateProvider();
        var service = provider.GetRequiredService<IResourceService>();

        await service.CreateAsync(new(
            ResourceKind.Link,
            "C# reference",
            Source: "https://example.com/csharp",
            Tags: ["reading"],
            IsFavorite: true));
        await service.CreateAsync(new(
            ResourceKind.Note,
            "Private note",
            Description: "draft",
            Tags: ["writing"]));

        var page = await service.ListAsync(new(
            text: "reference",
            kinds: [ResourceKind.Link],
            tags: ["READING"],
            favoritesOnly: true,
            offset: 0,
            limit: 100));

        var match = Assert.Single(page);
        Assert.Equal(ResourceKind.Link, match.Kind);
        Assert.True(File.Exists(Path.Combine(scope.DatabaseDirectory, "resources.json")));
    }

    [Fact]
    public async Task PackageRoundTripRequiresExplicitOverwrite()
    {
        byte[] packageBytes;
        ResourceId resourceId;
        using (var sourceScope = new DataDirectoryScope())
        {
            using var sourceProvider = CreateProvider();
            var sourceService = sourceProvider.GetRequiredService<IResourceService>();
            var created = await sourceService.CreateAsync(new(
                ResourceKind.Note,
                "Portable note",
                Description: "metadata only",
                Tags: ["transfer"]));
            resourceId = created.Id;
            var package = sourceProvider.GetRequiredService<IMarkstashPackageService>();
            await using var output = new MemoryStream();
            var export = await package.ExportAsync(output);
            Assert.Equal(1, export.PackageVersion);
            Assert.Equal(1, export.ResourceCount);
            packageBytes = output.ToArray();
        }

        using var destinationScope = new DataDirectoryScope();
        using var destinationProvider = CreateProvider();
        var destinationPackage = destinationProvider.GetRequiredService<IMarkstashPackageService>();
        await using var input = new MemoryStream(packageBytes);
        var imported = await destinationPackage.ImportAsync(input);
        Assert.Equal(1, imported.AddedCount);
        Assert.Equal(0, imported.UpdatedCount);

        await using var conflictingInput = new MemoryStream(packageBytes);
        await Assert.ThrowsAsync<ResourceConflictException>(() =>
            destinationPackage.ImportAsync(conflictingInput));

        await using var overwriteInput = new MemoryStream(packageBytes);
        var replaced = await destinationPackage.ImportAsync(
            overwriteInput,
            new MarkstashPackageImportOptions(OverwriteExisting: true));
        Assert.Equal(0, replaced.AddedCount);
        Assert.Equal(1, replaced.UpdatedCount);
        Assert.NotNull(await destinationProvider
            .GetRequiredService<IResourceService>()
            .GetAsync(resourceId));
    }

    [Fact]
    public async Task PackageImportRejectsTamperedPayloadAndUnsafePaths()
    {
        byte[] packageBytes;
        using (var sourceScope = new DataDirectoryScope())
        {
            using var sourceProvider = CreateProvider();
            await sourceProvider.GetRequiredService<IResourceService>().CreateAsync(new(
                ResourceKind.File,
                "A file",
                Source: "C:/data/file.txt"));
            var package = sourceProvider.GetRequiredService<IMarkstashPackageService>();
            await using var output = new MemoryStream();
            await package.ExportAsync(output);
            packageBytes = output.ToArray();
        }

        using var destinationScope = new DataDirectoryScope();
        using var destinationProvider = CreateProvider();
        var destinationPackage = destinationProvider.GetRequiredService<IMarkstashPackageService>();
        var tampered = RewriteZipEntry(packageBytes, "resources.json", "{}"u8.ToArray());
        await using var tamperedInput = new MemoryStream(tampered);
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            destinationPackage.ImportAsync(tamperedInput));

        using var unsafeArchive = new MemoryStream();
        using (var archive = new ZipArchive(unsafeArchive, ZipArchiveMode.Create, leaveOpen: true))
        {
            archive.CreateEntry("../manifest.json");
        }

        unsafeArchive.Position = 0;
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            destinationPackage.ImportAsync(unsafeArchive));
    }

    private static ResourceRecord CreateResource(IEnumerable<string>? tags = null) => new(
        new ResourceId("resource-1"),
        ResourceKind.Note,
        "Example",
        null,
        null,
        tags,
        false,
        DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow,
        null);

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddMarkstashApplication();
        services.AddMarkstashInfrastructure();
        return services.BuildServiceProvider();
    }

    private static byte[] RewriteZipEntry(byte[] source, string path, byte[] replacement)
    {
        using var input = new MemoryStream(source);
        using var original = new ZipArchive(input, ZipArchiveMode.Read);
        using var output = new MemoryStream();
        using (var rewritten = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in original.Entries)
            {
                var copy = rewritten.CreateEntry(entry.FullName);
                using var target = copy.Open();
                if (entry.FullName == path)
                {
                    target.Write(replacement);
                    continue;
                }

                using var sourceStream = entry.Open();
                sourceStream.CopyTo(target);
            }
        }

        return output.ToArray();
    }

    private sealed class DataDirectoryScope : IDisposable
    {
        private const string VariableName = "MARKSTASH_DATA_DIR";
        private readonly string? _previousValue;

        public DataDirectoryScope()
        {
            RootDirectory = Path.Combine(
                Path.GetTempPath(),
                "Markstash.Tests",
                Guid.NewGuid().ToString("N"));
            _previousValue = Environment.GetEnvironmentVariable(VariableName);
            Environment.SetEnvironmentVariable(VariableName, RootDirectory);
        }

        public string RootDirectory { get; }

        public string DatabaseDirectory => Path.Combine(RootDirectory, "Database");

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(VariableName, _previousValue);
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
        }
    }
}
