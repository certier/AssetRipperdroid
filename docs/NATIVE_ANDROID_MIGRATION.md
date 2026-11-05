# Native Android migration

## Product requirement

The Android application is a full-screen native Android app. A WebView, browser tab, embedded browser, loopback HTTP server, or hosted ASP.NET UI is not acceptable in the shipped app.

Keep the AssetRipper extraction and serialization engine wherever its dependencies are compatible with .NET for Android. Replace only presentation and platform integration. Do not rewrite the engine or delete reusable engine projects to make the repository appear Android-only.

## Verified baseline

The previous Android preview used a WebView, started WebApplicationLauncher from AssetRipper.GUI.Web, and attempted to bundle ASP.NET Core managed assemblies from the Linux ARM64 runtime package. That APK was only a packaging experiment. Linux runtime-pack assemblies are not Android runtime assemblies, and a successful publish did not prove that Kestrel could execute on Android.

The current native-android branch has moved beyond that preview:

- MainActivity is a native Android Activity with a full-screen native view hierarchy.
- The Android app no longer references AssetRipper.GUI.Web.
- The Linux ASP.NET Core runtime-pack workaround and obsolete INTERNET/cleartext manifest settings have been removed.
- Native SAF file and folder pickers stage input into app-private storage.
- The app now references the existing AssetRipper.Export.UnityProjects, AssetRipper.Export.PrimaryContent, and AssetRipper.IO.Files projects.
- The root AssetRipper.slnx now contains only the Android application; its engine dependencies build transitively.
- The desktop multi-platform publish workflow has been removed. The test workflow runs reusable engine test projects separately.
- AndroidEngineService calls the existing ExportHandler.LoadAndProcess and ExportHandler.Export APIs directly. Primary-content export is also exposed by the adapter.
- SAF export copies the generated Unity project tree into the user-selected document tree.

This is an implementation milestone, not a verified release. The current branch has not yet been confirmed by a successful Android build or a physical-device run. No claim is made that every transitive engine dependency is Android-compatible.

## Target architecture

```text
Native Android Activity / Views
  ├── Import file/folder picker (SAF)
  ├── Source list and operation status
  ├── Process selected Unity sources
  └── Export Unity project (SAF destination)
          |
          v
Android platform adapter
  ├── ContentResolver URI access
  ├── private staging and streaming copies
  ├── Android lifecycle and UI dispatch
  └── error reporting
          |
          v
AndroidEngineService
  ├── FullConfiguration
  ├── ExportHandler.LoadAndProcess
  ├── ExportHandler.Export
  └── PrimaryContentExporter
          |
          v
Existing AssetRipper engine projects
```

The engine entry points were traced from the existing GUI orchestration source. GameFileLoader previously delegated loading and processing to ExportHandler.LoadAndProcess(paths, LocalFileSystem.Instance), and Unity project export to ExportHandler.Export. The Android adapter calls those existing engine APIs directly; it does not route work through MVC controllers or invent a new extraction API.

## Native screen inventory

| Screen | Current state | Acceptance criteria |
|---|---|---|
| Home/import | First native screen implemented | Native controls; no browser surface; portrait/landscape/tablet layout |
| Source list | Implemented in memory | File/folder names shown; duplicate URI selection prevented |
| Configure | Not implemented | Options and defaults come from FullConfiguration |
| Job/progress | Basic status text and engine log callback | Durable foreground job, progress phases, cancellation, process recreation |
| Results | Not implemented | Actual counts, warnings, output summary |
| Export | Unity project export flow wired to SAF tree | Provider errors, collision handling, complete output verification |
| Logs | Engine messages currently feed the status text | Filterable logs and shareable redacted diagnostic report |
| Settings | Not implemented | Persisted settings with implemented effects |

Use native Android widgets. Preserve accessibility semantics, large touch targets, dynamic font scaling, localization, and responsive layouts. Prioritize correctness of import → process → export before decorative dashboards.

## Engine and runtime compatibility gate

The Android project references these existing engine projects:

- AssetRipper.Export.UnityProjects
- AssetRipper.Export.PrimaryContent
- AssetRipper.IO.Files

Their transitive project and NuGet dependency graph must be validated on Android. In particular:

1. Restore and compile the full Android project from a clean checkout.
2. Verify that all transitive dependencies load under the Android runtime; a successful compile alone is insufficient.
3. Run a tiny known-good Unity fixture through LoadAndProcess on an ARM64 device/emulator.
4. Verify the Unity project exporter and primary-content exporter on the same fixture.
5. Inventory native libraries, dynamic code generation, reflection, file-system assumptions, and linker/trimming behavior.
6. Keep ASP.NET Core and the Linux runtime pack out of the Android dependency graph.

If a dependency is unsupported on Android, isolate or replace it at the adapter boundary. Do not silently skip processing features.

## Storage contract

Use Storage Access Framework content URIs as the user-visible storage contract. Persist granted URI permissions where supported. Inputs are streamed into app-private staging because the existing engine currently consumes filesystem paths. Folder imports recurse through DocumentsContract. Export writes to private staging and copies the directory tree to the selected SAF destination.

Remaining storage work: check free space before staging, bound concurrent copies, handle provider timeouts/disconnections, verify output counts and byte totals, make cleanup robust, and test duplicate names and read/write failures across local and cloud document providers.

## Background execution and lifecycle

Current extraction and export are dispatched off the UI thread, but remain Activity-owned tasks. They are not yet durable jobs. Rotation/process recreation, user cancellation, foreground notification, progress persistence, and reliable cleanup are not complete.

The target is a durable user-initiated job with persisted source/output references, progress checkpoints where possible, terminal state, explicit cancellation, and an Android foreground service with a persistent notification. WorkManager may coordinate deferrable work but is not a substitute for an interactive long-running extraction service.

## Android-only repository scope

The product repository should build and release Android only. Keep reusable upstream engine projects and source files. Remove desktop packaging, desktop-only workflows, and desktop instructions only after confirming they are not needed by the engine or upstream merge strategy. Do not delete platform conditionals from reusable engine code merely to make the repository look Android-only.

Required outcomes:

- One documented Android application entry point.
- Android-only CI for restore, build, static checks, tests, and APK/AAB packaging.
- CI runs for both branch pushes and pull requests.
- A protected release workflow using a maintainer-owned signing key.
- No release tag until runtime, SAF, and physical-device smoke tests pass.
- README and release notes match tested behavior.

## Delivery sequence

### A. Baseline and dependency map
- Record branch/head and preserve existing user changes.
- Inventory solution projects, engine entry points, GUI routes, workflows, native dependencies, and license notices.
- Establish reproducible build/test results.
- Deliverable: dependency map and compatibility report.

### B. Native shell — first slice implemented
- Replace the WebView Activity with a full-screen native Activity.
- Add native SAF file/folder selection and selected-source display.
- Remove the web host and Linux ASP.NET runtime-pack workaround.
- Remaining: navigation, responsive layout, accessibility, and persisted source/job state.

### C. Direct engine adapter — first slice implemented
- Reference reusable engine projects directly.
- Call the existing LoadAndProcess and Export APIs.
- Remaining: clean Android build, runtime compatibility, known-good fixture verification, settings UI, cancellation, and robust progress reporting.

### D. End-to-end storage and job lifecycle
- Verify SAF import/export with local and cloud providers.
- Add free-space checks, bounded streaming, collision policy, and cleanup.
- Implement durable foreground jobs, notifications, progress, cancellation, and process recreation.

### E. Product quality
- Add configuration, results, logs, diagnostics, settings, accessibility, localization, tablet layouts, and performance limits.
- Test large projects and low-memory behavior.

### F. Android-only release
- Remove obsolete desktop-specific packaging/workflows only after confirming engine build requirements and upstream merge strategy.
- Build APK and AAB as appropriate; test clean install and upgrade signing.
- Publish signed pre-releases only after all acceptance gates pass.

## Definition of done

- No WebView, browser UI, Kestrel, loopback HTTP server, or ASP.NET Core runtime-pack workaround in the shipped app.
- All primary screens are native Android controls.
- Real AssetRipper engine processing is invoked through verified existing APIs.
- SAF import and export work with supported document providers.
- Long jobs survive Activity recreation, provide progress, and support cancellation.
- A known-good Unity fixture completes end-to-end on a physical ARM64 device.
- CI builds from a clean checkout and runs relevant tests.
- Release signing is protected and reproducible.
- README, storage behavior, supported inputs, and limitations match tested behavior.

## Current status

Native UI, SAF source selection/staging, direct engine calls, SAF export wiring, an Android-only root solution, and removal of desktop binary publishing are committed to the native-android branch. The ARM64 APK publish and artifact upload passed in [Android workflow run #77](https://github.com/certier/AssetRipperdroid/actions/runs/42038033550). The six reusable engine test groups passed in [engine test run #89](https://github.com/certier/AssetRipperdroid/actions/runs/42038033504). The APK is an engineering test artifact; it has not been installed on a physical device and no real Unity fixture has been processed on Android. Runtime compatibility and real-world SAF provider behavior remain unverified. The app is not a release.
