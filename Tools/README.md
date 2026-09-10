# Automated two-player testing

Runs a real host + client match on one machine, unattended, and prints both sides as one
transcript. A full run — connect, create room, room list, join, start, Play scene, cosmetics —
takes about 22 seconds.

## Run

```powershell
# once, or after any script change: Unity menu -> Panic At The Pond -> Build Two Test Players
./Tools/run-multiplayer-test.ps1
./Tools/run-multiplayer-test.ps1 -HostFishHat "Fish Cap Hat" -ClientFishHat "Fish Black hat"
```

Exit code is 0 only when **both** sides report `RESULT-PASS`. Raw player logs are kept in
`Build/TestLogs/`.

Useful switches: `-Room <name>` (keep it to 10 characters, see below), `-TimeoutSeconds`,
`-KeepWindows` to leave the players open for inspection, `-RawLogs` to dump exceptions from the
raw logs.

## How it works

| | |
|---|---|
| `Assets/_Project/Scripts/Editor/MultiplayerTestBuild.cs` | Builds `Build/TestHost` and `Build/TestClient` |
| `Assets/_Project/Scripts/Testing/AutoTestDirector.cs` | Drives one side; dormant without `-autotest` |
| `run-multiplayer-test.ps1` | Launches both, merges the logs, reports |

Two design points that are load-bearing:

**Two separate builds, not two copies of one.** Unity keys PlayerPrefs on
`HKCU\Software\<company>\<product>`, so two instances of the same executable share a single save —
including the selected cosmetics. A bug of the form *"the hat shows on the other player but not on
me"* cannot even be expressed when both players read the same selection. Each test build gets its
own `productName`, so each gets its own save.

**The harness drives the game, it does not reimplement it.** Every step calls the same handler the
button calls (`CreateJoinManager.OnClickAction`, `RoomRowPrefab.SelectRoom`). A harness that
reproduced the flow itself would pass while the real path was broken.

`Application.runInBackground` is forced on at build time. Two windowed players means one is always
unfocused, and an unfocused Unity player stops its loop entirely — it stops sending, stops
receiving, and times out of the room.

## Gotchas

- **Room names are capped at 10 characters** by the create-room InputField. Longer names are
  silently truncated, so the host creates `AUTOTEST_4` while the client looks for `AUTOTEST_4679`
  forever. The harness reports the truncation rather than hanging.
- Everything the harness writes lives under `Build/`, which is already gitignored.
- The Editor defers script compilation while unfocused; if a change does not seem to take effect,
  focus the Editor or call `CompilationPipeline.RequestScriptCompilation()` before building.

## Visual checks

Logs prove state; they cannot prove appearance. A hat can report `enabled=True order=3 scale=2` and
still be drawn off the character's head. So every side screenshots **itself**:

- `shots/<role>-play.png` and `-play-late.png` are taken automatically during the match.
- With `-Hold`, each player refreshes `shots/<role>-hold.png` every 8 seconds, so anything triggered
  from the other side becomes visible within seconds.

Screenshots come from Unity's own `ScreenCapture`, not from grabbing the window off the desktop.
Windows refuses `SetForegroundWindow` to a background process, so a desktop grab of a background
player returns the wallpaper, and DPI scaling resizes it. Rendering from inside the player is immune
to focus, occlusion and scaling.

For the deepest checks, run **hybrid mode**: the Editor takes one seat (`StartInEditor`) and a built
player the other. The Editor side has MCP attached, so it can be screenshotted *and* inspected —
`GameManager.Instance.LoadSpawnFisherman()` from the Editor as master, for example, drives the
fish→fisherman role transition that a passive harness never reaches.

## Audio

The harness pins `AudioListener.volume` to 0 and `AudioListener.pause` to true for the whole run,
re-applying every frame. Unattended runs are silent, and two players cannot fight over the audio
device.

## Reading the output

`COSMETIC` lines are the interesting ones. Each is tagged `MINE` or `remote(actor N)`, so a
cosmetic that replicates correctly appears **twice per player** across the two logs — once as
`MINE` on its owner's side and once as `remote` on the other's. A hat missing from one of those two
places is the asymmetric bug, visible at a glance.
