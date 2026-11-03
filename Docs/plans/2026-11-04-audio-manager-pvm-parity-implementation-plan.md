# `com.quang.audio` 0.2.0 — match PetVsMonster `_Audio` — Implementation Plan

Design: [2026-11-04-audio-manager-pvm-parity-design.md](../designs/2026-11-04-audio-manager-pvm-parity-design.md). Decisions: keep it as a package; apply fixes 1–4 (in the package, and in BlockDrag's usage).

- **SRC** = `/Users/minhquang/Documents/PackUnity/Quang_Packages` (`main`, HEAD `246027b`)
- **BD** = `/Users/minhquang/Documents/Unity Project/BlockDrag` (`feature/audio-package`, HEAD `0c4a42b`)

## Step 1 — rewrite the package (SRC, `Packages/com.quang.audio`)

| File | Change |
|---|---|
| `Runtime/AudioData.cs` (new) | `namespace Quang.Audio`. `[CreateAssetMenu(fileName = "AudioData", menuName = "ScriptableObjects/System/AudioData")] AudioData : SerializedScriptableObject` with Odin-serialized `Dictionary<BGM_TYPE, Audio> _bgmAudioDict`, `Dictionary<SFX_TYPE, Audio> _sfxAudioDict` (`[Title("BGM")]`/`[Title("SFX")]`), read-only properties `BgmAudioDict`/`SfxAudioDict`. `[Serializable] class Audio { AudioClip clip; [Range(0,1)] float multiplier = 1; }`. The reference's unused `BGMList`/`SFXList` are left out |
| `Runtime/AudioManager.cs` | rewritten after the reference: `[Serializable] struct AudioSourceData` (`audioSource`, `audioSourcePool`, `currentAudio`, `baseVolume`, `SetLoop`, `SetAudio`, `Play`, `Pause`, `Stop`, `IsPlaying`, `BaseVolume`, `Volume`, `IsMute`) and `AudioManager : Singleton<AudioManager>, IAudioService` (`audioData`, `audioSourcePool`, `bgm`, `sfx`, `loopSfxAudioSource`; `BgmVolume`, `SfxVolume`, `IsBgmMute()`, `IsSfxMute()`). Fades via `DOVirtual.Float(...).SetEase(Ease.Linear).SetUpdate(true)` (unscaled, like `Quang.UI` anims) |
| — fix 1 | `Awake`: `GameData data = Locator.Data?.GetData<GameData>()`; mute = `data?.setting.isBgmMute ?? false` (same for SFX) |
| — fix 2 | `Awake` starts with: `if (Locator.Audio != null && !ReferenceEquals(Locator.Audio, this)) { Destroy(gameObject); return; }`. `OnDestroy` clears `Locator.Audio` only if it is still this, and kills its tweens |
| — fix 3 | `PlayBgm`: fade out only when `bgm.IsPlaying && !bgm.IsMute && fadeOut > 0`; otherwise switch and play immediately. A pending BGM fade is killed before a new one starts |
| — fix 4 | pooled pop goes through `TryPop`: catches the "no free index" case of `ObjectContainer.Pop` by checking a free slot first (a count of in-use sources kept by the manager); if none is free, `SetAudio` reuses the current source (restarts it). `PlayLoopSfx` returns `null` when the pool is exhausted |
| `Runtime/AudioLibrarySO.cs(.meta)`, `Runtime/SfxSourcePool.cs(.meta)` | deleted |
| `Runtime/Quang.Audio.asmdef` | references `Quang.Base`, `Quang.Data`, `Quang.DesignPattern` (DOTween / Odin are precompiled and auto-referenced, as in `Quang.Data`) |
| `Runtime/AssemblyInfo.cs` | unchanged (`InternalsVisibleTo("Quang.Audio.Tests")`) |
| `package.json` | `0.2.0`; deps `com.quang.base 0.27.0`, `com.quang.data 0.12.6`, `com.quang.designpattern 0.4.5`; description updated |
| `Tests/AudioLibrarySOTests.cs`, `Tests/SfxSourcePoolTests.cs` (+ meta) | deleted |
| `Tests/AudioManagerTests.cs` | rewritten; the manager is built in code (GameObject + `BGM`/`SFX` children + `ObjectContainer` with `Amounts=[3]`), with an `internal` init entry point so tests do not rely on `Awake`: `Init_RegistersLocatorAudio`, `Init_WhenAnotherRegistered_DestroysSelf` (fix 2), `Init_WithoutDataService_IsUnmuted` (fix 1), `ToggleSfxVolume_True_SetsVolumeZero`, `PlaySfx_WhenSfxBusy_UsesAnotherPooledSource`, `PlaySfx_WhenPoolExhausted_DoesNotThrow` (fix 4), `PlayBgm_WhenNothingPlaying_SwitchesImmediately` (fix 3), `PlaySfx_WhenTypeMissing_DoesNothing` |
| `Tests/AudioSourceDataTests.cs` (new) | `Volume_IsBaseVolumeTimesMultiplier`, `IsMute_WhenBaseVolumeZero_ReturnsTrue` |
| `Tests/Quang.Audio.Tests.asmdef` | add `Quang.Data`, `Quang.DesignPattern` |
| `README.md`, `CHANGELOG.md` | new structure (prefab hierarchy, `AudioData`, `ObjectContainer` setup), `## [0.2.0]` marked **breaking** (removed types, new deps), fixes 1–4 listed |

## Step 2 — check against the local source (BD)

1. Temporarily: `com.quang.base`, `com.quang.audio` → `file:` paths in SRC; `"testables": ["com.quang.audio"]`.
2. `refresh_unity` → `read_console`: no compile errors. Note: until Step 4, the BlockDrag prefab still points at `AudioLibrarySO` fields (harmless missing-field warnings only).
3. `run_tests` `Quang.Audio.Tests` → all green.

## Step 3 — BlockDrag assets (BD, done with the local package still in place)

| Item | Change |
|---|---|
| `Assets/_Game/_Data/Audio/AudioData.asset` (new) | via Editor code: `BLOCK_PLACE` → `sfx_place_block`, `LINE_CLEAR` → `sfx_line_clear`, `LOSE` → `sfx_game_over` (multiplier 1); `BACK_GROUND_1` → `bgm_main_loop` (multiplier 0.5) |
| `Assets/_Game/_Data/Audio/AudioLibrary.asset(.meta)` | deleted |
| `Assets/_Game/_Base/AudioManager.prefab` | rebuilt **in place** (same file, same GUID): remove extra components; root = `AudioManager` + `ObjectContainer` (`MainObjects=[SFX]`, `Amounts=[10]`); children `BGM` (AudioSource, loop, playOnAwake off) and `SFX` (AudioSource, playOnAwake off); `audioData`, `audioSourcePool`, `bgm.audioSource`, `sfx.audioSource` wired. The `AudioManager` script GUID stays the same (same `.cs.meta`), so the scene instances in `LoadStart` and `GameScene` keep working |
| `Assets/_Game/_BlockDrag/Audio/BlockDragAudio.cs` | `Start` only calls `Locator.Audio?.PlayBgm(gameplayBgm)`; remove the `GameData` mute block and update the summary |
| `.agents/rules/architecture.md` | `Quang.Audio` row: `AudioManager` (`Singleton`, `AudioSourceData` bgm/sfx, `ObjectContainer` SFX pool, reads mute from `GameData`), `AudioData` (Odin dictionaries); `BlockDragAudio` line: drop "saved mute state" |

## Step 4 — release (SRC → GitHub) ⚠ outward-facing

1. Commit SRC: `audio: rebuild after PetVsMonster AudioManager (0.2.0)`.
2. `tools/publish.sh --push` → `release-2026.11.04.N`.
3. BD manifest: all `com.quang.*` → new tag; remove the `file:` entries and `testables`. Resolve; check `Library/PackageCache/com.quang.audio@*` has `AudioData.cs` and no `AudioLibrarySO.cs`.

## Step 5 — verification

Automated:
- `read_console`: no errors; no missing-script / missing-field warnings on `AudioManager.prefab`.
- EditMode tests: `Quang.Audio.Tests` (run once with the local package in Step 2) and BD `EditModeTests` (expected only the 6 known `GameOverCheckTests` failures).

Play Mode (stepping frames, since the Editor is unfocused):
1. From `LoadStart`: after `GameScene` loads → exactly 1 `AudioManager` (DontDestroyOnLoad), hierarchy `BGM` + `SFX` ×10; `BGM` source playing `bgm_main_loop` at volume 0.5; exactly 1 enabled `AudioListener`; no console errors.
2. `PlaySfx` BLOCK_PLACE / LINE_CLEAR / LOSE → pooled `SFX` sources playing; 12 `PlaySfx` in one frame → no exception (fix 4).
3. `ToggleSfxVolume(true)` → SFX volume 0; `ToggleBgmVolume(true)` → BGM volume 0; back to normal on `false`.
4. `GameScene` played directly → still works (fix 1), one manager.

Manual (you): listen to BGM, place, line clear, game over; toggle sound/music in Settings.

## Step 6 — commit (BD)

`Audio: move to com.quang.audio 0.2.0 (AudioData, BGM/SFX child sources)`, including `.meta` files. No push / PR unless asked.

## Rollback

Before Step 4: revert SRC changes and the BD manifest. After Step 4: the tag stays; in BD revert to `#release-2026.11.03.1` and restore the previous prefab/asset from git (`0c4a42b`).
