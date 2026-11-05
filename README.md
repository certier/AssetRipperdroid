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

This fork is being converted into an **Android-only, full-screen native application**. The final Android interface will use native Android controls and will not use a WebView, browser UI, embedded web server, or hosted ASP.NET interface. The existing AssetRipper processing engine is intended to be preserved behind an Android-specific adapter, subject to runtime and dependency validation.

**Current state:** the existing `android-port` APK is an earlier WebView-hosted engineering preview. It is not the native app described above, and its successful APK packaging does not prove that the ASP.NET Core/Kestrel host runs on Android. Do not treat it as a usable release or rely on it for important data. Native UI and direct engine integration are not yet complete.

- [Native Android migration architecture and acceptance criteria](docs/NATIVE_ANDROID_MIGRATION.md)
- [Previous Android preview notes and known limitations](docs/ANDROID_PORT.md)
- [Android build workflow](https://github.com/certier/AssetRipperdroid/actions/workflows/android.yml?query=branch%3Aandroid-port)

No public Android release will be tagged until native UI, engine execution, SAF import/export, and physical-device smoke tests pass. The Android project currently targets ARM64 and .NET 10 for Android; supported API levels and release packaging remain subject to validation.

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
