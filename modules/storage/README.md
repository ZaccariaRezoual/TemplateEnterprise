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

## Public files

Some files have to be readable by someone who is not signed in — the pictures
on a showcase page are the obvious case. A file therefore declares its
**visibility at upload**:

```http
POST /api/files
Content-Type: multipart/form-data

file=<bytes>&visibility=Public
```

The rules, and why they are what they are:

- **The default is `Private`.** Every project will eventually publish a file;
  none of them wants to publish one by accident. A default of `Public` turns a
  forgotten parameter into a disclosure.
- **Visibility is decided once, at upload.** There is no endpoint that flips
  it afterwards: "make this public" is exactly the one-click action that turns
  a mistake into a leak with no review step in between. Re-upload instead.
- **A separate route serves public files** — `GET /api/files/public/{id}`,
  anonymous. It is a different route rather than a branch inside the
  authenticated one so that "this URL can only ever serve public files" is a
  property of the route, not of an `if` someone can move.
- **A private id on the public route answers 404, not 403.** Otherwise the
  endpoint becomes a way to test which identifiers exist.

Put `/api/files/public/{id}` in an `<img src>`. The authenticated route stays
`/api/files/{id}` and serves anything to a signed-in caller.

## Security decisions

- **Keys are generated, never derived from the file name.** An upload called
  `../../appsettings.json` can never become a path.
- **Keys are validated anyway** and the resolved path re-checked against the
  root before any I/O. Two guards, because a future caller may pass a key that
  came from the database or a URL.
- **The content type is decided from the bytes, never from the uploader.** The
  declared type is stored as metadata and never served; `ContentSniffer` reads
  the file's first bytes and the response carries what it found. A recognized
  image (PNG, JPEG, GIF, WebP, AVIF) is served inline so a page can display
  it; **everything else is `application/octet-stream` as an attachment**,
  which no browser executes. `X-Content-Type-Options: nosniff` closes the
  remaining gap.
- **SVG is deliberately not in that allowlist.** It is a document that can
  carry script, and serving one inline from our own origin is stored XSS with
  extra steps. Uploading one still works — it is downloaded, not rendered.
- **Uploads are size-capped** (`Storage:MaxUploadBytes`, 10 MB by default). An
  unbounded upload endpoint is a denial-of-service vector.
- **Delete removes metadata first.** An orphaned blob is recoverable garbage;
  a metadata row pointing at deleted bytes is a broken download.

## Endpoints

| Method | Route                    | Permission     |
| ------ | ------------------------ | -------------- |
| POST   | `/api/files`             | `files.write`  |
| GET    | `/api/files/{id}`        | authenticated  |
| GET    | `/api/files/public/{id}` | anonymous      |
| DELETE | `/api/files/{id}`        | `files.delete` |

Delete is idempotent: removing an unknown file is not an error.

## Configuration

```json
{ "Storage": { "LocalRootPath": "storage-files", "MaxUploadBytes": 10485760 } }
```

The local provider suits development and single-instance deployments. Multiple
instances need shared storage — implement `IFileStorageProvider` against your
object store.
