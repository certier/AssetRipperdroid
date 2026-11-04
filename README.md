# AssetRipper

[![](https://img.shields.io/github/downloads/AssetRipper/AssetRipper/total.svg)](https://github.com/AssetRipper/AssetRipper/releases)
[![](https://img.shields.io/github/downloads/AssetRipper/AssetRipper/latest/total.svg)](https://github.com/AssetRipper/AssetRipper/releases/latest)
[![](https://img.shields.io/github/v/release/AssetRipper/AssetRipper)](https://github.com/AssetRipper/AssetRipper/releases/latest)
[![](https://weblate.samboy.dev/widgets/assetripper/-/gui/svg-badge.svg)](http://weblate.samboy.dev/engage/assetripper/)

AssetRipper is a powerful tool for analyzing Unity game files. For example, it enables game developers to:

* Find dependency assets that were accidentally included in their game
* Convert their assets into the native Unity engine format
* Identify code that could not be inlined or trimmed from their build
* Find broken asset references that would cause issues in their game

AssetRipper supports Unity versions from `3.5.0` to `6000.4.X`. However, support quality may vary slightly for different Unity versions.

[Downloads](https://assetripper.com/download.html)

## Android (early access)

An Android host is being developed on top of the existing AssetRipper application. It keeps the original web interface and Unity asset-processing engine, with a native Android Activity providing the WebView and Android's Storage Access Framework for file selection and export.

[Android build status](https://github.com/certier/AssetRipperdroid/actions/workflows/android.yml?query=branch%3Aandroid-port) · [Android implementation notes](docs/ANDROID_PORT.md)

**Current build:** Android ARM64 Release APK packaging passes in CI. The latest successful build is available as the [AssetRipper Android APK workflow artifact](https://github.com/certier/AssetRipperdroid/actions/runs/41859858312). Download the artifact ZIP and extract the APK before installing it. Workflow artifacts expire after 14 days.

**Important:** this is an engineering preview, not a supported release. The Android host currently bundles managed ASP.NET Core assemblies as a compatibility experiment. APK compilation does not prove that the server starts correctly on Android. Installation, startup, import, and export still need validation on an Android device. Do not use it as the only copy of important project data.

### Build from source

Requirements: .NET 10 SDK, the .NET Android workload, and an Android ARM64 device (Android 7.0 / API 24 or later).

```sh
dotnet workload install android
dotnet publish Source/AssetRipper.Android/AssetRipper.Android.csproj \
  --configuration Release \
  --framework net10.0-android \
  --runtime android-arm64 \
  -p:AndroidPackageFormat=apk
```

The current Android target is ARM64 only. The app's development version is `0.1.0-alpha.1`. Device testing is not complete, so this build is not a supported public release.

### Maintainer release process

Versioned Android pre-releases use tags named `android-v*` and the [Android Release workflow](.github/workflows/android-release.yml). Before creating a release tag, configure these repository or environment secrets:

- `ANDROID_KEYSTORE_BASE64`: base64-encoded release keystore
- `ANDROID_KEYSTORE_PASSWORD`: keystore password
- `ANDROID_KEY_ALIAS`: signing key alias
- `ANDROID_KEY_PASSWORD`: signing key password

The workflow fails before publishing if any signing secret is missing. It signs the ARM64 APK, generates a SHA-256 checksum, and creates a GitHub pre-release. Keep the keystore and passwords out of the repository. Do not create a release tag until the Android runtime and import/export smoke tests have passed.

See [Android port status and known limitations](docs/ANDROID_PORT.md) before trying the build.

## Premium Edition

There is a premium edition of AssetRipper. This edition includes additional [features and improvements](https://assetripper.com/premium/features/).

## Tips

If you find this tool useful, please consider [tipping](https://assetripper.com/premium/get/) to support development and gain access to [AssetRipper Premium](https://assetripper.com/premium/features/).

## Discord [![](https://img.shields.io/discord/867514400701153281?color=blue&label=AssetRipper)](https://discord.gg/XqXa53W2Yh)

The development of this project has a dedicated [Discord server](https://discord.gg/XqXa53W2Yh).

## Legal Disclaimers

* AssetRipper is licensed under the [GNU General Public License v3.0](LICENSE.md).
* AssetRipper is not sponsored by or affiliated with Unity Technologies or its affiliates.
* "Unity" is a registered trademark of Unity Technologies or its affiliates in the U.S. and elsewhere.
* The [Credits](https://assetripper.github.io/AssetRipper/articles/Credits.html) page contains a list of attributions.
