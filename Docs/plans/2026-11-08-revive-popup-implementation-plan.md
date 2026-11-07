# Revive Popup — Implementation Plan

- Date: 2026-11-08
- Design: [2026-11-08-revive-popup-design.md](../designs/2026-11-08-revive-popup-design.md) (approved)
- Branch: `feature/quang-analytics`, or a new `feature/revive-popup` branch if you prefer

## Deviation from design

- **Card fitting.** The design proposed a new `ReviveCardFitter` script. The existing `UI.ScaleToFitParent` (`_UI/Scripts/Component`) already does exactly that: it keeps a fixed design size and scales down only, never up. It also has tests. The plan reuses it, so no new fitter script is added.

## Steps

### Step 1 — Session contract (`Quang.Base` via asmref)

File: `Assets/_Game/_BlockDrag/Base/IBoardSessionService.cs`
- Add `bool CanRevive { get; }`.
- Add `event Action OnOutOfMoves` and `event Action OnRevived`.
- Add `void Revive()` and `void ConfirmGameOver()`.
- Update the XML doc: `OnGameOver` now means the final lose.

File: `Assets/Tests/EditMode/BlockDragLocatorTests.cs`
- Update `FakeBoardSession` to implement the new members (empty bodies).

### Step 2 — Revive hand generation (`BlockDrag/Spawn`, pure C#)

File: `Assets/_Game/_BlockDrag/Spawn/AssistHandBuilder.cs`
- Add `public List<ShapeVariant> BuildRevive(BlockBoard board, int count)`. It does the following:
  1. Copy the board into `sim`.
  2. Collect every family, variant and position where `sim.FindLinesCompletedBy(variant, pos)` completes at least one line.
  3. Pick the highest `Score(...)` and place it on `sim` (the existing `PlaceOnSim` clears lines).
  4. Fill the rest with the existing candidate loop (`CollectCandidates`/`PickFromTop`) at difficulty 0.
  5. Return an empty list if step 2 finds nothing.
- Reuse the existing private helpers. No change to `Build`.

File: `Assets/_Game/_BlockDrag/Spawn/HandGenerator.cs`
- Add `public List<ShapeVariant> GenerateRevive(BlockBoard board, int count)`. It calls `assistBuilder.BuildRevive`. If the result is empty, it falls back to `Generate(board, count, 0f, null, true)` and logs `Debug.LogWarning`.

### Step 3 — Board session state (`BlockDrag`)

File: `Assets/_Game/_BlockDrag/BlockSpawner.cs`
- Add the fields `bool reviveUsed` and `bool awaitingRevive`.
- Add the events `OnOutOfMoves` and `OnRevived`.
- Implement `CanRevive => !reviveUsed`.
- `TriggerGameOverDelayed`: set `awaitingRevive = true` and raise `OnOutOfMoves` instead of `OnGameOver`.
- `ConfirmGameOver()`: if `awaitingRevive`, set it to false and raise `OnGameOver`. Repeated calls do nothing.
- `Revive()`: if `!awaitingRevive || reviveUsed`, return. Otherwise:
  - Set `awaitingRevive = false`, `reviveUsed = true` and `isGameOver = false`.
  - Call `ClearHand()`.
  - Spawn `Generator.GenerateRevive(GetBoard(), SlotCount)` into the 3 slots with the existing `SpawnShapeAt`, which keeps skin de-duplication.
  - Raise `OnRevived`, then call `CheckPlacementsAndGameOver()`.
- `RestartGame()`: reset `reviveUsed` and `awaitingRevive`.
- Refactor the "spawn variants into empty slots" loop out of `SpawnNewHand` into `SpawnHand(List<ShapeVariant>)`, so `Revive` and `SpawnNewHand` share it.

File: `Assets/_Game/_BlockDrag/ScoreManager.cs`
- Subscribe to `OnOutOfMoves`, which calls `StopComboTimer()` (A2). The subscribe and unsubscribe sit next to the existing `OnGameOver` ones. `HandleGameOver` stays unchanged, so it still saves the high score on the final lose only.

No change to `BlockDragAnalytics`. `game_lose` already listens to `OnGameOver`, which is now the final lose.

### Step 4 — Countdown logic (`BlockDrag.UI`, pure C#)

New file: `Assets/_Game/_UI/Scripts/Revive/ReviveCountdown.cs` (namespace `UI`)
- `ReviveCountdown(int seconds = 5, float skipDelay = 2f, float zeroHold = 0.3f, int redFromSecond = 2)`
- `void Start()`, `void Stop()`, `void Tick(float dt)`
- `int Shown` (5…0), `bool IsRed => Shown <= redFromSecond`, `bool IsRunning`
- Events: `OnSecondChanged(int)`, `OnSkipAvailable`, `OnExpired`. Each one fires at most once per step, and `OnExpired` fires once.

### Step 5 — `RevivePopup` rewrite (`BlockDrag.UI`)

File: `Assets/_Game/_UI/Scripts/RevivePopup.cs`. Replace the template code: remove the Heart, Gold and Video paths, `Locator.Gameplay`, `TopCanvas` and `levelTxt`.
- Serialized fields:
  - Buttons: `UIButton reviveButton`, `UIButton skipButton`, `CanvasGroup skipGroup`.
  - Texts: `TMP_Text countTxt`, `TMP_Text scoreTxt`.
  - Ring: `Image ringFill`, `Sprite ringYellow`, `Sprite ringRed`.
  - Colours: `Color numberNormal` (white), `Color numberRed` (`#F0444F`).
  - Timing: `float fillStepDuration = 0.2f`, `float skipFadeDuration = 0.2f`.
- `Open`: reset the `resolved` flag, set the score text (`N0`, InvariantCulture), set the fill to 1 with yellow, hide Skip (alpha 0, not interactable), start the countdown.
- `Update`: `countdown.Tick(Time.unscaledDeltaTime)`.
- `OnSecondChanged(n)`: set the text, colour and sprite by `IsRed`, then `ringFill.DOFillAmount(n / 5f, fillStepDuration).SetUpdate(true)`.
- `OnSkipAvailable`: fade the `skipGroup` alpha in and make it interactable.
- `OnExpired` and `skipButton` call `Decline()`. `reviveButton` calls `RequestRevive()`. Both go through a `resolved` guard.
- `TryShowReviveAd(Action onSuccess)`: for now it calls `onSuccess()` directly, with a `// TODO: Locator.Ads.Reward` comment.
- Listen to `Locator.BoardSession.OnGameRestarted` while open (the Home path), and close the popup without resolving.
- Kill tweens in `OnClose` and `OnDestroy`. Unsubscribe the buttons in `OnDestroy`.

File: `Assets/_Game/_UI/Scripts/GameplayCanvas.cs`
- In `BindSession`/`UnbindSession`, add `OnOutOfMoves += HandleOutOfMoves`.
- `HandleOutOfMoves`: if `CanRevive`, call `UIManager.Ins.OpenUI<RevivePopup>()`. Otherwise call `boundSession.ConfirmGameOver()`.
- `HandleGameOver` stays unchanged and opens `LoseCanvas`.

### Step 6 — Prefab `Assets/_Game/Resources/UI/RevivePopup.prefab`

Rebuild its contents in the Editor through Unity MCP, keeping the root GameObject and GUID so `UIManager` resources stay valid. Hierarchy, following design 3.5:

```
RevivePopup (Canvas root, RevivePopup component)
├─ Dim (Image black α 0.6, stretch, raycast)
└─ SafeArea (SafeAreaFitter, stretch)
   └─ Card (center, ScaleToFitParent designSize ≈ 900×1300)
      ├─ Panel (white rounded 9-slice from existing UI sprites)
      │   ├─ SubtitleTxt "No more moves left!"
      │   ├─ Ring: RingBg (countdown_ring_bg) / RingFill (Filled, Radial360, Top, Clockwise) / CountTxt
      │   ├─ DescTxt "Clear lines and keep your score"
      │   ├─ ScoreTxt (gold, TMP auto-size)
      │   ├─ ReviveBtn (Revive-selection + ic_heart + "REVIVE" + badge_ad)
      │   └─ SkipBtn (underlined TMP, CanvasGroup)
      └─ Ribbon (ribbon_purple + "CONTINUE?")
```

- Revive sprites: set the texture import type to Sprite (UI) for the `revive/` PNGs that are not Sprites yet. Set a 9-slice border on `ribbon_purple` and `Revive-selection` if needed.
- Fonts and materials: reuse the TMP font assets and outline materials that `LoseCanvas.prefab` uses, so the style matches.
- Popup open and close animation: keep the `UISCanvasTransition` that the prefab already uses.

### Step 7 — Tests (EditMode)

- New: `Assets/Tests/EditMode/ReviveHandTests.cs` (`HandGenerator.GenerateRevive`):
  - `GenerateRevive_OnNearlyFullBoard_FirstBlockCompletesALine`
  - `GenerateRevive_ReturnsThreeBlocksPlaceableInSequence`
  - `GenerateRevive_WhenNoLineCanComplete_FallsBackToAssistHand`
- New: `Assets/Tests/EditMode/ReviveCountdownTests.cs`:
  - `Tick_StepsShownFromFiveToZeroOnWholeSeconds`
  - `IsRed_StartsAtTwo`
  - `OnSkipAvailable_FiresOnceAtTwoSeconds`
  - `OnExpired_FiresOnceAfterZeroHold`
  - `Stop_PreventsExpiry`
- New: `Assets/Tests/EditMode/BlockSpawnerReviveTests.cs`, only if `BlockSpawner` can be driven in EditMode the way `GameOverCheckTests` does it:
  - `ConfirmGameOver_RaisesOnGameOverOnce`
  - `Revive_SecondTime_IsIgnored`
  - `RestartGame_ResetsCanRevive`

### Step 8 — Docs

- `.agents/rules/architecture.md`: in Gameplay Structure, describe the revive flow: `OnOutOfMoves` leads to `RevivePopup`, which leads to `Revive()` or `ConfirmGameOver()` and then `OnGameOver`. Also note that `OnGameOver` now means the final lose.

## Impacted files

| File | Change |
|------|--------|
| `_BlockDrag/Base/IBoardSessionService.cs` | modify |
| `_BlockDrag/BlockSpawner.cs` | modify |
| `_BlockDrag/ScoreManager.cs` | modify (combo stop on out of moves) |
| `_BlockDrag/Spawn/AssistHandBuilder.cs`, `HandGenerator.cs` | modify |
| `_UI/Scripts/Revive/ReviveCountdown.cs` (+ folder meta) | new |
| `_UI/Scripts/RevivePopup.cs` | rewrite |
| `_UI/Scripts/GameplayCanvas.cs` | modify |
| `Resources/UI/RevivePopup.prefab` | rebuild contents |
| `ComboBlock_UI_assets/revive/*.png.meta` | import settings (Sprite, borders) |
| `Tests/EditMode/BlockDragLocatorTests.cs` | update fake |
| `Tests/EditMode/ReviveHandTests.cs`, `ReviveCountdownTests.cs`, (`BlockSpawnerReviveTests.cs`) | new |
| `.agents/rules/architecture.md` | doc update |

There are no package, asmdef, scene or serialized-data migrations.

## Verification

1. **Compile.** `read_console` shows no errors after each script step.
2. **EditMode tests.** Run the full EditMode suite through Unity MCP `run_tests`. All tests pass, the new ones included.
3. **Play Mode, manual in `GameScene`:**
   - Fill the board until no move is left. RevivePopup opens and `LoseCanvas` does not.
   - The ring empties step by step with a smooth 0.2s tween. Yellow 5/4/3 turns red at 2/1/0. Skip appears at about 2s.
   - Let it time out. The popup holds on "0", then `LoseCanvas` opens. The high score is saved, and `game_lose` is logged once (Firebase debug log).
   - Tap Skip. `LoseCanvas` opens.
   - Tap REVIVE. 3 new blocks appear, and placing them clears at least one line. Score and high score are unchanged.
   - Get stuck a second time in the same run. `LoseCanvas` opens directly.
   - Try Again or Home, play a new run, and get stuck. The popup is offered again.
4. **Responsive.** Check the Game view at 720×1280, 1080×1920, 1080×2400, 1170×2532 (Simulator with a notch) and 1536×2048 (iPad). The card stays inside the safe area, nothing is clipped, and text does not overflow.
5. **Screenshots.** Take screenshots of the popup at 5 (yellow) and at 2 (red) for review.
