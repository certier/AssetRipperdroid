# Native Android migration

## Decision

The Android application must present a **native Android interface**. A WebView, browser tab, embedded browser, loopback HTTP server, or hosted ASP.NET UI is not an acceptable final UI.

Keep the AssetRipper extraction and serialization engine wherever its dependencies are compatible with .NET for Android. Replace only the presentation and platform integration layers. Do not rewrite the engine or delete desktop source projects until their reusable boundaries and dependencies have been mapped.

## Verified starting point

The android-port branch currently contains:

- Source/AssetRipper.Android, targeting net10.0-android.
- A MainActivity that creates an Android WebView, starts WebApplicationLauncher, and loads http://127.0.0.1:18555/.
- A project reference to AssetRipper.GUI.Web.
- An experimental reference to the Linux ARM64 ASP.NET Core runtime package.
- SAF import/export code in the Android activity and an Android dialog bridge.
- CI and release workflows described in the root README and previous Android port notes.

The current APK is therefore a **WebView-hosted preview**, not a native Android port. A successful APK publish does not prove that Kestrel or ASP.NET Core can execute under the Android runtime. The Linux runtime-pack workaround is not accepted as a production architecture.

## Target architecture

Use .NET for Android (net10.0-android) and Android SDK views/activities directly. Keep the UI full-screen and edge-to-edge, with Android system bars handled intentionally. No WebView dependency in the final application.

```text
Android Activity / native Views
  ├── Home and recent jobs
  ├── Import source picker (SAF)
  ├── Extraction configuration
  ├── Job progress / logs / cancellation
  ├── Export destination picker (SAF)
  └── Settings / diagnostics
          |
          v
Android application services
  ├── SAF URI access + streaming staging
  ├── durable job state + foreground execution
  ├── structured logs and error mapping
  └── Android lifecycle / notifications
          |
          v
AssetRipper engine adapter
  ├── explicit input/output abstractions
  ├── progress and cancellation
  └── existing AssetRipper processing pipeline
```

The engine adapter is a hard integration boundary. It must call a real, supported engine entry point. Do not invent a GameExtractor.Extract API or route calls through MVC controllers merely to make the UI appear connected. Before implementation, identify the existing application orchestration entry point and classify every transitive dependency for Android compatibility.

## Native screen inventory

| Screen | Native implementation | Acceptance criteria |
|---|---|---|
| Home | Activity with app bar, import action, recent jobs, storage summary | Native controls; no browser surface; adapts to portrait, landscape, and tablets |
| Import | SAF document/tree pickers and selected-source summary | Supports file, folder, cancellation, duplicate names, provider errors |
| Configure | Native form controls for available engine options | Options and defaults come from the engine, not hard-coded guesses |
| Job | Foreground execution, progress, phase, elapsed time, cancel | UI remains responsive; process recreation restores job state |
| Results | Output summary, warnings, file counts, open/share/export actions | Counts reflect actual output; failed export is never shown as success |
| Logs | Filterable structured log list and shareable diagnostic report | Sensitive local paths are redacted from shared diagnostics |
| Settings | Theme, output behavior, resource limits, diagnostics | Persisted settings; every displayed setting has an implemented effect |

Use Android native widgets and Material styling where a compatible AndroidX dependency is justified. Preserve accessibility semantics, large touch targets, dynamic font scaling, and localized strings. The first implementation should prioritize a usable end-to-end import → process → export path over decorative dashboards.

## Engine and runtime compatibility gate

Before wiring extraction into Android:

1. Identify the actual engine orchestration API used by the existing desktop GUI.
2. Trace its project references and NuGet dependencies.
3. Separate platform-neutral parsing/serialization from desktop-only UI, ASP.NET Core, filesystem, process, native-library, and OS integration.
4. Build a minimal Android instrumentation/smoke harness that loads the engine assembly and executes a tiny known-good fixture.
5. Remove the ASP.NET Core framework reference and Linux runtime-pack workaround from the Android dependency graph only after the engine adapter no longer requires the web host.
6. Verify ARM64 native dependencies and trimming/AOT behavior independently. Do not assume a Linux native library or runtime pack is Android-compatible.

If a dependency is unsupported on Android, isolate or replace that dependency at the adapter boundary. Do not silently skip processing features.

## Storage contract

Use Storage Access Framework content URIs as the user-visible storage contract. Persist granted URI permissions where supported. Stream inputs into app-private staging only when the engine requires seekable filesystem paths; enforce available-space checks and clean up abandoned staging data. Export through a selected tree URI, stream files, preserve directory structure, detect name collisions, and report provider failures. Never request broad storage permissions merely to bypass SAF.

## Background execution and lifecycle

Treat extraction as a durable job, not an Activity-owned task. Persist job identifiers, source references, output configuration, progress checkpoints where possible, and terminal status. Use an Android foreground service for user-initiated long-running extraction with a persistent notification and explicit cancellation. WorkManager may coordinate deferrable work, but must not be presented as a guarantee that a long interactive extraction can run uninterrupted. Reattach the UI to an existing job after rotation or process recreation.

## Android-only repository scope

The product repository should build and release Android only. Keep upstream reusable engine projects and source files; remove desktop packaging, desktop-only workflows, and desktop instructions only after confirming they are not needed by the engine or upstream merge strategy. Do not delete Windows/Linux/macOS conditionals inside reusable engine code merely to make the repository look Android-only.

Required repository outcomes:

- One documented Android application entry point.
- Android-only CI for restore, build, static checks, tests, and APK/AAB packaging.
- A separate protected release workflow using a maintainer-owned signing key.
- No release tag until runtime, SAF, and real-device smoke tests pass.
- README and release notes accurately state supported Android versions, ABI, known limitations, and test evidence.

## Delivery sequence

### A. Baseline and dependency map
- Record branch/head and preserve existing user changes.
- Inventory solution projects, entry points, GUI routes, dialog contracts, engine dependencies, workflows, and license notices.
- Establish reproducible baseline build/test results.
- Deliverable: dependency map and explicit list of reusable engine APIs.

### B. Native shell
- Replace the WebView Activity with a full-screen native Activity and native navigation.
- Add home, import, job, results, logs, and settings screen skeletons with real navigation.
- Remove Kestrel startup and WebView permissions/dependencies from the Android app.
- Deliverable: installable native shell with no embedded browser.

### C. Engine adapter
- Implement an adapter around the verified existing engine entry point.
- Keep Android-specific SAF and lifecycle concerns outside the engine.
- Add fixture-based Android-compatible engine tests.
- Deliverable: one small Unity fixture imports and processes without ASP.NET Core.

### D. End-to-end workflow
- Implement SAF import, options, foreground job execution, progress, cancellation, output staging, and SAF export.
- Implement robust error mapping, storage checks, and cleanup.
- Deliverable: complete import → extraction → export workflow on a physical ARM64 device.

### E. Product quality
- Implement recent jobs, logs, diagnostics, settings, accessibility, localization, tablet layouts, and performance limits.
- Test large projects and low-memory behavior.
- Deliverable: repeatable QA report and documented limitations.

### F. Android-only release
- Remove obsolete desktop-specific build/release workflows after confirming engine build requirements.
- Build APK and AAB as appropriate; test clean install and upgrade signing.
- Publish signed pre-releases only after acceptance gates pass.
- Deliverable: reproducible signed Android release with checksums and release notes.

## Definition of done

- No WebView, browser UI, Kestrel, loopback HTTP server, or ASP.NET Core runtime-pack workaround in the shipped app.
- All primary screens are native Android controls.
- Real AssetRipper engine processing is invoked through a verified adapter.
- SAF import and export work with local and supported document providers.
- Long jobs survive Activity recreation, provide progress, and support cancellation.
- A known-good Unity fixture completes end-to-end on a physical ARM64 device.
- CI builds from a clean checkout and runs the relevant tests.
- Release signing is protected and reproducible.
- README, privacy/storage behavior, supported inputs, and limitations match tested behavior.

## Current implementation status

This document is the migration contract and backlog baseline. It does not claim that the native UI or engine integration has already been implemented. The current Android Activity remains WebView-based until the native shell change is committed and validated.