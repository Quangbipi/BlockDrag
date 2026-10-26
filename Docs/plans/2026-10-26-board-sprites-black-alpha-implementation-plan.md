# Board sprites: black with alpha ~0.7 — Implementation Plan

Design: [2026-10-26-board-sprites-black-alpha-design.md](../designs/2026-10-26-board-sprites-black-alpha-design.md) (Option A approved)

## Impacted files

| File | Change |
|---|---|
| `Assets/_Game/_BlockDrag/Resource/BoardFrame.png` | Pixels rewritten (black, fill α0.70, border α0.60) |
| `Assets/_Game/_BlockDrag/Resource/GridCell_Empty.png` | Pixels rewritten (black, fill α0.70, edge α0.80) |

No changes to `.meta` files, prefabs, scenes or code.

## Steps

1. **Write the conversion script** in the session scratchpad (outside the project), in Python standard library only (`zlib`, `struct`), since PIL isn't installed:
   - Decode the PNG (8-bit RGBA, all filter types).
   - For each pixel with `a > 0`, classify its tone by comparing its luminance with the main fill (the most common opaque color):
     - Within the fill's luminance → base 0.70.
     - Darker than the fill (GridCell edge) → base 0.80.
     - Lighter than the fill (BoardFrame border) → base 0.60.
   - Output `(0,0,0, round(base × a))`; pixels with `a = 0` stay `(0,0,0,0)`.
   - Encode to PNG RGBA 8-bit (filter 0) and overwrite the original file.
2. **Run the script** on both files.
3. **Check the output** with the pixel-statistics script: list the colors and alpha values after conversion and confirm that:
   - RGB is `(0,0,0)` everywhere.
   - BoardFrame: fill alpha ≈ 179, border ≈ 153, corners scale down smoothly, transparent pixels stay 0.
   - GridCell: fill ≈ 179, edge ≈ 204.
   - Size is unchanged (64×64, 128×128).
4. **Refresh Unity** (`refresh_unity` via UnityMCP if connected) and check `read_console` for import errors.

## Verification

- `git status` shows only the 2 PNGs changed (no `.meta` changes).
- Manual check in the Editor (your part): open `GameScene` and look at the board on the current BG.
  - The frame is darker than the BG and the cells are darker still.
  - The grid lines between cells are still visible.
  - The frame corners stay smooth and the 9-slice doesn't stretch out of shape.
  - Change the BG color and confirm the board changes tint with it.

## Rollback

`git checkout -- Assets/_Game/_BlockDrag/Resource/BoardFrame.png Assets/_Game/_BlockDrag/Resource/GridCell_Empty.png`
