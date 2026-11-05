# Android preview: historical WebView implementation

> **Superseded:** this file records the first Android experiment. The active architecture is documented in [NATIVE_ANDROID_MIGRATION.md](NATIVE_ANDROID_MIGRATION.md). The product target is a full-screen native Android app with no WebView or embedded web server.

## Historical implementation

The first android-port implementation added a .NET 10 Android Activity that hosted the existing AssetRipper GUI in a WebView. It started the local web application on loopback and bridged Android's Storage Access Framework (SAF) pickers to desktop-oriented file dialogs.

That experiment included:

- A WebView host and loopback server startup.
- SAF file/folder selection and app-private staging.
- Export staging and copy to a selected SAF tree.
- An Android ARM64 APK build and release workflow.

## Runtime warning

The historical Android project referenced AssetRipper.GUI.Web and attempted to bundle managed ASP.NET Core assemblies from the Linux ARM64 runtime package. Linux runtime assemblies are not Android runtime assemblies. Successful APK compilation was not evidence that those assemblies load or that Kestrel can run on Android.

The WebView preview was never validated as a complete on-device product. Do not infer successful runtime startup, engine processing, or end-to-end import/export from its APK build.

## Supersession

The current android-port branch has replaced that WebView Activity with native Android views, removed the ASP.NET Core runtime-pack experiment, and introduced a direct engine adapter plus SAF import/export wiring. These newer changes still require clean CI and physical-device validation; they are not yet a supported release.

See [NATIVE_ANDROID_MIGRATION.md](NATIVE_ANDROID_MIGRATION.md) for current architecture, implementation status, Android-only repository scope, delivery sequence, and definition of done.
