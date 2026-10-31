# HomeCanvas mockup layout — Implementation Plan

Design: [2026-11-01-home-canvas-mockup-layout-design.md](../designs/2026-11-01-home-canvas-mockup-layout-design.md)
Branch: `feature/block-sprite-palette`

## Impacted files

| File | Change |
|---|---|
| `Assets/_Game/_UI/Scripts/SafeAreaFitter.cs` (+ `.meta`) | **new** component (`BlockDrag.UI`, namespace `UI`) |
| `Assets/_Game/_UI/Scripts/HomeCanvas.cs` | best score shown as `N0` (invariant culture) |
| `Assets/_Game/_BlockDrag/Resource/ComboBlock_UI_assets/bg_grid_tile.png` (+ `.meta`) | **new** generated sprite |
| `Assets/_Game/_BlockDrag/Resource/ComboBlock_UI_assets/panel_score.png` (+ `.meta`) | **new** generated 9-slice sprite |
| `Assets/_Game/Resources/UI/HomeCanvas.prefab` | layout rebuild |
| `Assets/Tests/EditMode/SafeAreaFitterTests.cs` (+ `.meta`) | **new** EditMode tests |

No package, gameplay, scene, or `.asmdef` changes (`EditModeTests` already references `BlockDrag.UI` — checked in step 1; added only if missing).

## Steps

### 1. `SafeAreaFitter` component

- `[RequireComponent(typeof(RectTransform))]`, `[ExecuteAlways]` not used (runtime only, avoids dirtying the prefab).
- `public static void ComputeAnchors(Rect safeArea, Vector2 screenSize, out Vector2 min, out Vector2 max)` — pure math, returns `(0,0)-(1,1)` when the screen size is zero.
- `Apply()` in `OnEnable` and in `Update` only when `Screen.safeArea` or `Screen.width/height` changed since the last apply. Sets `anchorMin/anchorMax`, keeps `offsetMin/offsetMax` at zero.
- Check `EditModeTests.asmdef` references `BlockDrag.UI`; add it if not.

### 2. EditMode tests — `SafeAreaFitterTests`

- `ComputeAnchors_FullScreen_ReturnsUnitAnchors`
- `ComputeAnchors_TopNotchAndBottomInset_ShrinksVerticalAnchors` (e.g. 1170×2532, safe area y 102→2485)
- `ComputeAnchors_ZeroScreen_ReturnsUnitAnchors`

### 3. `HomeCanvas.cs`

- `highScoreTxt.text = highScoreCounter.Displayed.ToString("N0", CultureInfo.InvariantCulture);` (also the initial `"0"` stays valid).
- No field changes.

### 4. Generate the two sprites

- Sample the background, grid line, panel fill and panel border colors from `Home@1x.png` (Unity `execute_code` reading the texture, or Python).
- `bg_grid_tile.png`: 96×96, transparent, 2 px light line on the top and left edges (grid pitch ≈ mockup 16 px × 3 → adjust to the sampled pitch). Import: Sprite, wrap Repeat, Full Rect mesh, no compression artifacts (RGBA32 or high quality).
- `panel_score.png`: ~120×120 rounded rect (radius ~36), dark fill + ~8 px darker border; import as Sprite with 9-slice borders ~44 px.

### 5. Rebuild `HomeCanvas.prefab` (via Unity MCP, prefab edit mode)

1. `Bg`: replace sprite tint with the sampled solid color (keep the stretched Image).
2. Add `BgGrid` (stretch, `Image.Type = Tiled`, `bg_grid_tile`, raycast off) right after `Bg`.
3. Add `SafeArea` (stretch, `SafeAreaFitter`) and under it:
   - `TopBar`: anchor top-stretch, height 170. Move `SettingBtn` here: anchor top-right, size 126×132, pos (−117, −114); sprite `btn_settings` (preserve aspect), hide its label text.
   - `BannerSlot`: anchor bottom-stretch, height 168, Image with the darker band color sampled from the mockup, raycast off. No placeholder text.
   - `Content`: stretch, `offsetMin.y = 168`, `offsetMax.y = −170`.
     - `LogoArea`: anchors (0.05, 0.45)–(0.95, 1). Move `Logo` in; stretch; add `AspectRatioFitter` (Fit In Parent, 1.451). Keep its `UIScaleAnim`.
     - `BottomGroup`: anchor bottom-center, pivot (0.5, 0), size 720×~620, pos y ≈ 60 (tuned so the gaps match the mockup at 1080×1920).
       - `BestLabel` (top): `ic_trophy` 75×75 + "BEST SCORE" TMP, Title font/material, white, ~60, horizontal layout centered.
       - `ScorePanel`: 630×180, `panel_score` sliced. Move `HightScoreTxt` inside: stretch, Title font/material, yellow face, ~105, centered, auto-size off.
       - `StartBtn` (bottom): size 720×210, sprite `btn_base_green`; label "PLAY", Title material, white, ~100; add `ic_play_white` icon (~66) left of the label (inside the button's `Group`, horizontal layout or fixed offsets).
4. Remove the old `HightScore` label (`TextCommon` "Hight Score").
5. Re-check `HomeCanvas` serialized refs: `startButton`, `settingButton`, `highScoreTxt` still point to the moved objects.

## Verification

1. Unity console: no compile errors after steps 1–3.
2. EditMode tests: run the full EditMode suite (`run_tests`) — all pass, including `SafeAreaFitterTests`.
3. Side-by-side render of the prefab at **1080×1920** next to `Home@1x.png`: positions/sizes of settings, logo, label, panel, play button and banner band match within a few percent.
4. Responsive screenshots at **1170×2532** (tall phone) and **1536×2048** (tablet 3:4): nothing overlaps, logo shrinks on the tablet, bottom group and banner keep size.
5. Device Simulator (iPhone with notch): settings button and banner slot sit inside the safe area; background still fills the screen.
6. Play mode from the start scene: Home opens with its animations, best score counts up with commas, Play starts the game, settings button still clickable.

Results (screenshots and test output) are reported back when done. Any deviation from the design (e.g. layout tuning that changes the hierarchy) is raised before continuing.
