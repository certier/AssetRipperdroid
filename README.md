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

## Android port (development)

This fork is being converted into an **Android-only, full-screen native application**. The Android app no longer uses the previous WebView/Kestrel host. The native Activity now provides SAF file/folder selection, stages selected input in app-private storage, calls AssetRipper's existing engine APIs directly, and exports a Unity project to a user-selected SAF folder. The root solution and binary publishing workflow have also been narrowed to Android; reusable engine source remains intact.

**Current state:** the native import/process/export path now builds as an ARM64 APK in GitHub Actions, and the reusable engine test suite passes. The CI APK is signed with the Android Debug certificate, not a production release key, and has not yet been installed or exercised on a physical device; real Unity fixture processing, transitive runtime compatibility, and real-world SAF provider behavior remain unverified. This is an engineering preview, not a supported release. Do not rely on it as the only copy of important data.

- [Latest successful Android ARM64 build and APK artifact](https://github.com/certier/AssetRipperdroid/actions/runs/42038033550)
- [Latest successful engine test run](https://github.com/certier/AssetRipperdroid/actions/runs/42038033504)

- [Native Android migration architecture, status, and acceptance criteria](docs/NATIVE_ANDROID_MIGRATION.md)
- [Historical WebView preview notes](docs/ANDROID_PORT.md)
- [Android build workflow](https://github.com/certier/AssetRipperdroid/actions/workflows/android.yml?query=branch%3Anative-android)

The current Android target is ARM64 and .NET 10 for Android (minimum API 24). No public release will be tagged until clean CI, engine runtime compatibility, SAF import/export, and physical-device smoke tests pass.

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
