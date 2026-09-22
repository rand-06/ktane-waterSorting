# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A Unity project for a *Keep Talking and Nobody Explodes* (KTaNE) mod module called **Water Sorting** (a puzzle where the player pours colored water between tubes to sort each tube into a single color). The project is a full checkout of the community ModKit fork (`Qkrisi/ktanemodkit`), with the module's own code and assets added directly under `Assets/`.

- Unity version: **2017.4.22f1** (see `ProjectSettings/ProjectVersion.txt`) — must be opened/built with this exact editor version, KTaNE mods are not forward compatible across major Unity versions.
- No package manager dependencies (`UnityPackageManager/manifest.json` is empty); third-party libraries are vendored as DLLs in `Assets/Plugins/Managed/` (`KMFramework.dll` — the base game's mod API, `KeepCoding.dll` — a community helper library, `Newtonsoft.Json.dll`).
- Steamworks integration is vendored under `Assets/Plugins/Steamworks.NET` and `Assets/Editor/Steamworks.NET` (used for Workshop upload via the ModKit editor tooling).

## Where the actual module code lives

The module's real logic is at the **top level of `Assets/`**, not inside `Assets/_Module/`:

- `Assets/waterSortingScript.cs` — the module's `MonoBehaviour` (`waterSortingScript`), attached to the module prefab. Contains all gameplay logic: tube layout, pour rules, puzzle generation/shuffling, solve detection, and settings (via `ModConfig<WaterSortingSettings>` from `Assets/Scripts/ModSettings.cs`).
- `Assets/waterComponent.cs` — near-empty marker component on the water "sector" prefab pieces used to find/tag water segments inside a tube (`GetComponentInChildren<waterComponent>()`).
- `Assets/Module.prefab` / `Assets/scene.unity` — the actual module prefab and scene used by the build.
- `Assets/Scripts/` — shared support code, mostly vendored/boilerplate from the ModKit template: `KMBombInfoExtensions.cs`, `GeneralExtensions.cs`, `ReflectionHelper.cs`, `Easing.cs`, `KMRuleSeedable.cs`, `KMColorblindMode.cs`, `KMBossModule.cs`, `ModSettings.cs` (the `ModConfig<T>` JSON settings-file helper used by the module), `AssemblyShare.cs`, `GameFixes.cs`.
- `Assets/Editor/` — ModKit editor tooling (asset bundler, mission editor, Steamworks upload, About window). `Assets/Editor/Scripts/AssetBundler.cs` is the build pipeline that packages the mod into an AssetBundle.
- `Assets/TestHarness/` — the ModKit's standalone in-editor test harness (`TestHarness.cs`) for playtesting a module without the full game; excluded from asset bundles (see `_THIS_FOLDER_IS_EXCLUDED_FROM_BUNDLES.txt`).

`Assets/_Module/` (with `_ExampleModule/`, `_Scripts/`, its own `Module.prefab`/`Module.unity`) is **leftover ModKit "Module Template" plugin boilerplate/example content** (see `CommunityPlugins/plugins.json`), not part of the shipped Water Sorting module. Don't confuse `Assets/_Module/Module.prefab` with the real `Assets/Module.prefab`.

## Building / testing

This is a Unity Editor–driven project; there is no CLI build or test suite.

- Open the project in Unity **2017.4.22f1**.
- Playtest a module in isolation via the **TestHarness** scene/prefab (`Assets/TestHarness/`) rather than the full game.
- Build the mod (AssetBundle + manifest) via the **Keep Talking ModKit** editor menu (installed as an Editor menu item by `Assets/Editor/Scripts/AssetBundler.cs` and `Init.cs`), which reads mod metadata from `ModConfig` (`Assets/Editor/Scripts/ModConfig.cs`).
- Steam Workshop upload is done through the same ModKit editor menu, backed by `Assets/Editor/Steamworks.NET` and `steam_appid.txt`/`steam_api64.dll` at the repo root.

## Module architecture notes (waterSortingScript.cs)

- Puzzle state is a `List<List<int>>` (`config`): one list per tube, each entry a color index (0 = empty water not present, colors are `1..N`). Grid size is controlled by `tubesAmount`, `emptiesAmount`, `sectors` (segments per tube), and `colors`, all read from `WaterSortingSettings` (mod settings JSON) and clamped against `MAX_TUBES`/`MAX_SECTORS`.
- `initTubesConfiguration()` builds the solved-state color multiset, then deals a Fisher-Yates shuffle of it back into the tubes (retrying until `doneShuffling()` is satisfied, i.e. not trivially already sorted). No pouring happens during generation, so a cross-color pour can never occur while building the puzzle; solvability is guaranteed because `emptiesAmount` is always clamped to at least 2 spare tubes (classic ball-sort-with-buffer guarantee). See `docs/generation-algorithm.md` for the full writeup.
- `canPour`/`pour` implement the single player-facing pour rule (also used by generation's solvability guarantee, though generation itself never calls `pour`): you can only pour the contiguous run of same-colored segments off the top of one tube into another that has room and either is empty or has a matching top color — mismatched colors can never be poured together.
- `handlePress(index)` implements the two-click tube-selection UI (select source tube, then destination tube) and triggers `checkForSolve()` → `KMBombModule.HandlePass()`.
- Mission string override: `TryOverrideMission()` parses a `[Water Sorting] tubes,empties,sectors,colors` pattern out of the bomb's mission description to let custom missions override the module's settings.
- `tubeScale`/`tubePosition` lay tubes out in a grid (rows of ~3) scaled to fit the module face, purely geometric — no gameplay logic.
- The Twitch Plays integration (`TwitchHelpMessage` field near the bottom of the file) is incomplete/stubbed — compare against `Assets/_Module/_Scripts/ModuleModule.TwitchPlays.cs` in the template if implementing it.
