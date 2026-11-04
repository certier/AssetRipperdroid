# Android port

## Objective

Run the existing AssetRipper application on Android without replacing its GUI or reimplementing its Unity asset pipeline. The Android host is a native Activity containing a WebView; the intended application UI remains the HTML/CSS/JavaScript served by `AssetRipper.GUI.Web`.

## Current implementation boundary

The first Android host milestone adds:

- a .NET 10 Android application project;
- a WebView host for the existing local web UI;
- a loopback-only Kestrel endpoint on port 18555;
- startup readiness polling and an on-screen startup error;
- an Android APK CI workflow.

This is a host integration milestone, **not a completed Android port**. The referenced GUI project currently targets `net10.0` and uses the `Microsoft.AspNetCore.App` shared-framework reference. Android does not provide that shared framework automatically. The project reference therefore requires the ASP.NET Core runtime dependency migration described below before the APK can be considered buildable or runnable.

## Required next milestones

1. **Android-compatible server runtime**
   - Replace shared-framework-only references in `AssetRipper.GUI.Web` and `AssetRipper.Web` with the minimum explicit ASP.NET Core package references that can run on Android.
   - Verify Kestrel loopback hosting on physical ARM64 Android devices.
   - Keep the existing route handlers, page renderers, APIs, and static assets unchanged.

2. **Storage Access Framework**
   - Implement Android document and directory pickers.
   - Import selected content URIs into app-private staging storage before passing ordinary paths to the existing importer.
   - Export into app-private staging directories and copy results to user-selected document-tree destinations.
   - Handle cancellation, duplicate names, insufficient storage, and interrupted transfers.

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
