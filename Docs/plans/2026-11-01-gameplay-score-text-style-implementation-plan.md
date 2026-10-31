# Gameplay score text style — Implementation Plan

Design: [2026-11-01-gameplay-score-text-style-design.md](../designs/2026-11-01-gameplay-score-text-style-design.md) (approved)

## Impacted files

| File | Change |
|---|---|
| `Assets/_Game/_UI/UIResources/UI/Font/Fredoka-Bold Title SDF.asset` (+ `.meta`) | **New** font asset. Atlas texture and default material are sub-assets |
| `Assets/_Game/_UI/UIResources/UI/Font/Fredoka-Bold Title Material.mat` (+ `.meta`) | **New** material preset (outline + underlay) |
| `Assets/_Game/Resources/UI/GameplayCanvas.prefab` | Overrides on `TopCenter/CurrentScore` and `HeadLeft/HightScore/Score` |

Not touched: `Fredoka-Bold SDF.asset`, its materials, any other prefab, any code.

All Editor work goes through UnityMCP (`execute_code`, TMP 3.0.7 API). Nothing is saved to scenes.

## Steps

1. **Create the font asset**
   - `TMP_FontAsset.CreateFontAsset(Fredoka-Bold.ttf, 90, 16, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false)`.
   - Save it the way TMP's own editor does: `AssetDatabase.CreateAsset` for the font asset, then `AddObjectToAsset` for the atlas texture (`"Fredoka-Bold Title Atlas"`) and the default material (`"Fredoka-Bold Title Atlas Material"`).
   - Copy `m_ClearDynamicDataOnBuild` and the face info family/style from `Fredoka-Bold SDF`, so it behaves the same in builds.
   - Pre-add the glyphs for `0123456789,.` with `TryAddCharacters`. Save and import.

2. **Create the material preset**
   - Copy the new default material to `Fredoka-Bold Title Material.mat`, keeping its shader (`TextMeshPro/Distance Field`) and `_MainTex` = new atlas.
   - Set starting values: `_FaceColor` white, `_FaceDilate` 0.2, `OUTLINE_ON` with `_OutlineWidth` 0.2 and `_OutlineColor` = dark navy sampled from the mockup outline pixels, `UNDERLAY_ON` with `_UnderlayColor` = same navy (alpha 1), `_UnderlayOffsetX` 0, `_UnderlayOffsetY` -0.8, `_UnderlayDilate` 0.3, `_UnderlaySoftness` 0.

3. **Update the prefab** (`PrefabUtility.LoadPrefabContents` → edit → `SaveAsPrefabAsset` → unload)
   - `CurrentScore`: `font` = Title SDF, `fontSharedMaterial` = Title Material, `fontStyle` = Normal, `fontSize` ~100, `characterSpacing` ~-6.
   - `HightScore/Score`: same font/material/style, `fontSize` ~36, `characterSpacing` ~-6, keep its yellow color.

4. **Tune by eye**
   - Render the prefab off-screen at 720×1280 (preview scene + camera, like the earlier check) and build a crop comparing it with the mockup.
   - Adjust size, spacing, dilate, outline and underlay values until glyph height, letter spacing, stroke weight and shadow offset match. Fix anchors/positions only if the score is clearly out of place after resizing.
   - Repeat (expected 2–4 rounds).

5. **Check the console** with `read_console` for TMP or import errors.

## Verification

- **Visual match:** final side-by-side crop (mockup on top, Unity below), shown to you.
- **Edges:** a 2× zoomed crop of "1,240" shows no clipped outline or underlay and no square artifacts around the glyphs.
- **Long value:** a temporary render with `CurrentScore` = `"1,234,567"` (not saved) shows the text still fits.
- **No side effects:**
  - `git status` lists only the 2 new assets (+ `.meta`) and `GameplayCanvas.prefab`.
  - `git diff` on `GameplayCanvas.prefab` shows only the font/material/style/size/spacing (and possibly position) overrides of the two texts.
  - `Fredoka-Bold SDF.asset` gets no new diff from this task.
- **Manual (your part):** enter Play Mode, play a few moves, and confirm the score updates and looks right on device resolution.
