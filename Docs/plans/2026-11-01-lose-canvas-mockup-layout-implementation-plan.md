# LoseCanvas mockup layout — Implementation Plan

Design: [2026-11-01-lose-canvas-mockup-layout-design.md](../designs/2026-11-01-lose-canvas-mockup-layout-design.md) (approved 2026-11-01, assumptions 1–4 confirmed)
Branch: `feature/block-sprite-palette`

## Impacted files

| File | Change |
|---|---|
| `Assets/_Game/_UI/Scripts/LoseCanvas.cs` | remove dead win path, `N0` score format |
| `Assets/_Game/_BlockDrag/Resource/ComboBlock_UI_assets/title_pill_red.png` (+ `.meta`) | **new** generated 9-slice sprite |
| `Assets/_Game/_BlockDrag/Resource/ComboBlock_UI_assets/pill_best.png` (+ `.meta`) | **new** generated 9-slice sprite |
| `Assets/_Game/Resources/UI/LoseCanvas.prefab` | layout rebuild |

No package, scene, gameplay (`BlockDrag`), `.asmdef` or test-file changes. `ScaleToFitParent`, `SafeAreaFitter`, `panel_popup`, `btn_base_green/orange` (already 9-sliced) are reused as they are.

**One simplification vs. the design:** `HomeCanvas` already formats its high score with `ToString("N0", CultureInfo.InvariantCulture)` inline. `LoseCanvas` uses the same one-liner instead of a new `ScoreFormat` helper, so no new helper and no new test file (a one-line framework call has nothing of ours to test). Result on screen is identical ("1,240").

## Steps

### 1. `LoseCanvas.cs`

- Remove: `nextLevelButton`, `blockGoldTxt`, `loadingTimes`, `loadingActions`, `loadingAction2s`, `gameData`, `gameplayData`, `isWin`, `topCanvas`, `BASE_GOLD`, `isNextLevel`, `OnNextLevel`, and the usings only they needed (`Quang.Data`, `Quang.Utilities.Timer`, `System`, `System.Data`, `System.Collections`, `Gameplay.BlockDrag` if unused).
- `Awake`: only subscribe `tryAgainButton` / `goHomeButton`; `OnDestroy` mirrors it.
- `Open(object param)`: `base.Open(param); ShowScore();` — no cast of `param`, no button toggling (Home is always active in the prefab).
- `ShowScore`: `highScoreTxt.text = highScore.ToString("N0", CultureInfo.InvariantCulture)`; the tween setter writes `scoreTxt.text = v.ToString("N0", CultureInfo.InvariantCulture)`. Add `using System.Globalization;`.
- `OnTryAgain` / `OnGoHome` unchanged.
- `read_console` → no compile errors (the prefab still has the now-removed serialized fields; Unity just drops them on next save).

### 2. Generate the two sprites

Snippet 3 of the skill (`render-snippets.md`) adapted, saved in `ComboBlock_UI_assets/` next to `title_pill.png`:

- `title_pill_red.png` 240×192: `#FF4D5E` fill, `#D12A3C` bottom band 18 px, `#1C1A3A` border 9, radius 42, `#FFA6AF` highlight bar (~75×15, rounded) at top-left inside the border. Border = (51, 51, 51, 51) → highlight sits in the left slice so it does not stretch.
- `pill_best.png` 108×108: `#F1EEFB` fill, `#1C1A3A` border 9, radius 54 (full). Border = (54, 54, 54, 54) → stays a pill at 492×108.
- Importer: Sprite, Full Rect, no mipmaps, uncompressed, Clamp.

### 3. Rebuild `LoseCanvas.prefab`

Close any open Prefab Mode first. One `LoadPrefabContents` script (kept in `execute_code` history). Positions are relative to `Popup` (900×1170, top-anchored children, `y` = −distance from panel top):

1. `Dimed`: color black α 0.58.
2. Create `SafeArea` (stretch, `Quang.UI.SafeAreaFitter`) → `PopupArea` (stretch, `offsetMin.y = 168`) → `PopupFit` (centre, 960×1300, `ScaleToFitParent` designSize 960×1300). Move `Popup` under `PopupFit`: centre, 900×1170, `anchoredPosition = (0, −56)`, keep `UIScaleAnim`.
3. Delete `Background`, `BestScore`, `Content` (with `Text_Info`, `Item`), `GroupButton/NextLevel`.
4. `Panel` (new, first child): stretch, `panel_popup` sliced.
5. `Title` (rename `TitleLevel`, last sibling so it draws over the panel): top-centre, 774×192, pivot top, `y = +111`; replace `Frame` sprite with `title_pill_red` sliced; `LevelTxt` → `TitleTxt` "GAME OVER".
6. `Subtitle` (new TMP): top-stretch L/R 75, height 60, centre `y = −183`, "No more moves left", `#6B6690`, 42.
7. `ScoreTitle` (reuse): same anchoring, centre `y = −279`, "SCORE", `#1C1A3A`, 40, character spacing ~8.
8. `ScoreTxt` (reuse, = `scoreTxt`): height 180, centre `y = −414`, Title font/material, white, 150.
9. `BestPill` (new): top-centre 492×108, centre `y = −582`, `pill_best` sliced, `HorizontalLayoutGroup` middle-centre spacing 18, control size on, expand off → `Icon` (`ic_trophy`, `LayoutElement` 54×54), `BestLabel` "BEST", `BestScoreTxt` (reuse, = `highScoreTxt`), both `#1C1A3A` 42, auto width.
10. `Buttons` (rename `GroupButton`): bottom-stretch, L/R 75, bottom 69, `VerticalLayoutGroup` spacing 33 (replaces `HorizontalLayoutGroup`), control size off, child alignment lower-centre, height 393.
    - `TryAgainButton` (= `tryAgainButton`) 750×189, `btn_base_green` sliced; `Group` gets `HorizontalLayoutGroup` middle-centre spacing 18 → `Icon` `ic_restart_white` 66 + `Text` "PLAY AGAIN" 72.
    - `HomeButton` (= `goHomeButton`) set **active**, 750×171, `btn_base_orange`; `Group` → `Icon` `ic_home_white` 60 + `Text` "HOME" 66. Order: TryAgain above Home.
11. All texts: TMP margin 0, `CenterGeoAligned`, wrap off, auto-size off, fontStyle Normal. Title-material texts (`TitleTxt`, `ScoreTxt`, button labels): `Fredoka-Bold Title SDF` + Title Material, `FontMaterialProp` disabled. Others: `Fredoka-Bold Title SDF` with its plain atlas material (the SettingPopup note: the `Fredoka-Bold SDF` default material has a blue outline).
12. Remove `AtlasSprite` components on any reused node whose sprite changes (it re-applies old atlas sprites in `Awake`).
13. Save, `AssetDatabase.SaveAssets()`, confirm the file contains `m_Name: BestPill` and no `m_Name: NextLevel`.

Canvas sorting: layer `UI` (id 2946559511) order 50 — exists, kept.

### 4. Render & iterate

- Snippet 4 at 1080×1920, 1170×2532, 1536×2048, 1080×2400. Compare 1080×1920 with the mockup side by side; tweak font sizes/offsets and re-run step 3 until within a few %.
- Refs check: `tryAgainButton`, `goHomeButton`, `scoreTxt`, `highScoreTxt` → `TryAgainButton`, `HomeButton`, `ScoreTxt`, `BestScoreTxt`.

## Verification

1. `read_console`: no compile errors after step 1.
2. EditMode tests: suite unchanged (the 6 `GameOverCheckTests` SetUp failures are pre-existing — report only).
3. Re-check `LoseCanvas.prefab` on disk after the test run (contains `BestPill`).
4. Play mode from `LoadStart`: play until no move fits (or trigger game over) → LoseCanvas opens with dim + scale-in, score counts up with `,` separator, BEST shows high score; Play Again → board resets, drag works; lose again → Home → HomeCanvas opens, Play starts a new board. Console clean after stop.
5. Device Simulator with a notched iPhone and an iPad: popup centred, nothing under the notch, not over the banner slot (manual check; cannot be scripted).
6. Commit only the files in *Impacted files*; leave TMP font-atlas diffs out.

## Implementation notes (2026-11-01)

Deviations found while implementing and verifying:

- **Popup position**: `Popup` at `y = −81` (not −56) and `PopupFit` / `ScaleToFitParent` design box **960×1340**, so at 1080×1920 the panel sits where the mockup has it (top 372, bottom 1542). Scale stays 1 on all four test resolutions (iPad area 1495 tall).
- **`title_pill_red` 9-slice border** = (L 120, B 51, R 51, T 51) so the top-left highlight stays inside the left slice and is not stretched.
- **Dark text colors are stored as linear values** (`Color.linear` of `#1C1A3A` / `#6B6690`) on `Subtitle`, `ScoreTitle`, `BestLabel`, `BestScoreTxt`: the project is in Linear color space and the `TextMeshPro/Mobile/Distance Field` shader of the font's default material uses vertex color as-is, so `#1C1A3A` displayed as ~`#5D5A83`. Rendered colors now measure `#1C1C3B` / `#6A6690`. The SettingPopup row labels have the same lighter look (out of scope).
- Prefab was found reverted on disk after the EditMode run although no Prefab Mode stage was open; the build script was re-applied and the file re-checked.

Verification done: compile clean; EditMode 190 tests — only the 6 pre-existing `GameOverCheckTests` SetUp failures. Renders at 1080×1920, 1170×2532, 1536×2048, 1080×2400: no overlap, scale 1. Refs: `tryAgainButton`, `goHomeButton`, `scoreTxt`, `highScoreTxt` intact. Play mode from `LoadStart`: Start → game over (via `GameplayCanvas.HandleGameOver`) → LoseCanvas opens, BEST shows `102,153`; Play Again → gameplay, drag enabled; game over → Home → HomeCanvas open, drag locked. Console clean. Notch check in Device Simulator still to do manually.
