# Android port

## Goal

Run the existing AssetRipper application on Android without replacing its web interface or Unity asset-processing pipeline.

The Android application is a native .NET Android Activity. It hosts the existing AssetRipper web UI in a WebView and starts the local application server on loopback.

## Current status

The Android ARM64 Release APK publishes successfully in GitHub Actions. The repository solution build, test workflow, and documentation build also pass on the integration branch.

That is a packaging milestone, not proof that the application is ready for general use. The APK has not yet been installed and exercised on a physical Android device. In particular, the Android server startup path still needs runtime validation.

| Area | Status |
| --- | --- |
| .NET 10 Android ARM64 project | Implemented |
| Existing AssetRipper web UI hosted in WebView | Implemented |
| Local server startup polling and startup error UI | Implemented; device validation pending |
| Android file and folder picker integration | Implemented; device validation pending |
| Import staging in app-private storage | Implemented |
| Export to a user-selected Storage Access Framework folder | Implemented; device validation pending |
| ARM64 test APK compilation and artifact upload | Passing in CI |
| Android installation and runtime smoke test | Not yet tested |
| Large-project, lifecycle, and low-memory testing | Not yet tested |
| Signed public release | Not available yet |

## Architecture

- `Source/AssetRipper.Android` contains the Android Activity and application manifest.
- The Activity hosts the original `AssetRipper.GUI.Web` application in a WebView.
- The local server listens on `127.0.0.1:18555`; the Activity waits for a successful HTTP response before navigating the WebView.
- `AndroidDialogBridge` connects the existing GUI dialog requests to Android document pickers.
- Selected files and folders are copied from content URIs into app-private staging directories. The existing importer therefore continues to receive filesystem paths.
- Export output is staged locally and copied recursively to the directory selected by the user through Android's Storage Access Framework.
- Desktop dialog behavior remains the fallback on non-Android platforms.

## Runtime compatibility warning

The existing web projects depend on `Microsoft.AspNetCore.App`. Android does not provide the ASP.NET Core shared runtime pack used by the desktop targets.

The Android project currently keeps the framework reference private and bundles managed ASP.NET Core assemblies from the .NET 10 Linux ARM64 runtime pack. This is an experimental compatibility approach. A successful Android publish only proves that the APK can be produced; it does not prove those assemblies can load under the Android runtime or that Kestrel can start there.

The first device test must verify assembly loading, Kestrel startup, the loopback endpoint, static assets, and the original GUI routes. If this approach fails, replace the hosting layer with an Android-compatible implementation while preserving the existing UI, application services, and asset pipeline.

## Build

Requirements:

- .NET 10 SDK
- .NET Android workload
- Android SDK components installed by the workload
- ARM64 Android device for runtime testing (minimum supported platform: Android 7.0 / API 24)

Build an APK:

```sh
dotnet workload install android
dotnet publish Source/AssetRipper.Android/AssetRipper.Android.csproj \
  --configuration Release \
  --framework net10.0-android \
  --runtime android-arm64 \
  -p:AndroidPackageFormat=apk
```

The CI workflow is [`.github/workflows/android.yml`](../.github/workflows/android.yml). It publishes the installable `com.assetripper.android-Signed.apk` as the `AssetRipper-Android-ARM64-Test-APK` artifact. The APK is signed with the CI debug key for device testing only. It is not signed with the maintainer's release key and must not be distributed as a public release. Workflow artifacts are temporary and expire after the configured retention period.

## Signed pre-release workflow

The maintainer workflow is [`.github/workflows/android-release.yml`](../.github/workflows/android-release.yml). It runs only for tags matching `android-v*`, requires a dedicated Android release keystore, signs the APK, calculates a SHA-256 checksum, and publishes a GitHub pre-release.

Configure the `android-release` GitHub Actions environment and add these protected environment secrets before creating a release tag:

- `ANDROID_KEYSTORE_BASE64`
- `ANDROID_KEYSTORE_PASSWORD`
- `ANDROID_KEY_ALIAS`
- `ANDROID_KEY_PASSWORD`

Create the signing key once on a trusted maintainer machine and store a secure backup:

```sh
keytool -genkeypair -v -keystore assetripper-release.jks -storetype JKS \\
  -keyalg RSA -keysize 4096 -validity 10000 -alias assetripper
base64 < assetripper-release.jks | tr -d '\n'
```

Put the base64 output and credentials into the four GitHub secrets listed above. Do not commit the keystore or its passwords. Keep the signing key backed up securely; updates to an installed Android application must use the same signing identity. The release workflow intentionally fails when signing configuration is missing rather than publishing a debug-signed package.

The current application display version is `0.1.0-alpha.1`. Increment both the display version and Android version code for each new app release. Do not tag this version for public distribution until the device validation plan below has passed.

## Validation plan

### 1. Runtime smoke test

- Install the APK on an ARM64 Android device or emulator.
- Confirm the application starts without an unhandled exception.
- Confirm the managed ASP.NET Core assemblies load and Kestrel binds only to loopback.
- Confirm the WebView loads the original GUI and can reach its API and static assets.
- Rotate the device, background the app, return to it, and test process recreation.

### 2. Import and export

- Import individual files and multiple files from the system picker.
- Import folders from internal storage, removable storage, and supported cloud document providers.
- Test cancellation, duplicate names, unreadable documents, unavailable providers, and interrupted copies.
- Export a Unity project to a newly selected folder.
- Test existing destination names, provider write failures, insufficient storage, and interrupted exports.
- Confirm failed or abandoned operations do not silently appear as completed exports.

### 3. Unity project coverage

Test representative APK, split APK, OBB, folder, Mono, and IL2CPP inputs. Verify texture, model, audio, and other previews, then open the exported Unity project in the supported Unity editor.

### 4. Resource and lifecycle behavior

- Test small and large projects, including projects that approach available device memory.
- Measure peak memory and temporary storage use.
- Verify progress reporting and cancellation.
- Ensure long-running jobs behave predictably when the Activity is backgrounded or the process is reclaimed.
- Add a foreground service and persistent job state if device testing shows that background execution cannot reliably complete long operations.

### 5. Release readiness

- Validate on at least one physical ARM64 device.
- Add automated or repeatable smoke tests for startup and basic import/export.
- Configure a dedicated release signing key through protected CI secrets; never commit the keystore or passwords.
- Publish versioned release notes that state tested Android versions, architecture, known limitations, and checksums.
- Keep the Android build marked as experimental until the runtime and storage tests pass.
