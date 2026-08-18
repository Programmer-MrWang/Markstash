# `.markstash` Package Format

`.markstash` is the portable, local-first interchange format for explicit backup or manual device
transfer. It is a ZIP archive with a versioned metadata payload. The format is independent of the
Windows JSON database and Android's database schema.

## Version 1 layout

```text
manifest.json
resources.json
checksums.sha256
attachments/        (reserved; may be an empty directory entry)
```

Version 1 exports metadata only. It does not include note bodies, downloaded files, credentials, or
other attachment bytes. The `attachments/` directory is reserved so a future version can add
content without changing the top-level contract; version 1 rejects attachment files.

## `manifest.json`

```json
{
  "format": "markstash",
  "packageVersion": 1,
  "createdAtUtc": "2026-08-18T00:00:00Z",
  "contentMode": "metadata",
  "resourceCount": 0,
  "attachmentsIncluded": false
}
```

`format`, `packageVersion`, `contentMode`, and `attachmentsIncluded` are compatibility gates.
`resourceCount` must equal the number of records in `resources.json`.

## `resources.json`

```json
{
  "schemaVersion": 1,
  "resources": [
    {
      "id": "2fcb2d96-6b6c-4df3-8b55-cb1c00eae1bf",
      "kind": "Link",
      "title": "Example",
      "source": "https://example.com",
      "description": null,
      "tags": ["reading"],
      "isFavorite": false,
      "createdAtUtc": "2026-08-18T00:00:00Z",
      "updatedAtUtc": "2026-08-18T00:00:00Z",
      "contentHash": null
    }
  ]
}
```

The record fields and validation rules are defined in `docs/resource-model.md`. IDs must be
unique within the package. Unknown future fields may be ignored by a compatible reader, but an
unknown package or document version must not be silently imported.

## `checksums.sha256`

Each non-directory payload entry except `checksums.sha256` has one line:

```text
<64 lowercase hexadecimal SHA-256>  manifest.json
<64 lowercase hexadecimal SHA-256>  resources.json
```

The importer requires an exact one-to-one set of checksum paths and validates every digest before
deserializing or writing any resource.

## Import safety

Readers must:

1. Reject absolute paths, backslashes, drive prefixes, `.` segments, `..` segments, duplicate
   entries, unsupported files, and excessive archive sizes.
2. Require all three files and validate the manifest and resource schema versions.
3. Validate checksums and resource IDs before touching storage.
4. Use `overwriteExisting = false` unless the caller explicitly chooses replacement. A conflict
   fails the batch rather than partially importing records.

Export is explicit and metadata-only by default. A selected resource that no longer exists is an
error; an unselected export includes all resources in deterministic ID order.
