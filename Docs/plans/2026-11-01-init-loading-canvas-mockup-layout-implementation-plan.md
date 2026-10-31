# InitLoadingCanvas mockup layout — Implementation Plan

Design: [2026-11-01-init-loading-canvas-mockup-layout-design.md](../designs/2026-11-01-init-loading-canvas-mockup-layout-design.md)
Branch: `feature/block-sprite-palette`

## Impacted files

| File | Change |
|---|---|
| `Assets/_Game/_UI/Scripts/InitLoadingCanvas.cs` | label text via a small static formatter, `"LOADING... NN%"` |
| `Assets/Tests/EditMode/InitLoadingCanvasTests.cs` (+ `.meta`) | **new** EditMode tests for the formatter |
| `Assets/_Game/_BlockDrag/Resource/ComboBlock_UI_assets/bar_track.png` (+ `.meta`) | **new** generated 9-slice sprite |
| `Assets/_Game/_BlockDrag/Resource/ComboBlock_UI_assets/bar_fill.png` (+ `.meta`) | **new** generated 9-slice sprite |
| `Assets/_Game/Resources/UI/InitLoadingCanvas.prefab` | layout rebuild |

No package, scene, gameplay or `.asmdef` changes (`EditModeTests` already references `BlockDrag.UI`).

## Steps

### 0. Prep

- Close Prefab Mode if open (avoid the stale-stage revert pitfall).
- Confirm console has no errors.

### 1. `InitLoadingCanvas.cs`

- Add `public static string FormatLoadingText(float value)` → `$"LOADING... {(int)(Mathf.Clamp01(value) * 100)}%"`.
- `SetPercentage` uses it: `loadingText.text = FormatLoadingText(value);`.
- No field or other API changes.

### 2. EditMode tests — `InitLoadingCanvasTests`

- `FormatLoadingText_Zero_ShowsZeroPercent` → `"LOADING... 0%"`
- `FormatLoadingText_Fraction_TruncatesPercent` → `0.657f` → `"LOADING... 65%"`
- `FormatLoadingText_OutOfRange_Clamps` → `-1f` → `0%`, `2f` → `100%`

### 3. Generate the bar sprites (`execute_code`, snippet-3 style)

- `bar_track.png`: 120×120 (canvas units), fully rounded (radius 45 on the 90-high body), fill `#1C1A3A`, bottom 9 px band `#432E8E` drawn as an offset copy behind the body. Import: Sprite, Full Rect, uncompressed, borders L/R 48, T/B 48 (keeps round ends at 90 high).
- `bar_fill.png`: 60×54, fully rounded (radius 27), top `#FFC83D`, lower 15 px `#E0A100`. Import: Sprite, Full Rect, uncompressed, borders L/R 27, T/B 0.
- Exact border numbers adjusted after the first render if the ends look clipped.

### 4. Rebuild `InitLoadingCanvas.prefab` (one `LoadPrefabContents` script, kept in `execute_code` history)

1. `Bg`: colour `#6236E0`, same sprite setup as Home's `Bg`.
2. Add `BgGrid` after `Bg`: stretch, `bg_grid_tile`, `Image.Type = Tiled`, raycast off.
3. Add `SafeArea` (stretch, `Quang.UI.SafeAreaFitter`) last in the root:
   - `LogoArea`: stretch, `offsetMin = (67, 570)`, `offsetMax = (−67, −330)`. Move `Logo` in, stretch it, add `AspectRatioFitter` (Fit In Parent, 3700/2550). Keep `UIScaleAnim`.
   - `BottomGroup`: anchor bottom-centre, pivot (0.5, 0), 780×201, pos y 312 — spans 312→513 above the safe-area bottom (label centre 357, bar centre 468, bar top 513).
     - Move `Slider_LoadingBar` in: anchor top-centre, 780×90, pos y −45 (centre); `bar_track` sliced; `Slider.interactable = false`, `transition = None`, no handle.
       - Add `Fill Area` (stretch, insets 18 on all sides) and reparent `Fill` into it; `Fill` stretch-y, `bar_fill` sliced; `Slider.fillRect = Fill`.
     - Move `Text (TMP)` out of the slider into `BottomGroup`, rename `LoadingText`: anchor bottom-centre, 600×90, centre y 45 from group bottom (label occupies the group's lower 90); `Fredoka-Bold Title SDF` + Title material, white face, font ~66, `margin = 0`, `CenterGeoAligned`, wrap off, auto-size off, style Normal; disable `FontMaterialProp` if present.
4. Save, unload, assert the file on disk contains `m_Name: LoadingText` and `m_Name: SafeArea`.

### 5. Render and tune

- Render the prefab at 1080×1920, 1170×2532, 1536×2048 (snippet 4) with the bar at 65 %, plus 1080×1920 at 0 % and 5 %.
- Compare 1080×1920 with `Loading@1x.png` (logo box, bar box, label size/position within a few %); tune font size, sprite borders and offsets; re-render.

## Verification

- [ ] Console clean after the script change.
- [ ] EditMode tests pass (`InitLoadingCanvasTests`; known `GameOverCheckTests` SetUp failure is pre-existing and reported, not chased).
- [ ] Prefab on disk still has the new layout after the test run.
- [ ] `SerializedObject` check: `loadingText` → `LoadingText`, `loadingBar` → `Slider_LoadingBar`.
- [ ] Renders: 1080×1920 matches the mockup; tall phone and iPad have no overlap; fill looks acceptable at 0 % / 5 %.
- [ ] Play mode from `LoadStart`: bar fills 0→100 %, label reads "LOADING... NN%", GameScene opens and the canvas dismisses.
- [ ] Notch: Device Simulator with a notched iPhone (manual — cannot be checked by script).
- [ ] Commit only intended files; leave `Fredoka-Bold SDF.asset` / `Title SDF.asset` atlas diffs out.
