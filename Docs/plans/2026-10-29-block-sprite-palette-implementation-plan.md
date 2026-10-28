# Block sprite palette — Implementation Plan

Design: [2026-10-29-block-sprite-palette-design.md](../designs/2026-10-29-block-sprite-palette-design.md) (approved, including section 8)
Branch: `feature/block-sprite-palette`

## Impacted files

| File | Change |
|---|---|
| `Assets/_Game/_BlockDrag/Data/BlockSkin.cs` (+ `.meta`) | **New.** `[Serializable] struct BlockSkin { Sprite sprite; Color color; HasSprite }` + `SameAs(BlockSkin)` |
| `Assets/_Game/_BlockDrag/Data/BlockColorPaletteSO.cs` | Add `List<BlockSkin> sprites`, `UseSprites`, `TryGetRandomSkin(exclude, out skin)`. `TryGetRandomColor` is unchanged |
| `Assets/_Game/_BlockDrag/BlockShape.cs` | `Skin` property, `Initialize(..., BlockSkin skin)` overload (old `Color?` overload forwards), apply sprite before scaling, `SetDimmed` base white in sprite mode |
| `Assets/_Game/_BlockDrag/BlockSpawner.cs` | `usedColors` → `usedSkins`, `PickColor` → `PickSkin`, call the new `Initialize` |
| `Assets/_Game/_BlockDrag/BlockGrid.cs` | Placed block: `ApplyScale(blockObj, cellSize)` instead of `CellScale`. Indicator: cache the default sprite, assign skin sprite or default + tint, scale from its sprite. Event `OnClearPreviewChanged(LineClearResult, BlockSkin)`, `SetClearPreview` dedup by skin, `GetShapeColor` → `GetShapeSkin` |
| `Assets/_Game/_BlockDrag/Effects/LineClearPreviewEffect.cs` | Handler/`Show` take `BlockSkin`; `TintedBlock` stores `OriginalSprite` + `OriginalScale`; `ApplyTint` swaps sprite (sprite mode) or tints (color mode); `RestoreTint` restores sprite, color, scale |
| `Assets/_Game/_Data/DataSO/BlockColorPalette.asset` | Fill `sprites` with the 7 `tile_*` sprites + representative colors. `colors` unchanged |
| `Assets/_Game/_BlockDrag/Resource/ComboBlock_UI_assets/board_Frame.png.meta` | `spriteBorder` L56/B72/R56/T56 (Unity order x=L, y=B, z=R, w=T), `spritePixelsToUnits` ≈ 180, `meshType: FullRect` (needed for Sliced) |
| `Assets/_Game/_BlockDrag/Prefabs/GridCellPrefab.prefab` | `m_Sprite` → `tiles/cell_empty.png` |
| `Assets/_Game/_BlockDrag/Prefabs/BoardFramePrefab.prefab` | `m_Sprite` → `board_Frame.png` |
| `Assets/_Game/_Scenes/Game/GameScene.unity` | 64 baked cells + 1 frame: `m_Sprite` GUID swap (only those lines) |
| `Assets/Tests/EditMode/BlockColorPaletteTests.cs` | New tests for `TryGetRandomSkin` |
| `Assets/Tests/EditMode/BlockSkinTests.cs` (+ `.meta`) | **New.** Tests for `HasSprite` / `SameAs` |
| Untracked `ComboBlock_UI_assets/` (+ `.meta`) | Committed with this branch, since prefabs/scene now reference it |

`BlockSkin.cs` goes into `_BlockDrag/Data/`, which is compiled into `Quang.Data` through the existing asmref, the same assembly as `BlockColorPaletteSO`. No asmdef/asmref changes.

## Steps

### A. Data
1. Create `BlockSkin.cs` (namespace `Gameplay.BlockDrag`):
   - Fields `sprite`, `color`; `HasSprite => sprite != null`.
   - `static BlockSkin FromColor(Color c)`.
   - `bool SameAs(BlockSkin other)`: same sprite if both have one, otherwise compare RGB with `Mathf.Approximately` (same rule as the palette's `Contains`).
2. `BlockColorPaletteSO`:
   - `[SerializeField] private List<BlockSkin> sprites = new List<BlockSkin>();`
   - `public bool UseSprites`: true if there is at least one entry with a sprite.
   - `TryGetRandomSkin(IReadOnlyList<BlockSkin> exclude, out BlockSkin skin)`: if `UseSprites`, random over entries with a sprite, preferring those not `SameAs` anything in `exclude`; else wrap `TryGetRandomColor` (exclude converted to colors). Force `color.a = 1`.

### B. BlockShape
3. Add `public BlockSkin Skin { get; private set; }`; `ShapeColor` becomes `=> Skin.color` (getter only; no external setter today).
4. New `Initialize(BlockShapeData, GameObject, BlockRotation, float, BlockSkin)`. The existing `Initialize(..., Color? color = null)` forwards with `BlockSkin.FromColor(color ?? data.shapeColor)`.
5. In the child loop, before `ApplyBlockScale`:
   - Sprite mode: `sr.sprite = Skin.sprite; sr.color = Color.white`.
   - Color mode: `sr.color = ShapeColor` (as today).
   - Move the color assignment before scaling (scaling reads `sprite.bounds`).
6. `SetDimmed`: `Color baseColor = Skin.HasSprite ? Color.white : ShapeColor;` then the same formula.

### C. BlockSpawner
7. `usedColors` → `usedSkins` (`List<BlockSkin>`); collect `existing.Skin`.
8. `PickColor` → `PickSkin`: palette `TryGetRandomSkin` → fallback `BlockSkin.FromColor(data.shapeColor)` (same warning log).
9. Call `shapeComp.Initialize(..., skin)`.

### D. BlockGrid
10. Placing (~L770): replace `blockObj.transform.localScale = CellScale` with `ApplyScale(blockObj, cellSize)`.
11. Indicator:
    - Cache `defaultIndicatorSprite` from the first created indicator's renderer.
    - In `ShowIndicator`, per cell: `sr.sprite = skin.HasSprite ? skin.sprite : defaultIndicatorSprite`; color = `HasSprite ? white : skin.color`, alpha `indicatorAlpha` / `clearPreviewGhostAlpha`; scale with `ApplyScale(indObj, cellSize)` instead of `CellScale`.
12. Clear preview: `event Action<LineClearResult, BlockSkin> OnClearPreviewChanged`; field `clearPreviewColor` → `clearPreviewSkin`; dedup uses `SameAs`; `GetShapeColor` → `GetShapeSkin` (null shape → `FromColor(Color.white)`). `HideIndicator`/`ClearGrid` pass `default`.

### E. LineClearPreviewEffect
13. `HandleClearPreviewChanged(LineClearResult, BlockSkin)`, `Show(LineClearResult, BlockSkin)`.
14. `TintedBlock` + `OriginalSprite`, `OriginalScale`.
15. `ApplyTint`: sprite mode → `sr.sprite = skin.sprite`, `sr.color = (1,1,1, sr.color.a)`, `BlockGrid.ApplyScale(block, blockGrid cell size)`; color mode → current tint code.
16. `RestoreTint`: restore `sprite`, `color`, `localScale` (same guard as today for blocks being cleared).
    - Needs `BlockGrid` to expose the cell size: use the existing public getter if there is one, otherwise add `public float CellSize => cellSize;`.

### F. Assets
17. `BlockColorPalette.asset` (via the Unity Inspector/UnityMCP so serialization is correct): 7 entries `tile_blue, cyan, green, orange, purple, red, yellow` with sprite + representative color (sampled from each tile's center pixel).
18. `board_Frame.png.meta`: border L56/B72/R56/T56, PPU 180, mesh type FullRect. Reimport.
19. Swap sprite references:
    - Get the new GUIDs from `board_Frame.png.meta` and `tiles/cell_empty.png.meta`.
    - In `GridCellPrefab.prefab`, `BoardFramePrefab.prefab`, `GameScene.unity`: replace `guid: 553b9710e8e9748b0bfa8b7bcdef9750` → cell_empty GUID and `guid: 57a719088b892495b99e3264699b3f33` → board_Frame GUID on `m_Sprite` lines only (`fileID: 21300000` stays valid for Single sprites).
    - Check that no other file references those GUIDs in a way that should change.

### G. Tests
20. `BlockSkinTests`: `HasSprite_WhenSpriteNull_ReturnsFalse`, `SameAs_WithSameSprite_ReturnsTrue`, `SameAs_WithDifferentSprite_ReturnsFalse`, `SameAs_ColorMode_ComparesRgbOnly`.
21. `BlockColorPaletteTests` (add): `TryGetRandomSkin_WhenSpritesEmpty_FallsBackToColors`, `TryGetRandomSkin_WhenHasSprites_ReturnsSpriteSkin`, `TryGetRandomSkin_WithExclude_ReturnsUnusedSprite`, `TryGetRandomSkin_WhenAllExcluded_ReturnsSpriteFromList`, `TryGetRandomSkin_WhenBothEmpty_ReturnsFalse`. Sprites created with `Sprite.Create` on a small `Texture2D` in the test and destroyed in `TearDown`.

## Verification

Automated:
- `refresh_unity` → `read_console`: no compile errors/warnings from the changed files.
- Run EditMode tests (`run_tests` via UnityMCP, or the batchmode command in AGENTS.md if the Editor is closed): all existing tests and the new ones pass. `BlockGridBoardFrameTests` must stay green.

Manual in the Editor (`GameScene`, Play), sprite mode:
- Hand: 3 shapes with tile sprites, no duplicate tile across the hand, same size as before.
- Drag: the ghost shows the same tile sprite, semi-transparent; in a line about to clear the alpha changes like before.
- Line-clear preview: blocks in the lines switch to the dragged tile and restore correctly when moving away or releasing without placing.
- Place: the placed block fills the cell exactly (no ~84% shrink); clear animations still work.
- Dimmed shape (no space left): visibly darker/transparent and not draggable.
- Board: new frame with smooth rounded corners (9-slice not stretched), frame edge doesn't cover the outer cells; new `cell_empty` cells sized correctly. If needed, tune PPU/border of `board_Frame` and `framePaddingRatio`; report the final values.

Manual, color mode (regression):
- Temporarily clear `sprites` in `BlockColorPalette.asset` (don't save) → blocks colored by `colors`, ghost/preview tint like before.

`git status` / diff check: only the files listed above; scene diff limited to the `m_Sprite` GUID lines.

## Commit

One commit on `feature/block-sprite-palette` after verification, e.g. `BlockDrag: use tile sprites for blocks and board`, including all `.meta` files and `ComboBlock_UI_assets/`. No push unless you ask.
