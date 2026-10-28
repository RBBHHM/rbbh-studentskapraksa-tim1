# RBI.ApiCore

## Purpose

Typed HTTP registration, transport results, correlation and safe resilience; no application models or authentication implementation.

## Source integration

This repository builds this library through ProjectReference; see ../SOURCE.md.

## Minimal setup

```csharp
builder.Services.AddRbiApiClient<IExampleApi, ExampleApi>("Example", options =>
    options.BaseUrl = new Uri("https://api.example.test/"));
```
IExampleApi/ExampleApi are application-owned; inject IRbiApiTransport into the implementation and use its typed GetAsync/SendAsync methods.

## Key contract

IRbiApiTransport returns RbiApiResult / RbiApiResult<T>. RbiApiError.Kind is semantic; response details are bounded but untrusted.

The configured request timeout covers logical request handlers, response headers and body deserialization/error reads. Expiry returns `Timeout`; caller cancellation remains `Cancellation`. Successful response size is application-owned; the transport bounds error bodies, not arbitrary successful payloads.

## Multipart imports / binary downloads

Inject `IRbiApiFileTransport` instead of `IRbiApiTransport` into the typed client
when files are needed. It extends the existing JSON interface; existing transport
implementers/consumers are not forced to change. `AddRbiApiClient` supplies both
through the same transport implementation.

`PostMultipartAsync<T>(relativePath, multipart, cancellationToken)` consumes and
disposes the multipart content and streams, including on failure/invalid origin.
It returns the backend's typed JSON result. The application must enforce its
upload size/schema rules; this method never retries POST.

`DownloadAsync(relativePath, maxBytes, cancellationToken)` returns `RbiApiFile`
with bytes, media type and a safe attachment basename. Default bound is 32 MiB;
allowed bounds are 1 byte through 256 MiB. Declared and chunked oversized bodies
are rejected. The request deadline includes the entire body read and streams
are disposed before return. Use smaller domain-specific limits where appropriate.
This is bounded buffering, not an unbounded streaming API.

Use the existing `.AddRbiUserAccessToken()` opt-in for both methods. The same
server-side token refresh, origin guard, correlation and semantic HTTP errors
apply; neither method exposes bearer tokens or raw HTTP responses. Never use
the underlying named HttpClient to bypass the logical token handler. The app
owns authorization, localized error copy and safe delivery of bytes to its user.

## Production caveats

Request URIs passed to the transport must be relative to `BaseUrl`; absolute or scheme-relative URIs are rejected so a propagated user token cannot reach another host. Map failures to application-owned localized UI copy. Do not directly render or log upstream Title/Detail. Configure real base URLs externally. Write operations are not automatically retried.

See [repository development guidance](https://github.com/mahmutovichana/rbi-design-core/blob/main/Docs/DEVELOPMENT.md), [deployment](https://github.com/mahmutovichana/rbi-design-core/blob/main/Docs/DEPLOYMENT.md) and [public API review](https://github.com/mahmutovichana/rbi-design-core/blob/main/Docs/PUBLIC-API.md).
