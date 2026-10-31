# Render & measure snippets

Run with Unity MCP `execute_code` (CodeDom, C# 6). Edit the constants at the top of each snippet. Output files go to the session scratchpad — replace `OUT_DIR`.

## 1. Measure the mockup

Colors at points, then the horizontal extent of non-background pixels per row (y from the top, mockup px). Multiply by 3 for canvas units.

```csharp
const string MOCKUP = "Assets/_Game/_BlockDrag/Resource/ComboBlock_UI_assets/UI Preview/Home@1x.png";
string[] BG = { "6236E0", "6B42E2", "4A2CA6", "4F34A7" }; // background + grid + band colors (sample them first)
int[][] POINTS = { new[] { 5, 5 }, new[] { 180, 630 } };

var t = new Texture2D(2, 2); t.LoadImage(System.IO.File.ReadAllBytes(MOCKUP));
System.Func<int, int, string> hex = (x, y) => ColorUtility.ToHtmlStringRGB(t.GetPixel(x, t.height - 1 - y));
var sb = new System.Text.StringBuilder();
foreach (var p in POINTS) sb.Append("(" + p[0] + "," + p[1] + ")=" + hex(p[0], p[1]) + " ");
sb.Append("\n");
for (int y = 0; y < t.height; y++)
{
    int mn = 9999, mx = -1;
    for (int x = 0; x < t.width; x++)
        if (System.Array.IndexOf(BG, hex(x, y)) < 0) { if (x < mn) mn = x; if (x > mx) mx = x; }
    if (mx >= 0) sb.Append(y + ":" + mn + "-" + mx + " ");
}
return sb.ToString();
```

Read runs of rows as elements: first/last row = top/bottom, min/max x = left/right. Rows that span the whole width are grid lines — ignore them.

## 2. Dump a prefab

```csharp
const string PREFAB = "Assets/_Game/Resources/UI/HomeCanvas.prefab";
var root = PrefabUtility.LoadPrefabContents(PREFAB);
var sb = new System.Text.StringBuilder();
System.Action<Transform, int> dump = null;
dump = (tr, d) =>
{
    var rt = (RectTransform)tr;
    sb.Append(new string(' ', d * 2) + tr.name + (tr.gameObject.activeSelf ? "" : " [off]")
        + " a=" + rt.anchorMin + rt.anchorMax + " pos=" + rt.anchoredPosition + " size=" + rt.sizeDelta + " :");
    foreach (var c in tr.GetComponents<Component>())
    {
        if (c is Transform) continue;
        sb.Append(" " + c.GetType().Name);
        var tx = c as TMPro.TMP_Text;
        if (tx != null) sb.Append("(font=" + tx.font.name + " size=" + tx.fontSize + " margin=" + tx.margin + " '" + tx.text + "')");
        var im = c as UnityEngine.UI.Image;
        if (im != null) sb.Append("(sprite=" + (im.sprite ? im.sprite.name : "null") + " type=" + im.type + ")");
    }
    sb.Append("\n");
    foreach (Transform ch in tr) dump(ch, d + 1);
};
dump(root.transform, 0);
PrefabUtility.UnloadPrefabContents(root);
return sb.ToString();
```

## 3. Generate simple sprites

A grid tile (line on left + top edges) and a rounded 9-slice panel, then importer setup.

```csharp
const string DIR = "Assets/_Game/_BlockDrag/Resource/ComboBlock_UI_assets/";
System.Func<string, Color> c = h => { Color o; ColorUtility.TryParseHtmlString(h, out o); return o; };

// Grid tile: TILE = mockup pitch * 3, LINE = mockup line width * 3
int TILE = 72, LINE = 3; Color line = c("#6B42E2");
var g = new Texture2D(TILE, TILE, TextureFormat.RGBA32, false);
for (int y = 0; y < TILE; y++) for (int x = 0; x < TILE; x++)
    g.SetPixel(x, y, (x < LINE || y >= TILE - LINE) ? line : new Color(0, 0, 0, 0));
System.IO.File.WriteAllBytes(DIR + "bg_grid_tile.png", g.EncodeToPNG());

// Rounded panel: SIZE, RADIUS and BORDER in canvas units (mockup px * 3)
int S = 120; float R = 36f, BW = 9f; Color fill = c("#422995"), border = c("#1C1A3A");
var p = new Texture2D(S, S, TextureFormat.RGBA32, false);
for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
{
    float h = S / 2f, cx = Mathf.Abs(x + 0.5f - h) - (h - R), cy = Mathf.Abs(y + 0.5f - h) - (h - R);
    float d = Mathf.Sqrt(Mathf.Max(cx, 0) * Mathf.Max(cx, 0) + Mathf.Max(cy, 0) * Mathf.Max(cy, 0)) + Mathf.Min(Mathf.Max(cx, cy), 0) - R;
    var col = Color.Lerp(border, fill, Mathf.Clamp01(0.5f - (d + BW))); col.a = Mathf.Clamp01(0.5f - d);
    p.SetPixel(x, y, col);
}
System.IO.File.WriteAllBytes(DIR + "panel_score.png", p.EncodeToPNG());
AssetDatabase.Refresh();

System.Action<string, Vector4, TextureWrapMode> setup = (path, b, wrap) =>
{
    var imp = (TextureImporter)AssetImporter.GetAtPath(path);
    imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single;
    imp.alphaIsTransparency = true; imp.mipmapEnabled = false; imp.wrapMode = wrap;
    imp.spriteBorder = b; imp.textureCompression = TextureImporterCompression.Uncompressed;
    var ts = new TextureImporterSettings(); imp.ReadTextureSettings(ts); ts.spriteMeshType = SpriteMeshType.FullRect; imp.SetTextureSettings(ts);
    imp.SaveAndReimport();
};
setup(DIR + "bg_grid_tile.png", Vector4.zero, TextureWrapMode.Repeat);
setup(DIR + "panel_score.png", new Vector4(R + BW, R + BW, R + BW, R + BW), TextureWrapMode.Clamp);
return "ok";
```

For an existing sprite drawn off-ratio, only set `spriteBorder` + Full Rect and reimport.

## 4. Render a prefab at several resolutions

Instantiates the prefab in a preview scene (the open scene is untouched), sizes a world-space root canvas exactly as `CanvasScaler` 1080×1920 / match 0.5 would, and writes one PNG per resolution. Also checks the canvas script's serialized refs.

```csharp
const string PREFAB = "Assets/_Game/Resources/UI/HomeCanvas.prefab";
const string SCRIPT = "HomeCanvas";                       // canvas component to check
string[] REFS = { "startButton", "settingButton", "highScoreTxt" };
const string OUT_DIR = "/path/to/scratchpad/";
int[][] SIZES = { new[] { 1080, 1920 }, new[] { 1170, 2532 }, new[] { 1536, 2048 } };

var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB);
var sb = new System.Text.StringBuilder();
var so = new SerializedObject(prefab.GetComponent(SCRIPT));
foreach (var f in REFS) { var v = so.FindProperty(f).objectReferenceValue; sb.Append(f + "=" + (v ? v.name : "NULL") + " "); }

foreach (var s in SIZES)
{
    var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
    var camGo = new GameObject("Cam"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGo, scene);
    var cam = camGo.AddComponent<Camera>(); cam.scene = scene; cam.orthographic = true; cam.orthographicSize = 1f;
    cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.aspect = (float)s[0] / s[1];
    var rt = new RenderTexture(s[0], s[1], 24); cam.targetTexture = rt;

    var rootGo = new GameObject("Root", typeof(RectTransform)); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rootGo, scene);
    rootGo.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
    float sf = Mathf.Pow(2, Mathf.Lerp(Mathf.Log(s[0] / 1080f, 2), Mathf.Log(s[1] / 1920f, 2), 0.5f));
    float h = s[1] / sf;
    var crt = (RectTransform)rootGo.transform;
    crt.sizeDelta = new Vector2(s[0] / sf, h); crt.position = new Vector3(0, 0, 10); crt.localScale = Vector3.one * (2f / h);

    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene); inst.transform.SetParent(rootGo.transform, false);
    var irt = (RectTransform)inst.transform; irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one; irt.offsetMin = Vector2.zero; irt.offsetMax = Vector2.zero;

    foreach (var t in inst.GetComponentsInChildren<TMPro.TMP_Text>(true)) t.ForceMeshUpdate();
    Canvas.ForceUpdateCanvases();
    foreach (var f in inst.GetComponentsInChildren<UnityEngine.UI.AspectRatioFitter>(true)) { f.enabled = false; f.enabled = true; }
    foreach (var lg in inst.GetComponentsInChildren<UnityEngine.UI.LayoutGroup>(true)) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)lg.transform);
    Canvas.ForceUpdateCanvases();
    cam.Render(); cam.Render();

    RenderTexture.active = rt;
    var tex = new Texture2D(s[0], s[1], TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, s[0], s[1]), 0, 0); tex.Apply();
    RenderTexture.active = null;
    var file = OUT_DIR + System.IO.Path.GetFileNameWithoutExtension(PREFAB) + "_" + s[0] + "x" + s[1] + ".png";
    System.IO.File.WriteAllBytes(file, tex.EncodeToPNG());
    sb.Append("\n" + file + " canvas=" + crt.sizeDelta);

    cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(rt);
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
}
return sb.ToString();
```

`SafeAreaFitter` does not run here (edit mode, full-screen safe area), so the renders show the layout without a notch.

## 5. Play mode check

Enter play with `manage_editor` → `play` from `LoadStart`, then:

```csharp
// state of the canvas
var type = System.Type.GetType("UI.HomeCanvas, BlockDrag.UI");
var hc = (Component)UnityEngine.Object.FindObjectOfType(type);
return hc == null ? "not open" : hc.name + " active=" + hc.gameObject.activeInHierarchy;
```

```csharp
// click a button (UIButton listens to pointer events)
var hc = (Component)UnityEngine.Object.FindObjectOfType(System.Type.GetType("UI.HomeCanvas, BlockDrag.UI"));
var btn = hc.transform.Find("SafeArea/Content/BottomGroup/StartBtn").gameObject;
var ev = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left };
UnityEngine.EventSystems.ExecuteEvents.Execute(btn, ev, UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
UnityEngine.EventSystems.ExecuteEvents.Execute(btn, ev, UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);
UnityEngine.EventSystems.ExecuteEvents.Execute(btn, ev, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
return "clicked";
```

```csharp
// screenshot of the Game view (written at end of frame; read it on the next call)
ScreenCapture.CaptureScreenshot("/path/to/scratchpad/play.png");
return "requested";
```

Stop with `manage_editor` → `stop`, then `read_console` for errors.
