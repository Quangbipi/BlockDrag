# Repository Guidelines

## Project Structure & Module Organization

This is a Unity 2022.3.62f2 project. Keep first-party gameplay code and content under `Assets/_Game/`; its feature folders include `_UI`, `_Items`, `_IAP`, `_Ads`, `_Tutorial`, and `_SubSystem`. Shared foundations live in `_Base`, `_Common`, `_Data`, `_DesignPattern`, and `_Utilities`. Scenes are stored in `Assets/_Game/_Scenes` and `Assets/Scenes`; project-wide settings belong in `ProjectSettings/`, and package dependencies in `Packages/manifest.json`.

Third-party assets such as Spine, DOTween, Odin Inspector, and Ingame Debug Console already have dedicated directories. Avoid editing vendor code unless upgrading or applying a documented compatibility fix. Never commit generated `Library/`, `Temp/`, `Logs/`, IDE project files, or local `UserSettings/`. Preserve every Unity `.meta` file when moving or adding assets.

## Architecture Rules

Read `.agents/rules/architecture.md` before architectural or cross-module changes. Preserve its dependency direction: UI and feature services may depend on data and shared contracts, while `Hung.Base`, `Hung.DesignPattern`, and utilities must not depend on concrete features. Update the architecture memory whenever a change intentionally alters these boundaries.

## Build, Test, and Development Commands

Open the repository through Unity Hub with editor `2022.3.62f2`, then run the game from the intended scene. Useful editor shortcuts are under `Open Scene` in Unity's menu.

Run automated tests headlessly (replace the editor path for your platform):

```sh
/path/to/Unity -batchmode -projectPath "$PWD" -runTests -testPlatform EditMode -testResults TestResults.xml -quit
/path/to/Unity -batchmode -projectPath "$PWD" -runTests -testPlatform PlayMode -testResults TestResults.xml -quit
```

Create player builds through **File > Build Settings**; no repository-specific command-line build entry point currently exists.

## Coding Style & Naming Conventions

Use C# with four-space indentation and braces on new lines. Follow existing conventions: PascalCase for types, methods, and public members; camelCase for locals and serialized fields; namespaces reflect modules such as `UI` or `Base.UI`. Keep one primary type per file and match its filename. Prefer `[SerializeField]` over making Inspector references public. Place reusable code in the appropriate `.asmdef` module and avoid introducing circular assembly references.

## Testing Guidelines

Unity Test Framework 1.1.33 is installed, but no first-party test assemblies are present. Add Edit Mode tests under `Assets/Tests/EditMode` and Play Mode tests under `Assets/Tests/PlayMode`, each with a test `.asmdef`. Name fixtures `FeatureNameTests` and tests by behavior, for example `Close_WhenAnimationCompletes_DisablesCanvas`. Cover new data logic, utilities, and regressions; manually verify scene and prefab wiring in the Editor.

## Commit & Pull Request Guidelines

Git history is unavailable in this checkout, so no established message pattern can be verified. Use short imperative subjects, optionally scoped, such as `UI: fix remove-ads popup state`. Keep commits focused and include related `.meta` files. Pull requests should explain the change, list tested scenes/platforms, link an issue when applicable, and include screenshots or video for UI, animation, or visual changes. Call out prefab, scene, package, and serialized-data migrations explicitly.
