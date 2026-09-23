# Project Architecture Memory

Use this document as the architectural baseline when analyzing or changing BlockDrag. It records the current intended structure; verify scene and prefab wiring in Unity before assuming runtime availability.

## Layer Model

Dependencies should point downward through these layers:

```text
Presentation (Hung.UI)
    ↓
Feature services (Ads, Audio, IAP, Items, Tutorial, SubSystem, Gameplay)
    ↓
Coordination and data (Hung.Common, Hung.Data)
    ↓
Contracts and shared models (Hung.Base)
    ↓
Foundation (Hung.DesignPattern, Hung.Utilities.*)
```

1. **Foundation** — `_DesignPattern` supplies singleton, observer, command, memento, and pooling primitives. `_Utilities` supplies JSON, timers, input, math, logging, and generic helpers.
2. **Contracts and shared models** — `_Base` contains `GameData`, level/unit models, initialization, scene management, service contracts, and `Locator`. Feature `Base` directories join `Hung.Base` through `.asmref` files.
3. **Data** — `_Data` owns `DataManager`, persistent `GameData`, static ScriptableObject configuration, and level loading.
4. **Coordination** — `_Common` provides cross-feature events through `GameEventManager` and `Dispatcher<EventID>`.
5. **Feature services** — `_Ads`, `_Audio`, `_IAP`, `_Items`, `_Tutorial`, and `_SubSystem` define feature boundaries. Implementations register their interfaces with `Locator`.
6. **Presentation** — `_UI` contains `UIManager`, canvases, popups, components, and animations. UI prefabs live under `Assets/_Game/Resources/UI`.

## Runtime Rules

- Access cross-feature behavior through interfaces exposed by `Locator`; do not make lower layers depend on concrete UI or gameplay classes.
- Register persistent manager services during bootstrap, normally in `Awake`, before any consumer accesses them.
- Treat `.asmdef` and `.asmref` files as the real module boundaries. Folder names alone do not define assembly ownership.
- Keep static tuning in ScriptableObjects, mutable player state in `GameData`, and level definitions in `LevelDataSO` or `Resources/LevelData/Lvl_<index>`.
- Follow the UI lifecycle: `Setup → Open → UpdateUI → Show/Hide → Close`. Load reusable canvases through `UIManager` by component type.
- Use direct service calls for commands and queries. Use `GameEventManager` only for broadcasts where publisher and subscriber should remain decoupled.
- Pair every event subscription with unsubscription, normally in `Awake`/`Start` and `OnDestroy`.
- Reuse `SimplePool`, `ParticlePool`, or `MiniPool<T>` for frequently spawned runtime objects.

## Current Repository Caveats

- Concrete implementations for several service contracts are absent from the current `Assets` tree; do not assume every `Locator` property is initialized.
- Build Settings currently lists only `Assets/Scenes/SampleScene.unity`, while some bootstrap code expects additional scene indices.
- Some scene script GUIDs do not resolve to local `.meta` files, indicating missing or migrated assets.
- `RemoveAdsPopup` and `ShopCanvas` currently contain mostly commented-out behavior and should be treated as incomplete features.

When new evidence conflicts with this document, update the document in the same change that alters the architecture.
