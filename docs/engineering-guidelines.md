# Engineering guidelines

A short checklist of the conventions this repository applies. It is our own summary of the official .NET
framework design guidance, the C# coding conventions and the VitePress documentation, reduced to what matters here.

## C# / .NET plugins

- One public type per concern; file names match the main type. Long types are split into `partial` files named
  `Type.Concern.cs`, keeping namespace and type name unchanged.
- Nullable reference types and implicit usings stay on. Unused usings and private members are removed
  (IDE0005, IDE0051, IDE0052 are checked locally with `EnforceCodeStyleInBuild`).
- Public surface is minimal: `internal` by default, `public` only for the plugin entry type, the actions and the
  types the tests need. Sealed classes unless designed for inheritance.
- Repeated shape becomes a base class or helper (for example the start/stop/toggle output actions) instead of copy-paste.
  Identifiers that are persisted or referenced by users (action `Type` ids, variable names, settings keys) never change.
- Async: every async method that can wait takes a `CancellationToken` and passes it on; cancellation is handled as
  a normal exit (`OperationCanceledException`), never logged as an error. No `async void`, no blocking on tasks.
- Long-lived connections: one owner, a linked `CancellationTokenSource` per session, capped exponential backoff
  with jitter, a hard stop for errors that retrying cannot fix, cleanup in `finally`.
- Disposal: types that own sockets, timers or semaphores implement `IAsyncDisposable`/`IDisposable` and release them on every path.
- Locks protect small critical sections only; never await while holding one.
- Exceptions: throw specific types with an actionable message; catch only what is handled (filters with `when`).
- Low allocation on hot paths (polling, event handling): collection expressions, static lambdas and cached regexes are fine; no premature tuning elsewhere.
- Formatting comes from `.editorconfig` (UTF-8, LF in the repo, 4 spaces, file-scoped namespaces).
- Plugins never ship the SDK assembly (`ExcludeAssets="runtime"`), depend only on the published SDK package and
  keep their own version in their own changelog.

## Documentation (VitePress)

- Code samples are imported from real, compiling projects with `<<< @/../path#region`, so the docs cannot drift
  from the code. `#region` names are part of the docs contract and are not renamed.
- Path filters of the site workflow list every source file the docs import.
- Internal links must resolve; the build fails on dead links and missing regions.
- One topic per page, grouped in the sidebar by audience (introduction, tutorials, guides, reference).

## Working rules

- Behavior-preserving refactors go in small commits; each one builds and passes the tests.
- Anything that would change behavior or a public surface is written up in `docs/proposals/` instead of being done silently.
- Only code proven unused (build with the analyzers, search, tests) is deleted; the rest is listed in `docs/proposals/dead-code-review.md`.
