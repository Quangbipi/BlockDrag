# Implementation Plan: Chuyển LineClearPreviewEffect Sang Prefab

> **Ngày tạo:** 2026-10-12
> **Design:** `Docs/designs/2026-10-12-line-clear-preview-prefab-design.md`

---

## Bước 1 — Đo cellSize & bake sprite viền
- Vào Play Mode `GameScene`, đọc `BlockGrid.CellSize` (dùng để quy đổi kích thước hạt sang world unit), thoát Play.
- Dùng Unity Editor (execute_code) gọi `LineClearPreviewEffect.CreateGlowTexture(64, 24, 16)` (vẫn còn trong code ở bước này) → `EncodeToPNG` → ghi `Assets/_Game/_BlockDrag/Effects/Textures/LineClearGlow.png`.
- Cấu hình importer: `textureType = Sprite`, `spriteMode = Single`, `spritePixelsPerUnit = 64`, `spriteBorder = (24,24,24,24)`, `spriteMeshType = FullRect`, `mipmapEnabled = false`, `wrapMode = Clamp`, `filterMode = Bilinear`, `alphaIsTransparency = true`, nén `None` (texture nhỏ, tránh vệt nén ở viền mềm). Reimport.

## Bước 2 — Sửa `LineClearPreviewEffect`
**File:** `Assets/_Game/_BlockDrag/Effects/LineClearPreviewEffect.cs`
- Field:
  - Thêm `[SerializeField] ParticleSystem sparkles;`, `[SerializeField] SpriteRenderer glowTemplate;`, `[SerializeField] float glowLineEdgePixels = 16f;`
  - Bỏ `glowSprite`, `materialOverride`, `glowColor`, `sparkleSizeRange`, `sparkleLifetimeRange`, `sparklePalette`, `sortingOrder`.
  - Giữ `blockGrid`, `tintBlocks`, `glowPadding`, `pulseAlphaRange`, `pulseDuration`, `fadeDuration`, `sparklesPerSecondPerLine`, `sparkleSpeedRange`.
- Bỏ: `vfxRoot` tự tạo (dùng `transform`), `runtimeMaterial`, `runtimeGlowTexture`, `runtimeGlowSprite`, `BuildParticleSystem()`, `CreateGlowTexture()`, hằng `GlowTexture*`/`GlowPixelsPerUnit`.
- `Awake`: tìm grid; nếu `sparkles == null || glowTemplate == null` → `Debug.LogError(..., this)` + `enabled = false; return;`. Lưu `baseGlowColor = glowTemplate.color`; `glowTemplate.gameObject.SetActive(false)`; `sparkles.Play()`.
- `LayoutGlows`: `transform.SetPositionAndRotation(grid…)`, `transform.localScale = grid.lossyScale`; `scale = padding / (glowLineEdgePixels / glowTemplate.sprite.pixelsPerUnit)`; clone `Instantiate(glowTemplate, transform)` khi pool thiếu.
- `ApplyGlowAlpha`: `color = baseGlowColor; color.a *= pulse * fade`.
- `EmitSparkles`: `EmitParams` chỉ set `position`, `velocity` (để trống size/lifetime/color/rotation → lấy từ main module).
- `OnDestroy`: không còn gì cần dọn (viền clone là con của root).
- Không đổi: tint, cache, event, `Show/Hide`, `GetLineLocalRect`, `Update` thoát sớm.

## Bước 3 — Tạo prefab
**File mới:** `Assets/_Game/_BlockDrag/Prefabs/LineClearPreviewEffect.prefab` (dựng qua Unity MCP/execute_code, lưu bằng `PrefabUtility.SaveAsPrefabAsset`)
- Root `LineClearPreviewEffect`: component `LineClearPreviewEffect` (giá trị mặc định như hiện tại), gán `sparkles`, `glowTemplate`.
- Con `Sparkles`: ParticleSystem với thông số như `BuildParticleSystem()` hiện tại:
  - Main: loop, playOnAwake false, duration 1, startSpeed 0, gravity 0, World, Hierarchy, maxParticles 256,
    `startSize` Random Between Two Constants = `(0.04, 0.08) × cellSize`, `startLifetime` = `(0.4, 0.8)`,
    `startRotation` = `(0, 360°)`, `startColor` = Random Color gradient (trắng, `#FFC4EE`, `#FF78DC`).
  - Emission off, Shape off; SizeOverLifetime (0.3 → 1 @0.25 → 0); ColorOverLifetime (alpha 0 → 1 @0.2 → 1 @0.6 → 0).
  - Renderer: Billboard, material `Sprites-Default` (asset built-in), sortingOrder 6.
- Con `GlowTemplate` (inactive): SpriteRenderer `drawMode = Sliced`, sprite `LineClearGlow`, màu `#FF3DC8`, sortingOrder 6.

## Bước 4 — Cập nhật test
**File:** `Assets/Tests/EditMode/LineClearPreviewEffectTests.cs`
- Giữ 3 test `GetLineLocalRect_*`.
- Thay `CreateGlowTexture_CenterTransparent_EdgeOpaque` bằng:
  - `LineClearGlowSprite_HasNineSliceBorder` — load sprite qua `AssetDatabase` (EditMode), border = 24 mọi cạnh, ppu = 64.
  - `LineClearPreviewPrefab_HasSparklesAndGlowTemplateAssigned` — load prefab, component có đủ tham chiếu, template inactive, `drawMode == Sliced`.
- Kiểm tra `EditModeTests.asmdef` có cho dùng `UnityEditor` (EditMode tests mặc định là Editor-only) — nếu thiếu thì thêm, không đổi gì khác.

## Bước 5 — Scene
- `GameScene`: remove component `LineClearPreviewEffect` khỏi `VFXPut`; instantiate prefab ở gốc scene (`PrefabUtility.InstantiatePrefab`), set `blockGrid` → `BlockGrid`; lưu scene. Kiểm tra diff scene chỉ gồm các thay đổi này.

## Bước 6 — Kiểm tra
1. Refresh Unity, Console sạch.
2. EditMode tests: các test effect + `BlockBoardTests` pass (6 `GameOverCheckTests` fail sẵn — không liên quan).
3. Play Mode qua script: dựng hàng sắp đầy như lần trước → chụp màn hình so với bản cũ (viền + màu + ghost); rời ô → màu trả lại; đo 0 B GC khi giữ cùng ô.
4. (Bạn xác nhận) Mở prefab, đổi màu GlowTemplate / startSize Sparkles → thấy thay đổi khi chơi.

## Files tác động

| File | Loại |
|---|---|
| `Assets/_Game/_BlockDrag/Effects/LineClearPreviewEffect.cs` | Sửa |
| `Assets/_Game/_BlockDrag/Effects/Textures/LineClearGlow.png` (+ .meta) | Mới |
| `Assets/_Game/_BlockDrag/Prefabs/LineClearPreviewEffect.prefab` (+ .meta) | Mới |
| `Assets/Tests/EditMode/LineClearPreviewEffectTests.cs` | Sửa |
| `Assets/_Game/_Scenes/Game/GameScene.unity` | Sửa |

Không đổi `.asmdef` gameplay hay ranh giới kiến trúc.
