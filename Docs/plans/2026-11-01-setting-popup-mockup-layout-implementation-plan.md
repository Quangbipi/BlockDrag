# SettingPopup mockup layout — Implementation Plan

Design: [2026-11-01-setting-popup-mockup-layout-design.md](../designs/2026-11-01-setting-popup-mockup-layout-design.md)
Branch: `feature/block-sprite-palette`

## Impacted files

| File | Change |
|---|---|
| `Assets/_Game/_UI/Scripts/Component/ScaleToFitParent.cs` (+ `.meta`) | **new** component (`BlockDrag.UI`, namespace `UI`) |
| `Assets/Tests/EditMode/ScaleToFitParentTests.cs` (+ `.meta`) | **new** EditMode tests (`EditModeTests` already references `BlockDrag.UI`) |
| `Assets/_Game/_UI/Scripts/SettingPopup.cs` | restart button, direct Home, remove old confirm/loading path |
| `Assets/_Game/_UI/Scripts/HomeCanvas.cs` | `OnSetting` opens `SettingPopup` state 0 |
| `Assets/_Game/_UI/Scripts/GameplayCanvas.cs` | `OnSettingBtnClick` opens `SettingPopup` state 1 |
| `Assets/_Game/_UI/Scripts/GameSceneFlow.cs` | drag input locked while Home **or** SettingPopup is open |
| `Assets/_Game/_BlockDrag/Resource/ComboBlock_UI_assets/panel_popup.png` (+ `.meta`) | **new** generated 9-slice sprite |
| `Assets/_Game/_BlockDrag/Resource/ComboBlock_UI_assets/title_pill.png` (+ `.meta`) | **new** generated 9-slice sprite |
| `.../ComboBlock_UI_assets/buttons/btn_base_blue.png.meta`, `btn_base_orange.png.meta` | 9-slice border 70 + Full Rect (same as green) |
| `Assets/_Game/Resources/UI/SettingPopup.prefab` | layout rebuild |

No package, scene, gameplay (`BlockDrag`) or `.asmdef` changes.

## Steps

### 1. `ScaleToFitParent` component

- `[RequireComponent(typeof(RectTransform))]`, `[SerializeField] Vector2 designSize = new Vector2(984, 1290)`.
- `public static float ComputeScale(Vector2 available, Vector2 design)` → `min(1, available.x / design.x, available.y / design.y)`; returns 1 when `design` has a zero component or `available` is zero (layout not ready).
- `Apply()` in `OnEnable`, and from `OnRectTransformDimensionsChange` on itself plus a cheap check in `LateUpdate` when the parent's `rect.size` changed (parent size changes don't always notify the child). Sets `localScale = Vector3.one * scale`.
- Node's own `sizeDelta` = `designSize`, anchored centre, so children lay out against the design box.

### 2. EditMode tests — `ScaleToFitParentTests`

- `ComputeScale_AvailableLargerThanDesign_ReturnsOne`
- `ComputeScale_NarrowerThanDesign_ScalesByWidth` (979×1830 vs 984×1290 → 979/984)
- `ComputeScale_ShorterThanDesign_ScalesByHeight` (1247×1000 → 1000/1290)
- `ComputeScale_ZeroAvailable_ReturnsOne`

### 3. `SettingPopup.cs`

- Add `[SerializeField] UIButton restartButton;` subscribe/unsubscribe in `Awake` / `OnDestroy` like the others.
- `OnRestartButtonClick` → `Close(); Locator.BoardSession?.RestartGame();`
- `OnHomeButtonClick` → `Close(); Locator.UI.OpenUI<HomeCanvas>();`
- Delete `loadingTimes`, `loadingActions`, `confirmActions`, `OnGoHome` (no longer used).
- `SetupState`: keep showing/hiding `bottomGroup`; after it, `LayoutRebuilder.ForceRebuildLayoutImmediate` on the panel so the height updates the same frame. Drop the separate `SetActive` on `homeButton` / `resumeButton` (they live inside `bottomGroup`).

### 4. Open the popup

- `HomeCanvas.OnSetting` → `Locator.UI.OpenUI<SettingPopup>(0);`
- `GameplayCanvas.OnSettingBtnClick` → `Locator.UI.OpenUI<SettingPopup>(1);`

### 5. `GameSceneFlow.cs` — block dragging under the popup

- Fields `bool homeOpen, settingOpen; SettingPopup settingPopup;`
- `Start`: next to the Home preload, `settingPopup = Locator.UI.GetUI<SettingPopup>();` (preloads inactive) and subscribe `_OnOpen` / `_OnClose` → set `settingOpen`, call `RefreshInput()`.
- Home handlers set `homeOpen` instead of calling `SetInputEnabled` directly; the initial lock sets `homeOpen = true`.
- `RefreshInput()` → `SetInputEnabled(!homeOpen && !settingOpen)`. Unsubscribe in `OnDestroy`.
- The early-exit paths (`Locator.UI == null`, `homeCanvas == null`) keep their current behaviour.

### 6. Generate the two sprites

Snippet 3 of the skill (`render-snippets.md`) adapted:

- `panel_popup.png` 180×180: white fill, `#1C1A3A` border 9, radius 42, `#E3DEF5` band on the inner bottom 12 px, dark shadow 18 px below inside the texture. Border = (51, 69, 51, 51).
- `title_pill.png` 240×150: `#A855F7` fill, `#7E2BD1` bottom 24 px, `#1C1A3A` border 9, radius 42. Border = (51, 51, 51, 51).
- Importer: Sprite, Full Rect, no mipmaps, uncompressed, Clamp.
- `btn_base_blue` / `btn_base_orange`: set border (70, 70, 70, 70) + Full Rect, reimport.

### 7. Rebuild `SettingPopup.prefab`

Close any open Prefab Mode first. One `LoadPrefabContents` script (kept in `execute_code` history):

1. `Bg`: color black α 0.54.
2. Create `SafeArea` (stretch, `Quang.UI.SafeAreaFitter`) → `PopupArea` (stretch, `offsetMin.y = 168`) → `PopupFit` (centre, 984×1290, `ScaleToFitParent`). Move existing `Popup` under `PopupFit`, set it centre 984×1290, remove its `AspectRatioFitter`, keep `UIScaleAnim`.
3. Rename `Broad` → `Panel`: anchored top-centre at `y = -84` (title overhang), width 900, `Image` `panel_popup` sliced, `VerticalLayoutGroup` (padding L/R 75, top 141, bottom 93 — includes the 18 shadow; spacing 0; control child size on, expand off) + `ContentSizeFitter` vertical Preferred. Remove old `Frame`.
4. Rows: reuse `SwitchToggle`, `(1)`, `(2)` as `RowSound`, `RowMusic`, `RowVibration`: `LayoutElement` height 180, `HorizontalLayoutGroup` (middle-left, spacing 36, control size off). Icon 105×105 (`ic_sound/music/vibration`), label `TextDisplay` flexible width, `Fredoka-Bold SDF` 60, `#1C1A3A`, middle-left, margin 0. `Toggle`: 180×99, `toggle_on` sprite, delete `Handle`; `ImageStateButtonComponent` data → one entry: `Image` = toggle image, `Sprites` = [toggle_off, toggle_off, toggle_on, toggle_off], clear `Positions`/`Sizes`.
5. `Divider1`, `Divider2` between rows: `Image` `#EEEAF8`, `LayoutElement` height 6.
6. Spacer (`LayoutElement` height 75) then `Bot` (= `bottomGroup`): `VerticalLayoutGroup` spacing 51 →
   - `ResumeButton` 750×171: `btn_base_green` sliced; `Group` `HorizontalLayoutGroup` centre, `ic_play_white` 66 + "RESUME" Title 84.
   - `BotRow` (`HorizontalLayoutGroup` spacing 42): `RestartButton` (duplicate of `ResumeButton` structure, `btn_base_blue`, `ic_restart_white` 54, "RESTART" 60) and `BtnHome` (`btn_base_orange`, `ic_home_white` 54, "HOME" 60), 354×165 each. `RestartButton` gets its own `UIButton` (index 0, `button` ref to its own `Button`).
7. `Title` (moved out of `Panel` to `Popup`, after `Panel` in sibling order): anchored top-centre, 600×150, `title_pill` sliced, `y = -12`. `SettingText` "SETTINGS", Title font/material, 72, `CenterGeoAligned`, margin 0, `FontMaterialProp` disabled, centred.
8. `PopupCloseButton`: moved to `Popup`, `btn_close` 126×129, anchored to the panel's top-right corner position (`x = +450 + 6`, top `-84 + 18` relative to `Popup` top-centre), hide its old `Icon` child.
9. Delete `BtnExit (1)`, `BasicButton (1)`, `BasicButton (2)`, `BtnPolicy`, `Padding`, `Scroll View`, `VersionTxt`, root `Image`.
10. Wire `restartButton` via `SerializedObject`; re-check `closeButton`, `backgroundCloseButton`, `resumeButton`, `homeButton`, `bottomGroup`, `functionButtons[0..2]`.
11. Save, `AssetDatabase.SaveAssets()`, confirm `m_Name: RestartButton` is in the file.

Note: `PopupCloseButton` / `Title` are positioned against `Popup`, while the panel grows downward from a fixed top, so both stay on the panel's top edge in state 0 and 1.

### 8. Render & iterate

- Snippet 4 at 1080×1920, 1170×2532, 1536×2048 (plus 1080×2400), once with `bottomGroup` on (state 1) and once off (state 0). Compare 1080×1920 with the mockup side by side; adjust and re-run step 7 until positions/sizes are within a few %.
- Refs check: `closeButton`, `backgroundCloseButton`, `resumeButton`, `homeButton`, `restartButton`, `bottomGroup`, `functionButtons`.

## Verification

1. `read_console`: no compile errors after steps 1–5.
2. EditMode tests: `ScaleToFitParentTests` pass; rest of the suite unchanged (`GameOverCheckTests` SetUp failure is pre-existing — report only).
3. Re-check `SettingPopup.prefab` on disk after the test run (contains `RestartButton`, no `Scroll View`).
4. Play mode from `LoadStart`:
   - Home → settings button → popup shows 3 toggles only; toggles flip sprites and change sound/music/haptic; close (× and tap outside) → Home still there, blocks still not draggable.
   - Play → gameplay settings button → full popup; **try dragging a block while it is open → nothing moves**; Resume → drag works again.
   - Restart → popup closes, board resets, score 0, drag works.
   - Home → Home opens, drag locked; Play → same board continues.
   - Console clean after stop.
5. Device Simulator with a notched iPhone and an iPad: popup centred, nothing under the notch, not over the banner slot (manual check in the Editor; cannot be scripted).
6. Commit only the files in *Impacted files*; leave TMP font-atlas diffs out.

## Implementation notes (2026-11-01)

Deviations found while implementing and verifying:

- **Panel anchoring**: `Panel` is centred in the 984×1290 box (`y = -36`) instead of top-anchored, and `Title` / `PopupCloseButton` are children of `Panel` with `LayoutElement.ignoreLayout`, so the shorter state-0 panel stays centred on screen and both pieces stay on its top edge.
- **Measured spacing** used instead of the plan's estimates: rows 174 + dividers 6 (pitch 180), top padding 144, spacer before Resume 66, bottom padding 81, panel top 75 below the box top.
- **`SettingPopup.panel`** serialized field added for the layout rebuild in `SetupState`.
- **Row labels** use `Fredoka-Bold Title SDF` with its plain atlas material (the `Fredoka-Bold SDF` default material has a blue outline/underlay).
- **`AtlasSprite` removed** from `PopupCloseButton` (and its hidden icon): it re-applies the old atlas sprite in `Awake` even when disabled.
- **Canvas sorting layer**: the popup used sorting-layer ID `-1704152343`, which does not exist in this project, so it never rendered. Set to `UI`, order 100 (Home 11, Gameplay 10, Lose 50). Other legacy popups still use that ID — out of scope.
- **`Locator.Audio` is null** in the current game, so `OnFunctionButtonClick` threw before `UpdateUI`; changed to `Locator.Audio?.` (same as `BasePopup`).
- `FontMaterialProp` lives in namespace `Quang.Utilities` (assembly `Quang.UI`).

Verification done: EditMode 187 tests — only the 6 pre-existing `GameOverCheckTests` SetUp failures; `ScaleToFitParentTests` pass. Renders at 1080×1920, 1170×2532, 1536×2048, 1080×2400 in both states. Play mode from `LoadStart`: Home → popup (toggles only, all 3 toggles flip data + sprite, × closes, drag stays locked) → Play (drag unlocked) → Gameplay settings (full popup, drag locked) → Resume (unlocked) → Restart (`OnGameRestarted` fired, unlocked) → Home (Home open, locked). Console clean. Notch check in Device Simulator still to do manually.
