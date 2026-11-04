# Android port

## Objective

Run the existing AssetRipper application on Android without replacing its GUI or reimplementing its Unity asset pipeline. The Android host is a native Activity containing a WebView; the intended application UI remains the HTML/CSS/JavaScript served by `AssetRipper.GUI.Web`.

## Current implementation boundary

The first Android host milestone adds:

- a .NET 10 Android application project;
- a WebView host for the existing local web UI;
- a loopback-only Kestrel endpoint on port 18555;
- startup readiness polling and an on-screen startup error;
- an Android ARM64 APK CI workflow.
- an Android document-picker bridge: selected files and folders are copied from SAF content URIs into app-private staging paths before the existing importer sees them.
- export staging plus recursive copy into a user-selected SAF document tree after the existing exporter completes.

This is an initial integration milestone, **not a completed Android port**. The Release ARM64 Android publish and APK artifact upload now pass in GitHub Actions. The repository-wide solution build, tests, and DocFX build also pass. The SAF bridge still requires physical-device testing. The GUI libraries keep their `Microsoft.AspNetCore.App` framework references private. The Android host currently experiments with bundling the managed ASP.NET Core runtime assemblies from the Linux ARM64 runtime pack; these managed assemblies must be validated under the Android runtime before the host can be considered runnable.

## Required next milestones

1. **Android-compatible server runtime**
   - Install the generated APK on an ARM64 Android device or emulator.
   - Validate that the bundled ASP.NET Core managed assemblies load correctly under the Android runtime.
   - Verify Kestrel loopback hosting and all original routes on physical ARM64 Android devices.
   - If the managed runtime-pack approach is incompatible with Mono, replace only the HTTP hosting layer while retaining the existing page renderers, route behavior, APIs, and static assets.

2. **Storage Access Framework hardening**
   - Test Android document and directory pickers on physical devices and multiple document providers.
   - Harden cancellation, duplicate names, insufficient storage, provider errors, and interrupted transfers.
   - Add export conflict handling and cleanup for abandoned staging directories.

3. **Native dialog replacement**
   - Replace desktop native-dialog calls behind a platform abstraction.
   - Keep desktop dialog behavior unchanged.
   - Route Android dialog requests to the Activity's SAF bridge.

4. **Lifecycle and resource limits**
   - Keep long-running import/export work alive through a foreground service with a visible notification.
   - Persist job state and logs.
   - Handle process recreation and storage exhaustion.
   - Add cancellation and progress reporting without changing the original GUI layout.

5. **Verification**
   - Build Debug and Release APKs.
   - Install and launch on Android ARM64.
   - Test APK, split APK, OBB, folder, Mono, and IL2CPP inputs.
   - Verify texture/model/audio previews and Unity project export.
   - Test large inputs, low-memory conditions, rotation, backgrounding, and SAF export.

## Build

```bash
dotnet workload install android
dotnet publish Source/AssetRipper.Android/AssetRipper.Android.csproj -c Release -f net10.0-android
```

The build is expected to remain blocked until milestone 1 is completed. CI must report that failure rather than publishing a placeholder APK.
