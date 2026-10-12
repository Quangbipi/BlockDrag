# Implementation Plan: Viền Cầu Vồng Khi Phá Từ 2 Hàng/Cột

> **Ngày tạo:** 2026-10-13
> **Design:** `Docs/designs/2026-10-13-multi-line-rainbow-glow-design.md`
> **Giả định đã chốt:** (1) 1 hàng + 1 cột cũng tính là 2 đường → cầu vồng. (2) Ô vẫn tô màu khối đang kéo, chỉ viền đổi màu.

---

## Bước 1 — Shader `LineClearRainbowGlow`
**File mới:** `Assets/_Game/_BlockDrag/Effects/Shaders/LineClearRainbowGlow.shader` (+ `.meta`, thư mục `Shaders` + `.meta`)
- `Shader "BlockDrag/LineClearRainbowGlow"`, HLSL `CGPROGRAM` một pass, không gắn `LightMode` (URP vẽ qua `SRPDefaultUnlit`).
- Tags: `Queue=Transparent`, `RenderType=Transparent`, `IgnoreProjector=True`, `PreviewType=Plane`, `CanUseSpriteAtlas=True`. `Cull Off`, `ZWrite Off`, `Blend SrcAlpha OneMinusSrcAlpha`.
- Properties:
  - `[PerRendererData] _MainTex ("Sprite", 2D) = "white" {}`
  - `_GradientTex ("Gradient", 2D) = "white" {}`
  - `_BoundsMin ("Bounds Min (world)", Vector) = (0,0,0,0)`
  - `_BoundsSize ("Bounds Size (world)", Vector) = (1,1,0,0)`
  - `_GradientDir ("Gradient Direction", Vector) = (0.6,1,0,0)`
  - `_ScrollSpeed ("Scroll Speed", Float) = 0`
- Vertex: `worldPos = mul(unity_ObjectToWorld, v.vertex)`, `pos = UnityObjectToClipPos(v.vertex)`, truyền `uv`, `color`, `worldPos.xy`.
- Fragment (như design 2.3): `uv01 = (worldPos - _BoundsMin) / max(_BoundsSize, 1e-4)`; `t = frac(dot(uv01 - 0.5, _GradientDir.xy) + 0.5 + _Time.y * _ScrollSpeed)`; `rgb = tex2D(_GradientTex, float2(t, 0.5)).rgb * i.color.rgb`; `a = tex2D(_MainTex, i.uv).a * i.color.a`.

## Bước 2 — Material
**File mới:** `Assets/_Game/_BlockDrag/Effects/Materials/LineClearRainbowGlow.mat` (+ `.meta`, thư mục `Materials` + `.meta`)
- Tạo qua Unity (MCP `manage_material`) dùng shader ở Bước 1, giữ giá trị mặc định. `_GradientTex` để trống trong asset vì runtime sẽ gán texture bake từ `Gradient`.

## Bước 3 — Sửa `LineClearPreviewEffect`
**File:** `Assets/_Game/_BlockDrag/Effects/LineClearPreviewEffect.cs`

3.1. **Field mới**, header `Multi-Line` (đặt sau `Sparkles`):
```csharp
[Tooltip("Số hàng + cột tối thiểu để dùng viền cầu vồng")]
[SerializeField] private int multiLineThreshold = 2;
[Tooltip("Material mẫu dùng shader BlockDrag/LineClearRainbowGlow")]
[SerializeField] private Material rainbowGlowMaterial;
[SerializeField] private Gradient rainbowGradient = CreateDefaultRainbowGradient();
[Tooltip("Hướng đổi màu trên bàn (x: ngang, y: dọc)")]
[SerializeField] private Vector2 gradientDirection = new Vector2(0.6f, 1f);
[Tooltip("Tốc độ cuộn gradient (vòng / giây), 0 = đứng yên")]
[SerializeField] private float gradientScrollSpeed;
[Tooltip("Hạt lấp lánh lấy màu từ gradient khi phá nhiều đường")]
[SerializeField] private bool tintSparklesWithGradient;
```
`CreateDefaultRainbowGradient()` (static): Unity `Gradient` chỉ có tối đa 8 key màu. Dùng 7 key: `#FF3DC8@0`, `#FFD9A8@0.15`, `#3DF2FF@0.35`, `#3D7BFF@0.5`, `#5CFF8A@0.68`, `#FFE45C@0.84`, `#FF3DC8@1`; alpha là 1 trên toàn dải.

3.2. **State mới:** `Material defaultGlowMaterial`, `Material rainbowInstance`, `Texture2D gradientTexture`, `bool isMultiLine`.
Shader property ID: `static readonly int` cho `_GradientTex`, `_BoundsMin`, `_BoundsSize`, `_GradientDir`, `_ScrollSpeed` (dùng `Shader.PropertyToID`).

3.3. **`Awake`** (sau khi kiểm tra tham chiếu):
- `defaultGlowMaterial = glowTemplate.sharedMaterial`.
- Nếu `rainbowGlowMaterial != null`: `gradientTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false) { wrapMode = Repeat, filterMode = Bilinear, hideFlags = DontSave }`; `rainbowInstance = new Material(rainbowGlowMaterial) { hideFlags = DontSave }`; gọi `BakeGradient()` và `ApplyRainbowParams()`.
- Nếu không có: `Debug.LogWarning(...)` một lần. Chế độ nhiều đường khi đó vẫn dùng viền hồng.

3.4. **Hàm mới:**
- `public static bool IsMultiLine(LineClearResult result, int threshold) => result.Rows.Count + result.Columns.Count >= Mathf.Max(1, threshold);`
- `public static Color32[] BakeGradientPixels(Gradient gradient, int width)`: trả mảng màu `gradient.Evaluate(i / (width - 1f))`. Hàm này static để test được.
- `private void BakeGradient()`: dùng một buffer `Color32[]` cấp phát sẵn, gọi `SetPixels32` rồi `Apply(false)`.
- `private void ApplyRainbowParams()`: gán `_GradientTex`, `_GradientDir`, `_ScrollSpeed` cho `rainbowInstance`.
- `private void UpdateRainbowBounds()`: gộp `activeLineRects` (local) thành một hộp bao, đổi 2 góc sang world bằng `transform.TransformPoint` rồi `SetVector(_BoundsMin/_BoundsSize)`.
- `private Color CurrentGlowColor => UseRainbow ? Color.white : baseGlowColor;` với `UseRainbow => isMultiLine && rainbowInstance != null`.

3.5. **`Show`**: tính `isMultiLine = IsMultiLine(result, multiLineThreshold)` trước `LayoutGlows`.

3.6. **`LayoutGlows`**: trong vòng lặp viền đang bật, gán `glow.sharedMaterial = UseRainbow ? rainbowInstance : defaultGlowMaterial` (gán `sharedMaterial` nên không sinh instance mới). Cuối hàm, nếu `UseRainbow` thì gọi `UpdateRainbowBounds()`.

3.7. **`ApplyGlowAlpha`**: thay `baseGlowColor` bằng `CurrentGlowColor` khi tính `color`.

3.8. **`EmitSparkles`**: nếu `tintSparklesWithGradient && UseRainbow`, đặt `emitParams.startColor = rainbowGradient.Evaluate(Random.value)`. Ngoài trường hợp đó, gọi `emitParams.ResetStartColor()` để hạt giữ màu cấu hình trên ParticleSystem.

3.9. **`OnDestroy`** (mới): `Destroy(rainbowInstance)`, `Destroy(gradientTexture)`. Trong Editor khi không ở Play mode thì dùng `DestroyImmediate`.

3.10. **`OnValidate`**: `multiLineThreshold = Mathf.Max(1, ...)`. Nếu `Application.isPlaying && rainbowInstance != null` thì gọi `BakeGradient()` và `ApplyRainbowParams()` để chỉnh gradient trực tiếp lúc Play.

## Bước 4 — Prefab
**File:** `Assets/_Game/_BlockDrag/Prefabs/LineClearPreviewEffect.prefab`
- Gán `rainbowGlowMaterial = LineClearRainbowGlow.mat` (qua MCP `manage_prefabs` / `manage_components`). Gradient dùng giá trị mặc định từ code. Prefab đã có trước nên field mới sẽ mang giá trị field initializer khi deserialize. Sau đó mở Inspector kiểm tra lại gradient đã có đủ 7 key.
- `GameScene.unity` chỉ chứa instance của prefab nên không cần sửa scene, trừ khi instance đang override material.

## Bước 5 — Test EditMode
**File:** `Assets/Tests/EditMode/LineClearPreviewEffectTests.cs` (thêm vào fixture hiện có)
- `IsMultiLine_SingleRow_ReturnsFalse`
- `IsMultiLine_TwoRows_ReturnsTrue`
- `IsMultiLine_OneRowOneColumn_ReturnsTrue`
- `IsMultiLine_ThresholdThree_TwoLines_ReturnsFalse`
- `BakeGradientPixels_EndsMatchGradientEndKeys`
- `LineClearPreviewPrefab_HasRainbowMaterialWithRainbowShader`: material khác null và `shader.name == "BlockDrag/LineClearRainbowGlow"`.
- `LineClearRainbowGlowShader_IsSupported`: `Shader.Find(...)` khác null và `isSupported` là true.

`LineClearResult` dựng bằng constructor public: `new LineClearResult(new[]{0,1}, Array.Empty<int>(), new List<Vector2Int>())`.

## Bước 6 — Kiểm tra
1. `refresh_unity`, sau đó `read_console`: không có lỗi biên dịch hay lỗi shader.
2. Chạy toàn bộ EditMode test qua `run_tests`: test cũ và test mới đều phải pass.
3. Kiểm tra thủ công trong `GameScene` (Play):
   - Kéo khối phá **1 hàng**: viền hồng như cũ.
   - Kéo khối phá **2+ hàng**, **2+ cột**, **1 hàng + 1 cột**: viền cầu vồng, hai đầu hồng, giữa cyan/xanh, lệch xanh lá/vàng theo chiều ngang. Viền sát nhau phải liền màu.
   - Kéo qua lại giữa vị trí 1 đường và nhiều đường: màu đổi ngay, không nháy sai.
   - Nhấp nháy, fade in/out, tint ô, hạt lấp lánh không đổi.
   - Thả khối: animation phá hàng bình thường, viền tắt.
   - Chỉnh `rainbowGradient` lúc Play: màu cập nhật ngay.
4. Profiler (tuỳ chọn): khi đang kéo, GC Alloc mỗi frame của `LineClearPreviewEffect.Update` vẫn là 0.

## Ngoài phạm vi
- Không sửa `BlockGrid`, `BlockBoard`, `LineClearResult`, animation phá hàng, điểm hay combo.
