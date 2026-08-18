# Resource Model

Markstash is local-first. Windows and Android have separate storage engines, but they use the
same resource vocabulary and observable semantics. This document is the contract to mirror when
adding a new platform implementation.

## Resource

`ResourceId` is an opaque, non-empty string. New IDs are random UUID strings, but consumers must
not parse the value or use its bits as a business key. A persisted ID never changes.

`ResourceKind` has exactly these values:

- `Link`
- `File`
- `Note`

`ResourceRecord` contains:

| Field | Meaning |
| --- | --- |
| `id` | Stable `ResourceId`. |
| `kind` | One of the three `ResourceKind` values. |
| `title` | Required, trimmed display title. |
| `source` | Optional source URI, path, or other platform-defined locator. It is metadata, not resource content. |
| `description` | Optional trimmed description. |
| `tags` | A unique, case-insensitive set of non-empty trimmed strings. The original order is retained for stable display/export. |
| `isFavorite` | Local favorite flag. |
| `createdAtUtc` | Required UTC timestamp. |
| `updatedAtUtc` | Required UTC timestamp, never earlier than `createdAtUtc`. |
| `contentHash` | Optional opaque content hash. The current model does not require content bytes to be present. |

Dates are serialized as ISO-8601 UTC values. Null optional values are omitted or represented as
`null`, according to the host serializer. Resource bodies and attachments are deliberately not
part of `ResourceRecord`.

## Query

`ResourceQuery` has:

- `text`: optional case-insensitive substring matched against title, source, description, and tags;
- `kinds`: optional set of allowed kinds; an empty set means all kinds;
- `tags`: optional set; every requested tag must be present (case-insensitive);
- `favoritesOnly`: defaults to `false` (no favorite filtering); when `true`, only favorites are returned.
  Filtering explicitly for non-favorites is intentionally deferred so both platforms keep the same query shape;
- `limit`: default `100`, maximum `500`;
- `offset`: zero-based, non-negative offset.

Results are ordered by `updatedAtUtc` descending, then title case-insensitively, then ID ordinally.
Implementations must apply the same filters before pagination.

## Repository boundary

The application layer owns the repository port. Implementations provide:

- `get(id)` returning one record or null;
- `list(query)` returning a page;
- batch `upsert(records, overwriteExisting)` returning added/updated counts;
- `delete(id)` returning whether a record was removed.

An upsert with `overwriteExisting = false` is all-or-nothing and reports existing IDs as a
conflict. Package import uses this mode by default. UI code talks to application use cases, never
to a storage implementation.

## Windows storage

The current Windows adapter stores a versioned document at the platform's
`DatabaseDirectory/resources.json`:

```json
{
  "schemaVersion": 1,
  "revision": 1,
  "writtenAtUtc": "2026-08-18T00:00:00Z",
  "resources": []
}
```

The file is rewritten through a unique temporary file and atomic replacement. A process semaphore
and a lock file protect concurrent writers. A future schema is never overwritten by an older
implementation.

## Android mapping

Android may use Room, another local database, or a file store. Its repository must preserve the
same field meanings, filtering, ordering, pagination, and conflict behavior. The current Kotlin
model stores timestamps as epoch milliseconds internally; package adapters convert those instants
to the ISO-8601 `createdAtUtc` and `updatedAtUtc` fields. Tag and title normalization happens at
the platform boundary before persistence or export. Android does not connect to the Windows process
and does not use the Windows JSON file directly.
