# Gameplay Grid Background — Implementation Plan

- Date: 2026-11-27
- Design: `Docs/designs/2026-11-27-gameplay-grid-background-design.md` (approved 2026-11-27)
- Scope: scene and prefab data only. No C# changes, and no `.asmdef` or package changes.

## Impacted files

| File | Change |
|------|--------|
| `Assets/_Game/_Scenes/Game/GameScene.unity` | Add a `BgGrid` GameObject with a SpriteRenderer under `Environment`. |
| `Assets/_Game/Resources/UI/GameplayCanvas.prefab` | Set `_bgColors[0]` to Home purple. |

Unchanged: `bg_grid_tile.png` and its `.meta` (it already uses a Full Rect mesh, which Tiled draw mode needs), `GameplayCanvas.cs`, and `_bgColors[1..4]`.

## Steps

All edits go through the Unity Editor (Unity MCP) rather than hand-editing YAML, so Unity writes the file IDs and serialization.

1. **Open `GameScene.unity`** and check that `Environment` is at `(0,0,0)` with scale 1.
2. **Create `BgGrid`** as a child of `Environment`:
   - Transform: position `(0, 0, 0)`, scale `(1.6667, 1.6667, 1)`. At scale 1 one tile is 72 px ÷ 100 PPU = 0.72 units. Scaling by 1.6667 makes it **1.2 world units**, which is 72 screen px at 1080×1920 (60 px/unit).
   - SpriteRenderer settings:
     - Sprite `bg_grid_tile`
     - Draw Mode **Tiled**, Tile Mode Continuous
     - Size **(28.8, 24.48)** local units. That is 40 × 34 tiles, which comes to **48 × 40.8 world units**.
     - Colour white
     - Sorting Layer Default, **Order in Layer −100**
   - Layer: Default (0), so only the Base camera renders it. That camera's culling mask includes every layer, while `UICamera` and `Top` only render layers 5 and 9.
3. **Save the scene.**
4. **Edit `GameplayCanvas.prefab`** → `GameplayCanvas` component → `_bgColors[0]` = `(0.38431373, 0.21176471, 0.8784314, 1)`, which is the exact colour of Home's `Bg`. Save the prefab.
5. **Check the console** for errors or warnings. A Tiled SpriteRenderer warns if the sprite mesh isn't Full Rect.

## Verification

Manual checks in the Editor (Play Mode, starting from `LoadStart` → Home → Play):

| # | Check | Expected |
|---|-------|----------|
| V1 | Gameplay screen at 1080×1920 | Purple background with a grid. Tile size looks the same as on Home (compare with `Screenshots/02_home.png`). |
| V2 | Board, cells, tray blocks, drag preview, line-clear VFX | All draw on top of the grid. Nothing is hidden. |
| V3 | Game view at 1080×2400 (20:9) and 1536×2048 (4:3 tablet) | The grid reaches every screen edge, with no gaps. |
| V4 | Trigger a big combo or line clear (screen shake) | No edges of the grid show during the shake. |
| V5 | Perfect clear | The background tweens to the next colour and the grid stays visible. A new run or replay resets it to purple. |
| V6 | Open the Settings, Revive and Lose popups | They draw normally above everything. |

Automated tests: none added. The change is serialized scene and prefab data with no new logic. Run the existing EditMode suite once to confirm nothing regressed.

The PR should include before and after gameplay screenshots, and should call out that `GameScene.unity` and `GameplayCanvas.prefab` changed.
