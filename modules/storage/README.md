# Storage Module

File upload and download with a swappable storage provider.

## Where the bytes live

`IFileStorageProvider` abstracts the physical store: local disk in
development, S3-compatible object storage in production, with no change above
the interface. Note what it does **not** expose — paths. Callers work with
opaque storage keys, so no caller can build a filesystem path, which is how
traversal bugs get in.

Metadata (name, size, uploader) lives in this module's `storage` schema; it is
the only thing mapping a public id to the stored bytes.

## Security decisions

- **Keys are generated, never derived from the file name.** An upload called
  `../../appsettings.json` can never become a path.
- **Keys are validated anyway** and the resolved path re-checked against the
  root before any I/O. Two guards, because a future caller may pass a key that
  came from the database or a URL.
- **Downloads are served as `application/octet-stream`, never the uploader's
  content type.** Echoing it would let an uploaded `.html` execute on your own
  origin with the user's session.
- **Uploads are size-capped** (`Storage:MaxUploadBytes`, 10 MB by default). An
  unbounded upload endpoint is a denial-of-service vector.
- **Delete removes metadata first.** An orphaned blob is recoverable garbage;
  a metadata row pointing at deleted bytes is a broken download.

## Endpoints

| Method | Route             | Permission     |
| ------ | ----------------- | -------------- |
| POST   | `/api/files`      | `files.write`  |
| GET    | `/api/files/{id}` | authenticated  |
| DELETE | `/api/files/{id}` | `files.delete` |

Delete is idempotent: removing an unknown file is not an error.

## Configuration

```json
{ "Storage": { "LocalRootPath": "storage-files", "MaxUploadBytes": 10485760 } }
```

The local provider suits development and single-instance deployments. Multiple
instances need shared storage — implement `IFileStorageProvider` against your
object store.
