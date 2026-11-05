# Android preview: historical implementation and limitations

> **Superseded direction:** this document describes the earlier WebView-based Android preview. The product requirement is now a full-screen native Android UI with no WebView or embedded web server. Follow [NATIVE_ANDROID_MIGRATION.md](NATIVE_ANDROID_MIGRATION.md) for the active architecture and acceptance criteria.

## Historical implementation

The android-port branch originally added a .NET 10 Android Activity that hosted the existing AssetRipper GUI in a WebView. It started the local web application on loopback and bridged Android's Storage Access Framework (SAF) pickers to the existing desktop-oriented file dialogs.

That implementation included:

- Source/AssetRipper.Android targeting net10.0-android.
- A WebView host and loopback server startup.
- SAF file/folder selection and app-private staging.
- Export staging and copy to a selected SAF tree.
- Android build and release workflows.

## Important runtime warning

The Android project referenced AssetRipper.GUI.Web and attempted to bundle managed ASP.NET Core assemblies from the Linux ARM64 runtime package. Linux runtime assemblies are not Android runtime assemblies. Successful APK compilation is not evidence that those assemblies load or that Kestrel can run on Android.

The old preview has not been validated as a complete on-device product. In particular, do not infer successful runtime startup, engine processing, or end-to-end import/export from an APK build alone.

## Migration status

The current MainActivity is still the historical WebView implementation until it is replaced by the native shell in a separately validated change. The Android app must not be described as native or production-ready before that replacement and its tests are complete.

See [NATIVE_ANDROID_MIGRATION.md](NATIVE_ANDROID_MIGRATION.md) for:

- Native screen inventory and navigation target.
- Engine compatibility gate and adapter boundary.
- SAF storage contract and long-running job lifecycle.
- Android-only repository scope.
- Delivery sequence and definition of done.
