# Layer Alignment Refactor — Implementation Plan

Design: `Docs/designs/2026-10-12-layer-alignment-refactor-design.md`

## Step 1 — Update architecture documentation
- `.agents/rules/architecture.md`: rewrite the layer model for `Quang.*` packages, `BlockDrag`, `BlockDrag.UI`;
  document the asmref extension rule and the `Locator.Score` / `Locator.BoardSession` contracts.
- `AGENTS.md`: fix the project-structure paragraph (feature folders now come from packages) and the testing
  note (EditMode tests already exist).

## Step 2 — Remove dead code, fix combo subscription
- Delete `Assets/_Game/_BlockDrag/Data/GameplayData.cs` (+ `.meta`).
- `ComboEffectManager`: bind/rebind to the score service instead of a one-shot check
  (implemented on top of the Locator event from Step 4).

## Step 3 — Move gameplay code out of `Quang.Data`; unify namespaces
- `git mv` (with `.meta`): `BlockShapeData.cs`, `ScoreConfigSO.cs`, `ScoreConfig.asset` → `_BlockDrag/Config/`;
  `LevelData.cs`, `LevelDataSO.cs` → `_BlockDrag/Level/`.
- `BlockDrag.asmdef`: add `Quang.DesignPattern`.
- Wrap `BlockGrid`, `BlockShape`, `BlockSpawner`, `BlockDragHandler`, `BlockShapeData`, `BlockRotation`,
  `LevelDataSO` in `namespace Gameplay.BlockDrag`; add `using Gameplay.BlockDrag;` where needed
  (`HomeCanvas`, EditMode tests).

## Step 4 — Locator contracts for UI → gameplay
- New in `_BlockDrag/Base/`: `IScoreService.cs`, `IBoardSessionService.cs`, `Locator.BlockDrag.cs`.
- `ScoreManager`: implement `IScoreService`; register in `Awake`, unregister in `OnDestroy`;
  remove `Ins` / `OnInstanceReady`.
- `BlockSpawner`: implement `IBoardSessionService`; same lifecycle; remove `Ins` / `OnInstanceReady`.
- `GameplayCanvas`, `LoseCanvas`, `ComboEffectManager`: use `Locator.Score` / `Locator.BoardSession`.
- `CountdownManager.Instance`: public field → property with private setter.

## Step 5 — Extract board rules
- New in `_BlockDrag/Board/`: `IShapeFootprint.cs`, `BlockBoard.cs`, `LineClearResult.cs`.
- `BlockShape` implements `IShapeFootprint`.
- `BlockGrid`: delegate `IsCellOccupied`, `CanPlaceShape`, `CanShapeBePlacedAnywhere`, `CheckAndClearLines`
  and reset logic to `BlockBoard`; keep `occupiedBlocks` for GameObjects only.
- New tests: `Assets/Tests/EditMode/BlockBoardTests.cs`, `Assets/Tests/EditMode/BlockDragLocatorTests.cs`.

## Impacted files

| Area | Files |
|------|-------|
| Docs | `.agents/rules/architecture.md`, `AGENTS.md` |
| Assemblies | `Assets/_Game/_BlockDrag/BlockDrag.asmdef` |
| Gameplay | `BlockGrid.cs`, `BlockShape.cs`, `BlockSpawner.cs`, `BlockDragHandler.cs`, `ScoreManager.cs`, `Timer/CountdownManager.cs`, `Config/*`, `Level/*`, `Board/*`, `Base/*` |
| UI | `GameplayCanvas.cs`, `LoseCanvas.cs`, `ComboEffectManager.cs`, `HomeCanvas.cs` |
| Tests | existing EditMode tests (`using` only), `BlockBoardTests.cs`, `BlockDragLocatorTests.cs` |
| Assets | `ScoreConfig.asset` moved (GUID kept); no prefab/scene edits |

## Verification
1. Unity recompiles with zero errors (console).
2. All EditMode tests pass, including the new `BlockBoardTests` and `BlockDragLocatorTests`.
3. `grep` confirms `BlockDrag.UI` no longer references `ScoreManager.Ins` / `BlockSpawner.Ins`.
4. Manual in Editor (game scene): score/high score update, combo text appears on 2+ lines,
   game over opens `LoseCanvas`, *Try Again* restarts the board and resets the score.
5. Inspector: `Block_*.asset` and `ScoreConfig.asset` still show their script (no "Missing script").
