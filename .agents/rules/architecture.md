# Project Architecture Memory

Use this document as the architectural baseline when analyzing or changing BlockDrag. It records the current intended structure; verify scene and prefab wiring in Unity before assuming runtime availability.

## Module Map

Shared framework code ships as local `com.quang.*` packages (see `Packages/manifest.json`, sourced from `Quang_Packages`). Only game-specific code lives under `Assets/_Game`.

| Assembly | Location | Role |
|----------|----------|------|
| `Quang.DesignPattern` | package `com.quang.designpattern` | Singleton, observer, command, memento, pooling (`SimplePool`, `PoolType`) |
| `Quang.Utilities`, `Quang.Utilities.STimer`, `Quang.Utilities.Json` | package `com.quang.utilities` | Timers, JSON, math, logging, generic helpers |
| `Quang.Persistence` | package `com.quang.persistence` | Save/load backend |
| `Quang.Base` | package `com.quang.base` **+ `Assets/_Game/_BlockDrag/Base` via asmref** | `GameData`, service contracts, `Locator`, init, scene management |
| `Quang.Data` | package `com.quang.data` **+ `Assets/_Game/_BlockDrag/Data` via asmref** | `DataManager`, `GameplayData` and `BlockColorPaletteSO` ScriptableObjects, level loading |
| `Quang.Common` | package `com.quang.data/Common` | `GameEventManager` / `Dispatcher<EventID>` broadcasts |
| `Quang.UI` | package `com.quang.ui` | `UIManager`, `UISCanvas`, UI components |
| `Quang.Ads`, `Quang.IAP`, `Quang.Analytics`, `Quang.LiveOps.*` | `com.quang.services.*`, `com.quang.liveops.*` | Feature services; register their contracts with `Locator` |
| `BlockDrag` | `Assets/_Game/_BlockDrag` | Gameplay: board, shapes, spawner, score, combo timer, effects |
| `BlockDrag.UI` | `Assets/_Game/_UI` | Game canvases, popups, combo text, debug UI |
| `EditModeTests` | `Assets/Tests/EditMode` | EditMode tests for gameplay and UI logic |

`Assets/_Game/_Base`, `_Data`, `_Utilities` hold only prefabs, ScriptableObject assets and input actions — no code.

## Layer Model

Dependencies must point downward:

```text
Presentation        BlockDrag.UI
    ↓
Gameplay            BlockDrag
    ↓
Feature services    Quang.UI, Quang.Ads, Quang.IAP, Quang.Analytics, Quang.LiveOps.*
    ↓
Coordination/data   Quang.Common, Quang.Data
    ↓
Contracts/models    Quang.Base (Locator, GameData, service interfaces)
    ↓
Foundation          Quang.DesignPattern, Quang.Utilities.*, Quang.Persistence
```

- `BlockDrag` must never reference `BlockDrag.UI`.
- Packages must never reference `BlockDrag` or `BlockDrag.UI`.
- `BlockDrag.UI` talks to gameplay through `Locator` contracts (below), not through concrete gameplay singletons.

## Extending Package Assemblies (asmref)

Two folders inject game code into package assemblies. Keep them minimal — only code that **must** live there:

- `_BlockDrag/Base` → `Quang.Base`: `partial class GameData` save fields, and game contracts exposed on `Locator` (`IScoreService`, `IBoardSessionService`, `Locator.BlockDrag.cs`).
- `_BlockDrag/Data` → `Quang.Data`: `partial class GameplayData` tunables and the `ITEM` enum it uses (plus Odin type-name bindings for the pre-migration asset).
  - `partial class DataManager` (`DataManager.BlockDrag.cs`): the game's single `TryGetGameSOData<T>` implementation. It serializes and returns `GameplayData` and `BlockColorPaletteSO`. Register any further game ScriptableObject that must be a `DataManager` field here.
  - `BlockColorPaletteSO`: the random block colour set. It lives here, not in `BlockDrag/Config`, because a `DataManager` field must be a type `Quang.Data` can see. Gameplay reads it through `Locator.Data.GetSOData<BlockColorPaletteSO>()`.

Everything else (shape data, score config, level schema, gameplay logic) belongs to the `BlockDrag` assembly.

## Gameplay Structure (`BlockDrag`)

- `Board/BlockBoard` — pure C# board rules: occupancy, placement checks, full-line detection. Unit-tested; no Unity objects.
- `BlockGrid` — presentation of the board: cell GameObjects, indicator, clear animations. Delegates rules to `BlockBoard`.
- `BlockShape` (implements `IShapeFootprint`), `BlockDragHandler`, `BlockSpawner` (implements `IBoardSessionService`).
- `ScoreManager` (implements `IScoreService`), `Timer/CountdownManager`, `Effects/BlockPlaceEffect`.
- `Config/` — `BlockShapeData`, `ScoreConfigSO`, `ScoreConfig.asset`. `Level/` — legacy `LevelData` / `LevelDataSO` schema still read by `HomeCanvas`.
- Namespace: `Gameplay.BlockDrag` for all gameplay types.

## Runtime Rules

- Access cross-feature behavior through interfaces exposed by `Locator`; lower layers never depend on concrete UI or gameplay classes.
- Gameplay services register in `Awake` and clear their `Locator` slot in `OnDestroy` only if it still points to themselves. Consumers bind to the current value and rebind on `Locator.ScoreChanged` / `Locator.BoardSessionChanged` so registration order does not matter.
- Treat `.asmdef` and `.asmref` files as the real module boundaries. Folder names alone do not define assembly ownership.
- Keep static tuning in ScriptableObjects, mutable player state in `GameData`, and level definitions in `Resources/LevelData/Lvl_<index>`.
- Follow the UI lifecycle: `Setup → Open → UpdateUI → Show/Hide → Close`. Load reusable canvases through `UIManager` by component type.
- Use direct service calls for commands and queries. Use `GameEventManager` only for broadcasts where publisher and subscriber should remain decoupled.
- Pair every event subscription with unsubscription, normally in `OnEnable`/`OnDisable` or `Awake`/`OnDestroy`.
- Reuse `SimplePool`, `ParticlePool`, or `MiniPool<T>` for frequently spawned runtime objects.

## Current Repository Caveats

- Concrete implementations for several package service contracts may be absent from the scenes; do not assume every `Locator` property is initialized.
- Build Settings lists only `Assets/_Game/_Scenes/Game/GameScene.unity`; `Home`, `LoadStart`, `CryptoLoader` scenes exist but are not in the build.
- `RemoveAdsPopup` and `ShopCanvas` currently contain mostly commented-out behavior and should be treated as incomplete features.

When new evidence conflicts with this document, update the document in the same change that alters the architecture.
