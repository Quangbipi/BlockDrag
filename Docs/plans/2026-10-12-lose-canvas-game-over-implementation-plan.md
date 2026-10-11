# Implementation Plan: Game Over opens LoseCanvas

Design: `Docs/designs/2026-10-12-lose-canvas-game-over-design.md`

## Steps
1. `Assets/_Game/_BlockDrag/BlockSpawner.cs` — remove the auto-creation of `GameOverUI` in `Start`; add `static Ins` + `static OnInstanceReady` (same pattern as `ScoreManager`) so UI never needs `FindObjectOfType`.
2. Delete `Assets/_Game/_BlockDrag/GameOverUI.cs` and `GameOverUI.cs.meta`.
3. `Assets/_Game/_UI/Scripts/GameplayCanvas.cs` — bind/unbind `BlockSpawner.Ins.OnGameOver` (rebinding via `BlockSpawner.OnInstanceReady`) (OnEnable / OnDisable / OnDestroy / Open); handler calls `UIManager.Ins.OpenUI<LoseCanvas>(false)`.
4. `Assets/_Game/_UI/Scripts/LoseCanvas.cs` — `OnTryAgain` → `Close()`, `CloseUI<TopCanvas>()`, `BlockSpawner.Ins.RestartGame()` (same as old replay button). No heart check / interstitial / level reload.

No prefab, scene, asmdef, or serialized-data migration.

## Verification
- Unity compiles with no errors (console clean).
- Play the gameplay scene, fill the board until no move: LoseCanvas appears after `gameOverDelay`, no `GameOverUI` object is created.
- Tap Try Again: LoseCanvas and TopCanvas close, board and hand reset, score resets to 0, game is playable; losing again reopens LoseCanvas.
