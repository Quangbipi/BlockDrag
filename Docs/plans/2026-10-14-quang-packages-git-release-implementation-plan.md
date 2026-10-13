# Switch `com.quang.*` packages to the git release — Implementation Plan

Design: [2026-10-14-quang-packages-git-release-design.md](../designs/2026-10-14-quang-packages-git-release-design.md)

Repos involved:
- **SRC**: `/Users/minhquang/Documents/PackUnity/Quang_Packages` (no remote, branch `main`)
- **REL**: `/Users/minhquang/Documents/PackUnity/Quang_Package_Release` (`origin` = `github.com/Quangbipi/Package_Release`)
- **GAME**: this BlockDrag project

## Step 1 — Fix `InitCanvas` (SRC)

File: `Packages/com.quang.base/Runtime/Init/InitCanvas.cs`

Replace the two commented-out lines with null-guarded calls:

```csharp
// Start()
if (clearDataButton != null)
    clearDataButton.onClick.AddListener(OnClearDataButtonClick);

// OnDestroy()
if (clearDataButton != null)
    clearDataButton.onClick.RemoveListener(OnClearDataButtonClick);
```

## Step 2 — Bump version (SRC)

- `Packages/com.quang.base/package.json`: `"version": "0.26.0"` → `"0.26.1"`
- `Packages/com.quang.base/CHANGELOG.md`: add `## [0.26.1] - 2026-10-14`, with the entry
  "`InitCanvas`: `clearDataButton` is optional; listener wiring is skipped when unassigned."
- Dependents' `"com.quang.base": "0.26.0"` stay as they are.

## Step 3 — Package guide

`HUONG_DAN_PACKAGES.md` stays in REL (already pushed in `a492a8c`), and nothing moves. REL is clean, so `publish.sh` can run.

## Step 4 — Commit SRC

`git add -A` in SRC. This stages the `InitCanvas` fix, the version bump, CHANGELOG, the ~198 `.meta` normalisations (GUIDs verified unchanged).
Message: `base: make InitCanvas clearDataButton optional (0.26.1)`.

## Step 5 — Publish

```bash
cd SRC && tools/publish.sh --push
```

Expected: a new tag `release-2026.10.13.1` is created and pushed. The commit message lists the
`base 0.26.0 -> 0.26.1` bump, plus "changed without version bump" for packages that differ only in
`.meta` files.
Verify with: `git ls-remote --tags origin` in REL shows the new tag.

## Step 6 — Point GAME at the release

`Packages/manifest.json`, replace the 6 `file:` entries with:

```
"com.quang.<id>": "https://github.com/Quangbipi/Package_Release.git?path=Packages/com.quang.<id>#release-2026.10.13.1"
```

for `designpattern, utilities, persistence, base, data, ui`.
Then trigger a Unity package resolve (`refresh_unity` via MCP, or refocus the Editor). Unity rewrites
`packages-lock.json` with `"source": "git"` and a commit `hash` for each package.

## Files changed

| Repo | File |
|---|---|
| SRC | `Packages/com.quang.base/Runtime/Init/InitCanvas.cs`, `package.json`, `CHANGELOG.md`, ~198 `.meta` |
| REL | publish commit: `Packages/**`, `release-index.json` |
| GAME | `Packages/manifest.json`, `Packages/packages-lock.json` |

No `BlockDrag` / `BlockDrag.UI` code, prefabs or scenes change.

## Verification

1. `packages-lock.json`: all 6 entries have `"source": "git"`, and `com.quang.base` is at `0.26.1`.
2. Unity console has no compile errors after the resolve (`read_console`).
3. Run EditMode tests (`run_tests`), which must pass as before.
4. Manual check: play `CryptoLoader.unity`. `Canvas_Debug` should open without a `NullReferenceException`, and Start enters the game.

## Rollback

Revert `Packages/manifest.json` and `packages-lock.json` to their `file:` versions. The pushed tag is kept as is (tags are immutable).
