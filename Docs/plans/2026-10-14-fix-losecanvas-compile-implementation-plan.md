# Fix LoseCanvas compile error — Implementation Plan

Design: [2026-10-14-fix-losecanvas-compile-design.md](../designs/2026-10-14-fix-losecanvas-compile-design.md)

## Scope

This plan covers the compile fix only. The runtime risks listed in the design (null `topCanvas`, `BASE_GOLD` always 0, unused `System.Data`) are not part of it.

## Impacted files

| File | Change |
|---|---|
| `Assets/_Game/_UI/Scripts/LoseCanvas.cs` | Restore `using Quang.Utilities.Timer;` and remove the leftover blank or whitespace-only lines in the `using` block |

No `.asmdef`, prefab, scene, package, or serialized-data changes.

## Steps

1. Edit the `using` block inside `namespace UI` of `LoseCanvas.cs` so it reads:
   ```csharp
   using Quang.Data;
   using Quang.Base;
   using Quang.UI;
   using Quang.Utilities.Timer;
   using System;
   using System.Data;
   using TMPro;
   ```
   (`Quang.Common` stays removed.)
2. Trigger a Unity refresh or recompile.

## Verification

1. The Unity console shows no `CS0103 TimerManager` error and no other compile errors in `LoseCanvas.cs`.
2. `git diff` on `LoseCanvas.cs` shows only the removal of `using Quang.Common;` compared with `HEAD`.
