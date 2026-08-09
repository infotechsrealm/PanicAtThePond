# Panic At The Pond — Project Overview

> **Living document.** This file is the single source of truth for how the project is put together.
> It must be updated whenever code, assets, config, or project settings change.
>
> - **Last full audit:** 2026-07-29
> - **Audited against commit:** `a1457f36` ("New shop assests are added") + uncommitted working-tree changes
> - **Structure:** migrated to the `Assets/_Project/` layout on 2026-07-29 — see §0.
> - **Migration detail:** `Assets/_Project/restructure_report.md` covers the restructure itself —
>   what is done, what remains, risk tiers, and rollback.
> - **Scope of audit:** every `.cs` file under `Assets/Scripts` and `Assets/Editor` (96 files, ~28,032 lines),
>   `ProjectSettings/`, `Packages/manifest.json`, `Assets/StreamingAssets/`, `Assets/Resources/`,
>   `.mcp.json`, `.claude/`, and the scene/prefab inventory.

---

## 0. Project Structure (ruleset compliance)

Migrated 2026-07-29 to the mandated `Assets/_Project/` layout. Spec:
`D:\Unity_ai_Project_Structure_prompt.md` **v3.0** (Unity 6.5), which supersedes the earlier v2.0.

**Scope note:** UI Toolkit (spec §3) is **explicitly waived by the user** — this project has extensive
existing uGUI and keeps it. Every other section of the spec has been applied or has a documented
reason it cannot be. See `restructure_report.md` for the full migration record.

### Scene partitioning (spec §1.3)

Scripts are split into a **shared tier** (parent folders) and **scene tiers** (`Dash/`, `Play/`,
`Splash/`). Ownership was computed from scene→prefab GUID closure plus a transitive code-reference
pass. **70 of 101 scripts are genuinely shared**; only 11 are scene-exclusive, and all 11 are leaves
with no inbound references.

### Assemblies (spec §1.6)

`PanicAtThePond` (shared) · `PanicAtThePond.Dash` / `.Play` / `.Splash` (scene tiers, `autoReferenced:
false`) · `PanicAtThePond.Editor` (Editor-only). Scene assemblies reference the shared assembly and
never each other. `DOTween.Modules.asmdef` was added to the vendor DOTween `Modules/` folder because
its uGUI shortcuts ship as source.

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── PanicAtThePond.asmdef      ← shared-tier assembly
│   │   ├── Managers/      (21)  GameManager, GS, DashManager, ScoreManager, PlayFabManager,
│   │   │                        CoinManager, HungerSystem, MashPhaseManager, MiniGameManager,
│   │   │                        BackManager, RegionManager, HoverTooltipManager, InGameMenu,
│   │   │                        CustomNetworkManager, CoustomeRoomManager, PhotonLauncher,
│   │   │                        LANDiscoveryMenu, AudioManager, UIManager, PoolManager,
│   │   │                        InputManager
│   │   ├── Controllers/   (13)  Fish/Fisherman controllers + _Mirror twins, GoldenFishAI,
│   │   │                        all Fisherman animation scripts
│   │   ├── Gameplay/      (13)  Hook, Worm/Junk spawners + managers + _Mirror twins,
│   │   │                        EnvironmentScatterManager, SmoothTransformSync, DestroyAfterAnim
│   │   ├── UI/            (27)  menus, panels, lobby screens, tables, dropdowns, Preloader,
│   │   │                        GameOver, SettingsMenu
│   │   ├── Shop/          (10)  ShopManager, CosmeticRuntimeApplier, LocalPlayManager,
│   │   │                        ShopCosmeticSelector + the 6 SaltShop/* files
│   │   ├── Data/           (2)  ScoreSystemSettings, PlayerControls.cs (generated)
│   │   │                        + PlayerControls.inputactions
│   │   ├── Utilities/      (3)  LegacyTextSharpener, FishermanSpriteLoader,
│   │   │                        ResetStatsAchievements
│   │   ├── Dash/           (8)  PanicAtThePond.Dash.asmdef
│   │   │                        UI/ ButPanelManager, CreaditsManager, HintsManager,
│   │   │                            PauseManager, RoomFilterManager
│   │   │                        Utilities/ AutoBlockRaycastOnInputClick, RaycastBlockerFinder
│   │   │                        Managers/ SteamIntegration
│   │   ├── Play/           (1)  PanicAtThePond.Play.asmdef · Gameplay/ GameScaler
│   │   ├── Splash/         (2)  PanicAtThePond.Splash.asmdef
│   │   │                        UI/ SplashManager · Utilities/ SceneObjectImageSaver
│   │   ├── Editor/         (1)  FixUIEditor + PanicAtThePond.Editor.asmdef
│   │   ├── Interfaces/     (1)  IPoolable
│   │   ├── Events/  Enums/       (empty, .gitkeep — reserved by the ruleset)
│   ├── Art/          Animations/, Fonts/
│   ├── Audio/        Panic at the Pond SFX/
│   ├── Prefabs/      Prefebs/  ← original misspelling preserved deliberately
│   ├── Resources/    runtime-loaded prefabs, controllers, ShopUI art
│   ├── Scenes/       Splash, Dash, Play
│   ├── ScriptableObjects/, UI/, Settings/, ScriptTemplates/, _Recovery/
│   ├── project_overview.md   ← this file
│   └── Handoff.md            ← historical LAN-fixes handoff doc
├── ThirdParty/       Mirror, PlayFabSDK, PlayFabEditorExtensions,
│                     com.rlabrecque.steamworks.net (incl. SteamManager.cs),
│                     Unity-Logs-Viewer, Plugins
├── Photon/           ⚠ NOT moved — see below
├── TextMesh Pro/     ⚠ NOT moved — see below
└── StreamingAssets/  MUST stay here (Unity hard requirement; ShopConfig reads
                      Application.streamingAssetsPath)
```

### Deliberate exceptions

| Folder | Why it stayed in `Assets/` root |
|---|---|
| `StreamingAssets/` | Unity only recognises StreamingAssets at `Assets/StreamingAssets`. Not movable. |
| `Photon/` | 7 Photon files hardcode their own `Assets/Photon/...` path, incl. `PhotonAppSettings.cs` (settings regeneration) and `PhotonGUI.cs` (inspector icons). Moving it requires editing vendor source, which contradicts the ruleset's own "ThirdParty — untouched" principle. |
| `TextMesh Pro/` | TMP resolves `TMP_Settings` via `Resources.Load` and Unity regenerates the folder at `Assets/TextMesh Pro` on reimport. Deferred to a separately-verified step. |
| `Plugins/NuGet/` | **Auto-regenerated.** The `com.ivanmurzak.unity.mcp` package's dependency resolver writes its DLLs to this hardcoded path on load. The project's real plugin content (DOTween) *was* moved to `ThirdParty/Plugins/` and resolves correctly from there. Do not fight this folder — it will come back. |
| `ScriptTemplates/` | **Auto-regenerated.** Mirror re-installs its `NewNetworkBehaviour` / `NewNetworkManager` templates here on load. |

### Migration safety record
- All moves done with `git mv`, with Unity **closed** (verified via `Temp/UnityLockfile` not being held).
- Restore point: `D:\ITR\Git\Unity\_PATP_BACKUP_pre-restructure` (5.8 MB, 270 files, all scenes + prefabs).
- **Verified: 0 of 97 script GUIDs changed**, and every `.cs` still has its paired `.meta`. Scene and
  prefab references resolve by GUID, so nothing was broken by the move.
- `ProjectSettings/EditorBuildSettings.asset` scene paths updated to `Assets/_Project/Scenes/`
  (GUIDs already matched).

### Namespaces (done 2026-07-29)
All 96 own scripts carry `namespace PanicAtThePond.<Module>` matching their folder
(`Managers`, `Controllers`, `Gameplay`, `UI`, `Shop`, `Data`, `Utilities`, `Editor`).

Because the codebase is heavily cross-coupled (`GameManager` alone reaches into `FishController`,
`FishermanController`, `WormSpawner`, `JunkSpawner`, `HungerSystem`, `MashPhaseManager`,
`ScoreManager` and `GS`), every file also received a blanket set of
`using PanicAtThePond.*;` imports for the seven **runtime** modules.
`Events`, `Interfaces` and `Enums` are deliberately excluded — they are still empty, so importing
them would be a `CS0246`. `Editor` is excluded from runtime files because it compiles into a
separate assembly.

Encoding was preserved byte-for-byte (BOM presence and line endings per file); these sources contain
emoji and Hindi text inside `Debug.Log` strings that a naive rewrite would corrupt.

### Infrastructure Managers (added 2026-07-29, not yet wired)
Five new files, written to the ruleset's style rules (singleton `Instance`, duplicate-destroy in
`Awake`, `[SerializeField] private` + `_camelCase`, `UPPER_SNAKE_CASE` constants, XML `<summary>` on
public members, prescribed member order, cleanup method). They are **purely additive** — no existing
script references them yet, so behaviour is unchanged.

| File | Purpose |
|---|---|
| `Scripts/Interfaces/IPoolable.cs` | `OnSpawn()` / `OnDespawn()` hooks for pooled types. |
| `Scripts/Managers/PoolManager.cs` | Prefab-keyed pool with `Get` / `Release` / `Prewarm` / `Cleanup`. **Local objects only** — the project's 32 spawn sites are `PhotonNetwork.Instantiate` / `NetworkServer.Spawn`, which own their own net IDs and lifecycle. |
| `Scripts/Managers/AudioManager.cs` | `PlaySfx` / `PlayMusic` / `StopMusic` over a fixed voice pool. Reuses the existing PlayerPrefs keys `MasterVolume`, `MusicVolume`, `SFXVolume` so it stays consistent with `GS` and `SettingsMenu` during migration. |
| `Scripts/Managers/UIManager.cs` | uGUI panel registry: `RegisterPanel` / `ShowPanel` / `HidePanel` / `HideAllPanels`. Panels self-register, avoiding `GameObject.Find`. |
| `Scripts/Managers/InputManager.cs` | Sole reader of raw input; exposes `OnMoveChanged`, `OnCastStarted/Released`, `OnReelPressed`, `OnMashPressed`, `OnDropJunkPressed`, `OnBackPressed`. |

**Input asset caveat (fact, not assumption):** `PlayerControls.inputactions` has
`generateWrapperCode: 0`, no generated `PlayerControls.cs` exists, and its only actions are Unity's
default template (`Move` / `Look` / `Fire`). Those do not describe this game, whose real controls are
Horizontal/Vertical axes, `W`/`S` rod selection, hold `X`+`V` to charge a cast, right-click to reel,
`Space` to mash, `Q` to drop junk, `Esc` to go back. `InputManager` therefore models the **real**
scheme. The ruleset's "generate a C# class from the asset" step only becomes meaningful once the
asset is authored to match — that is the prerequisite, and it is still outstanding.

### Field encapsulation (done 2026-07-29, partial by design)
**153 public fields** converted to `[SerializeField] private`. The field **name is unchanged**, so
Unity's serialization and every Inspector wiring is preserved exactly — this is why the pass is safe.

Of the original 478 public fields:
- **153 converted** — referenced only inside their own type.
- **161 left public** — read cross-script (`GameManager.Instance.myFish`,
  `FishermanController.Instance.catchadFish`, …). Encapsulating these requires public properties plus
  call-site updates; that is the next tier.
- **154 left alone** — `static` / `const` / `readonly` / already-attributed / Mirror `[SyncVar]`.
- **11 reverted** after compile — see below.

Two traps worth recording:
1. A field regex whose tail is `(?:=[^;]*)?;` also matches **expression-bodied properties**
   (`public Vector2 MoveInput => _moveInput;`), silently producing invalid
   `[SerializeField] private ... =>`. Four occurrences, all caught and fixed.
2. A cross-*file* reference check is not sufficient. **C# does not let an enclosing type read a nested
   type's private members**, which produced 81 `CS0122` errors from same-file access. The 11 affected
   members were all DTO/message fields and were reverted to public:
   `ShopManager.{CosmeticPreviewRule.CosmeticName, HatIconOverride.HatNameSubstring}`,
   `FishermanAnimationManager.{BodyPartAnimator.partName, AnimationInfo.rowLeft, AnimationInfo.totalFrames}`,
   `CustomNetworkManager.{GameModeMessage.gameMode, PlayerListMessage.allPlayerNames, SaltShopStateMessage.stateJson}`,
   `SaltShopService.{SaltShopState.windowStartUtcTicks, SaltShopState.ShopItem.displayName}`,
   `RoomRowPrefab.LANRoomInfo.fadText`.

   **Standing rule:** leave fields of plain `[Serializable]` data/message classes public — they are
   DTOs. Only encapsulate MonoBehaviour fields.

Verified: `Assembly-CSharp.dll` + `Assembly-CSharp-Editor.dll` rebuilt 10:06:53, 0 compile errors,
0 missing script references.

### Outstanding ruleset gaps (not yet done)
The remaining 161 cross-script public fields (need properties), `_camelCase` renaming, asset-name
conventions, Addressables, authoring `PlayerControls.inputactions` + migrating the 15 legacy
`Input.` call sites onto `InputManager`, wiring the 10 direct audio calls onto `AudioManager`, and the
`Managers/Systems/UI/Environment/Gameplay/Debug` scene root structure. See §13 and the risk table
below.

### Why the remaining rules are not simply "apply and done"
Measured against this codebase, not assumed:

| Rule | Measured scope | Consequence of applying blindly |
|---|---|---|
| `_camelCase` private fields | **563 public fields** | Unity serializes by field *name*. Renaming silently voids all 1,431 MonoBehaviour wirings across Dash (1089), Play (299) and Splash (43) plus every prefab, unless each field also gets `[FormerlySerializedAs]` **and** every scene/prefab is re-serialized. |
| Asset naming `SP_`/`M_`/`ANIM_`/`SFX_` | **104 `Resources.Load("literal")` calls** | Breaks those calls, the `iconResource` paths in `shop_config.json`, and `CosmeticRuntimeApplier`'s `name.Contains("blue_cap")`-style matching. |
| Addressables instead of `Resources.Load` | 104 call sites; package **not installed** | Converts the entire cosmetics pipeline from synchronous to asynchronous. |
| PoolManager / `IPoolable` | 32 `Instantiate` sites | They are `PhotonNetwork.Instantiate` / `NetworkServer.Spawn`. Pooling networked spawns is a networking redesign, not a refactor. |
| New Input System only | 15 legacy `Input.` calls | Feasible, but only verifiable by actually playing the game. |
| Scene root hierarchy | 3 scenes, 1,431 components | Reparenting a 2.4 MB scene is unverifiable without opening and diffing it in the Editor. |

---

## 1. Identity & Build Target

| Field | Value | Source |
|---|---|---|
| Product name | `Panic At The Pond` | `ProjectSettings/ProjectSettings.asset` |
| Company | `InfoTechsRealm` | `ProjectSettings/ProjectSettings.asset` |
| Bundle version | `1.0` | `ProjectSettings/ProjectSettings.asset` |
| Unity Editor | `6000.5.4f1` (rev `d550df8bd089`) | `ProjectSettings/ProjectVersion.txt` |
| Render pipeline | URP `17.5.0` (`Assets/Settings/UniversalRP.asset`, `Renderer2D.asset`) | `Packages/manifest.json` |
| Reference resolution | 1920×1080 (web fallback 960×600) | `ProjectSettings/ProjectSettings.asset` |
| Input handling | `activeInputHandler: 2` — **both** old Input Manager and new Input System are enabled | `ProjectSettings/ProjectSettings.asset` |
| Steam app id file | `steam_appid.txt` present at repo root (contents: `480`, Valve's Spacewar test id) | repo root |
| Last build stamp | `Build from MP_GOPANI at 7/29/2026 8:17:54 AM` | `Assets/StreamingAssets/build_info` |

**Scenes in build (in order):**
1. `Assets/_Project/Scenes/Splash.unity` — 111 KB, 43 MonoBehaviours
2. `Assets/_Project/Scenes/Dash.unity` — 2.39 MB, 1089 MonoBehaviours (the main menu / lobby / shop hub — by far the largest scene)
3. `Assets/_Project/Scenes/Play.unity` — 762 KB, 299 MonoBehaviours (the match scene)

**Tags:** `Fisherman`, `Fish`, `Worm`, `GoldTrout`, `Junk`, `Worm2`, `Bullet`, `HookWorm`, `Water`
**Layers (user):** `Water`(4), `UI`(5), `Fish`(6), `FisherMan`(7), `Worm`(8), `HookWorm`(9), `Hook`(10), `Junk`(11), `GoldFish`(12)

---

## 2. Third-Party Stack

Installed via UPM (`Packages/manifest.json`) plus a scoped registry for `package.openupm.com`
(scopes `com.ivanmurzak`, `extensions.unity`):

- **com.unity.render-pipelines.universal** 17.5.0 — URP / 2D renderer
- **com.unity.feature.2d** 2.0.2 — full 2D toolset (Animation, Aseprite, PSD importer, SpriteShape, Tilemap Extras, IK, Pixel Perfect)
- **com.unity.inputsystem** 1.19.0 — `Assets/PlayerControls.inputactions`
- **com.unity.ugui** 2.5.0, **com.unity.timeline** 1.8.12, **com.unity.visualscripting** 1.9.11
- **com.unity.nuget.newtonsoft-json** 3.2.2
- **com.unity.multiplayer.center** 1.0.1, **com.unity.test-framework** 1.7.0
- **com.unity.ai.assistant** 2.7.0-pre.2 (Unity AI Assistant / generators / MCP editor packages)
- **com.ivanmurzak.unity.mcp** 0.86.1 + **.animation** 1.2.21 — Unity MCP plugin (drives the `.claude/skills/*` tool set)

Vendored into `Assets/` (not UPM):
- **Mirror** (`Assets/Mirror`) — LAN networking, incl. `Mirror.Discovery`, Telepathy, kcp2k, SimpleWebTransport, EncryptionTransport, Edgegap
- **Photon PUN 2 + Realtime + Chat** (`Assets/Photon`) — online networking
- **PlayFab SDK + Editor Extensions** (`Assets/PlayFabSDK`, `Assets/PlayFabEditorExtensions`) — accounts, currency, user data
- **Steamworks.NET** (`Assets/com.rlabrecque.steamworks.net`) — achievements, friends, avatar
- **DOTween** (`Assets/Resources/DOTweenSettings.asset`) — splash and hint animations
- **glTFast**, **Unity-Logs-Viewer**, **Extensions.Unity.PlayerPrefsEx**, **ScriptablePacker**, **PsdPlugin**, **HFDownloader**
- **McpPlugin.dll / McpPlugin.Common.dll / ReflectorNet.dll** in `Assets/Plugins/NuGet`

---

## 3. What the Game Is

An asymmetric multiplayer 2D fishing game. Up to **7 players** join a room. Everyone starts as a
**Fish** swimming under the waterline. One player becomes the **Fisherman** in a boat above the
waterline by eating the **Golden Fish**. The Fisherman casts hooks baited with worms; fish must eat
free-floating worms to keep a **hunger bar** from draining while dodging hooks.

### Round flow (facts from `GameManager`, `FishController`, `FishermanController`, `Hook`)
1. `Play` scene loads. `GameManager.Start()` waits for the network to be ready (`SpawnPlayerWhenReady`, 10 s timeout with a forced-spawn fallback), then spawns one fish per player.
2. `WormSpawner` starts spawning worms; after `Random.Range(5,10)` seconds it spawns the **Golden Fish**.
3. A fish that collides with the Golden Fish (`FishController.OnTriggerEnter2D`) sets `GameManager.isFisherMan = true`, unlocks `WHAT_A_SNACK`, destroys all worms, and triggers `GameManager.LoadSpawnFisherman()`. In Photon this also does a **master-client handover** (`GetIdAndChangeHost` → `ChangeHostById`) so the new Fisherman has authority.
4. The Fisherman gets `fishermanWorms = (totalPlayers - 1) * baseWormMultiplier` (default multiplier **3**) worms in a bucket.
5. **Casting:** hold `X` **and** `V` to charge the cast meter, release to cast. Requires a rod selected via `W`/`S` (`HandleRodSelection`, left rod = `moveInputY == 1`, right rod = `-1`). Cast distance = `castingMeter.value * maxCastDistance`. Right-click reels the hook back in.
6. **Fish caught on hook** → `MiniGameManager.StartMiniGame()`: a random 3-letter A–Z sequence must be typed within `fishTimerSeconds` (default **3 s**). Success = fish escapes with the worm (+75 hunger). Fail → `MashPhaseManager.StartMashPhase()`.
7. **Mash phase:** mash `SPACE` to fill a slider to 100. Difficulty = `100 / Random.Range(spacebarJamMin, spacebarJamMax)`; the mash gets easier by 10 units per prior escape (clamped 15–70). Fisherman wins → fish is parented into the hook and reeled up. Fish wins → +75 hunger, escape counter++.
8. **Win conditions** (`FishermanController.CheckWorms`):
   - Fisherman caught `>= totalPlayers - 1` fish → **"Fisherman Win!"**
   - Fisherman's worm bucket hits 0 → **"Fisherman Lose! / Fishes Win!"**
   - All fish starve (hunger bar reaches 0 with no fisherman round active) → **"You all Starve!"** (a tie)

### Game modes (`GameModeDropdownHandler`, `GS.currentGameMode`)
| Index | Name | Behaviour |
|---|---|---|
| 0 | Quick Survivalist | Single chaotic round. No points; uses the plain Game Over panel. |
| 1 | Quick Cast | 1 scored round → score screen → winner screen. |
| 2 | Deep Sea Fishing | 5 scored rounds (`GS.currentRound` 1→5), replaying `Play` between rounds, then winner screen. |

A **starvation tie** in modes 1 and 2 sets `GS.MarkTiePreloderForDash()` and returns everyone to `Dash` with a "tie" preloader shown for `max(7 s, tiePreloderReturnDelay)`.

### Water-visibility modes (`DropdownHandler`, applied in `FishermanController.ApplyVisibilityMode`)
| Dropdown | Flag on `GS` | Stated effect |
|---|---|---|
| Clear Waters | `ClearWaters` | Both sides can see each other |
| Murky Waters | `MurkyWaters` | Neither side can see each other |
| Deep Waters | `DeepWaters` | Fisherman can't see fish; fish can see him |
| Reflective Waters | `ReflectiveWater` | Fisherman can see fish; fish can't see him |

The water overlay is guarded by `SetWaterVisible()`, which will **only ever** enable it for the local Fisherman — fish-side clients never see it regardless of mode.

### Maps / backgrounds (`GameManager.AssignRandomBackground`)
- `possibleBGSprites` + `possibleWaterSprites` are index-paired arrays.
- Offline: a genuine `Random.Range` pick per session.
- Networked: a **deterministic** index derived from `GS.playAgainCount * 7919 + 104729`, mixed with an FNV‑1a hash of the Photon room name, so every client picks the same map without an extra RPC. The global RNG state is saved and restored around the call.
- `BG_2` uses the animated GIF background (`AnimatedBackground` + `Fishingshop2Frames`-style frame arrays), caps fish `maxBounds.y` at `-0.4`, sets fisherman spawn Y to `1.6` and `maxX` to `6`.
- `background-fishing` → fisherman Y `1.3`; `background-fishing-largemap` → Y `1.7`, water overlay `Top` `1.999999` (vs `15.49511` for BG_2).
- Cloud overlays (`clouds_1_5`, `clouds_1_0`) are always force-disabled.

---

## 4. Networking Architecture

The project runs **two complete networking stacks side by side**, selected by the `LAN` toggle in the
Create/Join panel, which sets `GS.Instance.isLan`.

```
                       GS.Instance.isLan
                     ┌────────┴────────┐
                  true                false
              Mirror (LAN)         Photon PUN 2 (online)
```

Almost every gameplay script has an `if (GS.Instance.isLan) { … } else { … }` fork. Mirror-side logic
lives in paired `*_Mirror` `NetworkBehaviour` classes; Photon-side logic uses `[PunRPC]` on the
main class.

| Photon class | Mirror partner |
|---|---|
| `FishController` | `FishController_Mirror` |
| `FishermanController` | `FishermanController_Mirror` |
| `Hook` | `Hook_Mirror` |
| `JunkManager` | `JunkManager_Mirror` |
| `WormManager` | `WormManager_Mirror` |
| `WormSpawner` | `WormSpawner_Mirror` |
| `JunkSpawner` | `JunkSpawner_Mirror` (currently an empty stub) |
| `GoldenFishAI` | `GoldenFishAI_Mirror` |

### Mirror / LAN
- `CustomNetworkManager : NetworkManager` (`Instence` static). Auto-registers `Resources/Fish` and `Resources/Fisherman` as spawn prefabs in `Awake`.
- Custom `NetworkMessage` structs: `PlayerNameMessage`, `PlayerListMessage`, `VisibilityMessage`, `GameModeMessage`, `ScoreSystemConfigMessage`, `SaltShopStateMessage`.
- On a client joining, the host pushes visibility mode, game mode, the full score-system config, and the resolved Sal‑T shop rotation to that connection specifically.
- `LANDiscoveryMenu` does **port-sweeping discovery**: it scans broadcast ports starting at `baseBroadcastPort` (and 47777 in some paths), stopping after 15 consecutive silent ports. Hosting finds a free TCP game port + free UDP broadcast port, capped at port **7792**, then `StartHost()` + `AdvertiseServer()`. Transport is `TelepathyTransport`.
- Room password, room name, player count, max players and the broadcast port all ride along in the Mirror discovery `ServerResponse`.

### Photon / online
- `CoustomeRoomManager : MonoBehaviourPunCallbacks` owns room create/join. Rooms carry custom properties `pwd` (password), `region` (`PhotonNetwork.CloudRegion`), and `creatorSteamId`; `pwd` and `region` are exposed to the lobby.
- Room name validation: 3–10 chars, `^[a-zA-Z0-9_]+$`. Password (optional) must be ≥ 6 chars. Player limit clamped 2–7.
- `PhotonNetwork.AutomaticallySyncScene = true`.
- Robustness tuning in `GS.Awake` / `OnSceneLoaded`: `Application.runInBackground = true`, `KeepAliveInBackground = 300 s`, `DisconnectTimeout = 45000 ms`.
- `RoomFilterManager` switches Photon regions by disconnecting and calling `ConnectToRegion` (`eu` / `us` / `au`, or `ConnectUsingSettings` for "Best Region"), plus a Steam-friends-only filter that matches the room's `creatorSteamId` against `SteamFriends.GetFriendCount/GetFriendByIndex`.
- `RegionManager` maps region strings to `Europe` / `NorthAmerica` / `Oceania` icons and, for LAN, infers a region from `TimeZoneInfo.Local.Id`.

### Host migration
When a fish becomes the Fisherman in Photon, master-client authority is transferred to them
(`ChangeHostById` → `PhotonNetwork.SetMasterClient`). `GS.isMasterClient` records who the **original**
host was; only that player sees the Play Again / Lobby buttons. If the original host clicks Play Again
while no longer master, `GameManager.isRestoringHost` is set and `RequestHostBack` pulls authority back
before restarting (completed in `OnMasterClientSwitched`).

---

## 5. Script Inventory

96 gameplay scripts, ~28,032 lines. Grouped by responsibility; line counts are exact as of this audit.

### Core singletons / global state
| Script | Lines | Role |
|---|---|---|
| `GS.cs` | 591 | Global state (`DontDestroyOnLoad`). Holds `isLan`, `IsMirrorMasterClient`, `isMasterClient`, nickname, water-mode flags, game mode, round, `playerScores`, `wormCoins`, `scoreSystemSettings`, achievement trackers (`currentRoundWormsUsed`, `hooksEscaped`, `wormsEatenThisRound`). Also owns Steam achievement sync, BG music/SFX volume, screen mode (F11), preloader + tie-preloader lifecycle. Declares `public static class UnityThread { MainThread }`. |
| `GameManager.cs` | 1664 | Per-match orchestrator. Spawning, map/background selection, bucket UI, round end, score bonuses, achievement unlocks, game-state reset, scene reload, Photon callbacks. |
| `DashManager.cs` | 139 | Main-menu hub: Play / LocalPlay / Settings / Credits / Quit / Hints, PlayFab coin display. |
| `BackManager.cs` | 107 | Global ESC/back stack. Self-installs via `[RuntimeInitializeOnLoadMethod]`. |

### Players & gameplay
| Script | Lines | Role |
|---|---|---|
| `FishController.cs` | 909 | Fish movement (Rigidbody2D velocity), hunger death float-to-surface, collisions with worms/hooks/junk/Golden Fish, junk carry & drop (`Q`), win/lose states, per-species speed. |
| `FishController_Mirror.cs` | 816 | Mirror twin. `SyncVar`s for hat name + fish species index, fisherman spawn command, junk pickup/leave, worm spawn, mash-phase relay, game pause. |
| `FishermanController.cs` | 1076 | Boat movement (clamped X), rod selection, cast meter, hook spawn, animator bool/trigger sync, win/lose checks, cricket ambience, water-visibility application. |
| `FishermanController_Mirror.cs` | 258 | Mirror twin. `SyncVar` hat/hair/direction with retry-on-spawn coroutine; server-side hook spawn with `Resources.Load` fallback. |
| `Hook.cs` | 481 | Hook physics, `LineRenderer` fishing line with per-rod/per-viewpoint offsets, worm attach, reel-in, cleanup. |
| `Hook_Mirror.cs` | 79 | Mirror commands/RPCs for rod tip and worm attach. |
| `GoldenFishAI.cs` | 686 | Elaborate flee AI: dynamic shark (player) list refresh every 0.05 s, weighted flee vectors, 16-sector safest-position search with wall/corner penalties, escape locking, 20 s alert memory, 2‑minute fatigue curve down to 40 % speed, smooth-damped direction and push. |
| `MiniGameManager.cs` | 157 | 3-letter typing mini-game with countdown. |
| `MashPhaseManager.cs` | 382 | Spacebar mash tug-of-war, difficulty scaling, LAN/Photon variants. |
| `HungerSystem.cs` | 78 | Hunger bar drain + `AddHunger` (note: adds `hungerBar.value * amount / 100`, i.e. **proportional** to current hunger, not a flat amount). |
| `SmoothTransformSync.cs` | 44 | `IPunObservable` lerped position sync. |

### Spawners & world objects
`WormSpawner.cs` (199) · `WormSpawner_Mirror.cs` (31) · `WormManager.cs` (32) · `WormManager_Mirror.cs` (22) ·
`JunkSpawner.cs` (117) · `JunkSpawner_Mirror.cs` (16, stub) · `JunkManager.cs` (112) · `JunkManager_Mirror.cs` (55) ·
`GoldenFishAI_Mirror.cs` (37) · `EnvironmentScatterManager.cs` (171, deterministic plant scatter seeded by round + mode + group name)

### Networking / lobby
`CustomNetworkManager.cs` (507) · `CoustomeRoomManager.cs` (747) · `LANDiscoveryMenu.cs` (658) ·
`CreateJoinManager.cs` (300) · `HostLobby.cs` (759) · `ClientLobby.cs` (143) · `JoinPanel.cs` (69) ·
`CreatePanel.cs` (34) · `PasswordPopup.cs` (68) · `PhotonLauncher.cs` (79) · `RoomTableManager.cs` (271) ·
`RoomRowPrefab.cs` (113) · `RoomFilterManager.cs` (223) · `PlayerTableManager.cs` (180) · `RegionManager.cs` (203)

### Shop & cosmetics
| Script | Lines | Role |
|---|---|---|
| `ShopManager.cs` | 4592 | **Largest file in the project.** The whole customization screen: fish/fisherman toggle, hat/species/hair dropdowns, composite preview resolution, bottom-right hat-icon placement (a long per-hat `anchoredPosition`/`sizeDelta` table), cell selection outlines, lock overlays, Sal‑T shop open/back/close with full page-state capture and restore. |
| `CosmeticRuntimeApplier.cs` | 2197 | Runtime cosmetic application. PlayerPrefs keys `SelectedFishHatCosmetic`, `SelectedFishermanHatCosmetic`, `SelectedFishermanHairCosmetic`. Resolves animator controllers from `Resources/FishControllers` and `Resources/FishermanControllers`; falls back to modular child-sprite rendering when a "pre-baked" controller asset is missing. Contains a hard-coded `24×4` `HeadCenterYGrid` for per-frame head alignment. |
| `LocalPlayManager.cs` | 969 | Fish species selection UI (Bass index 0 scale 1.0, Trout index 1 scale 3.3), trout unlock gate, arrow cycling, voyage-diagram colouring. |
| `SaltShopUI.cs` | 771 | Runtime-built Sal‑T store front on the root Canvas (deliberately **not** under the scaled background panel). Screen-normalized anchors against 1920×1080, alpha-trimmed icon sizing via `GetOpaqueBounds`, BUY? popup, PlayFab purchase flow. |
| `SaltShopService.cs` | 143 | Deterministic 24 h rotation. Window index = `floor(unixSeconds / intervalSeconds)`; seed = `windowIndex * 31 + rotationSeedSalt`; Fisher–Yates shuffle of the whole in-rotation pool; ships the **entire** shuffled pool so each client can skip what it already owns. |
| `SaltShopClientState.cs` | 107 | Client-side holder. A server payload always wins; a non-authoritative peer shows **nothing** rather than a locally computed shop. |
| `SaltShopPhotonSync.cs` | 96 | Self-installing (`[RuntimeInitializeOnLoadMethod]`) Photon room-property publisher/reader. Key `saltShopState`. |
| `ShopConfig.cs` | 104 | Loader/model for `StreamingAssets/shop_config.json`, cached; `Reload()` drops the cache. |
| `CosmeticUnlocks.cs` | 87 | Ownership: PlayerPrefs `HatUnlocked_<id>` + PlayFab user data `Cosmetic_<id> = "Unlocked"`. Raises `OnUnlocksChanged`. |
| `ShopCosmeticSelector.cs` | 132 | Swaps `BoxSelected` / `BoxUnselected` backing sprites on cosmetic cells. |

### Fisherman animation (modular sprite system)
`FishermanChildAnimatorSync.cs` (406, syncs child animators' state+normalized time to the root and offsets the oar per facing) ·
`FishermanAnimationManager.cs` (364, 24-row × 4-column 64 px grid sprite driver with left/right sub-sprite disambiguation) ·
`FishermanAnimationController.cs` (257) · `FishermanAnimationSystem.cs` (231) · `FishermanAnimationVerifier.cs` (91) ·
`FishermanHatSystem.cs` (153) · `FishermanSpriteLoader.cs` (65) · `FishermanDirectionFlipper.cs` (34)

> **Note:** `CosmeticRuntimeApplier.ApplyToFisherman` / `ApplyFishermanCosmeticsByName` explicitly
> **destroy** `FishermanAnimationSystem`, `FishermanAnimationController`, `FishermanAnimationVerifier`
> and `FishermanHatSystem` at runtime when a `head`/`Head` child exists. Those four scripts are
> superseded by the Animator-controller path and only run on prefabs without a modular head.
> `FishermanDirectionFlipper.Update()` is intentionally empty — flipping is done by animator states.

### Scoring, economy, achievements
`ScoreManager.cs` (422, animated chest/wrapper rise + winner screen + PlayFab coin award) ·
`ScoreSystemSettings.cs` (313, 16 tunable values, serialized as strings, synced via Photon room props or Mirror message) ·
`ScoreUI.cs` (50) · `CoinManager.cs` (37) · `PlayFabManager.cs` (291) ·
`AchivementsManager.cs` (32) · `AchievementCellManager.cs` (133) · `AchievementTestUI.cs` (41) ·
`DashAchievementUI.cs` (74) · `ResetStatsAchievements.cs` (22) · `SteamIntegration.cs` (83) ·
`Steamworks.NET/SteamManager.cs` (182, stock Steamworks.NET v1.0.13)

### UI / menus / utilities
`GameOver.cs` (394) · `SettingsMenu.cs` (256) · `PauseManager.cs` (33) · `InGameMenu.cs` (76) ·
`SplashManager.cs` (77, DOTween logo slide → loads `Dash`; also creates `PlayFabManager` and logs in) ·
`Preloader.cs` (52) · `DropdownHandler.cs` (153) · `GameModeDropdownHandler.cs` (95) ·
`HintsManager.cs` (183, DOTween loops) · `HowToPlayManager.cs` (20) · `QuitManager.cs` (34) ·
`CraditsManager.cs` (22) · `CreaditsManager.cs` (25) · `ControlesManager.cs` (118) ·
`FishControlManager.cs` (32) · `FishermanControlManager.cs` (43) · `ButPanelManager.cs` (6, empty `BuyPanelManager`) ·
`GameScaler.cs` (49) · `LegacyTextSharpener.cs` (279, overlays TMP text on legacy `Text` for crispness) ·
`AutoBlockRaycastOnInputClick.cs` (308) · `RaycastBlockerFinder.cs` (132) · `HoverTooltipManager.cs` (115) ·
`AnimatedBackground.cs` (91) · `UIImageFrameAnimator.cs` (120) · `DestroyAfterAnim.cs` (8) ·
`SceneObjectImageSaver.cs` (220)

### Editor
`Assets/Editor/FixUIEditor.cs` — `[InitializeOnLoad]`; on first load (guarded by `EditorPrefs` key
`FixResetButtonPosDone3`) it opens the `Dash` scene and repositions any Button whose name contains
"reset" under a "score" parent to scale 4 / anchored `(-190, -105)`.

---

## 6. The Sal‑T Shop (server-authoritative)

**Data:** `Assets/StreamingAssets/shop_config.json`

```
configVersion 1 · currency "WC" · rotationSlots 3 · rotationIntervalHours 24 · rotationSeedSalt 7741
```

13 hats defined. Prices are **200 / 500 / 1000** WC.

| id | Display name | Category | Price | In rotation | Unlocked by default |
|---|---|---|---|---|---|
| `FisherMan_Hat_-Blue_Cap` | Blue Cap | fisherman_hat | 200 | ✔ | ✘ |
| `FisherMan_Hat_-Red_Cap` | Red Cap | fisherman_hat | 200 | ✔ | ✘ |
| `FisherMan_Hat_-Chef_Hat` | Chef Hat | fisherman_hat | 500 | ✔ | ✘ |
| `FisherMan_Hat_-Ranger_Hat` | Ranger Hat | fisherman_hat | 500 | ✔ | ✘ |
| `FisherMan_Hat_-Soda_Hat` | Soda Hat | fisherman_hat | 1000 | ✔ | ✘ |
| `FisherMan_Hat_-Fish_Hat` | Fish Hat | fisherman_hat | 1000 | ✔ | ✘ |
| `TurtleHat` | Turtle Hat | fisherman_hat | 1000 | ✔ | ✘ |
| `FisherMan_Hat_-Default_-_Fishing_Hat` | Fishing Hat | fisherman_hat | 0 | ✘ | **✔** |
| `paper_boat` | Paper Boat Hat | fish_hat | 200 | ✔ | ✘ |
| `cap` | Cap | fish_hat | 200 | ✔ | ✘ |
| `hat` | Orange Hat | fish_hat | 500 | ✔ | ✘ |
| `hat2` | Top Hat | fish_hat | 500 | ✔ | ✘ |
| `beret` | Beret | fish_hat | 1000 | ✔ | ✘ |

**Authority flow**
1. Authority (Mirror host / Photon master / offline) calls `SaltShopService.ResolveCurrentShop()`.
2. Mirror: pushed per-connection as `SaltShopStateMessage`. Photon: published as room property `saltShopState` and re-published on master-client switch.
3. Clients render `SaltShopClientState.GetCurrent()` verbatim. A client joined to a session that hasn't sent a payload yet shows `"..."` — never a locally invented shop.
4. The shelf shows the first `visibleSlots` (3) entries the **local** player has not unlocked; if all are owned it shows `"SOLD OUT"`.
5. Purchase: `PlayFabManager.GetCurrency` → balance check → `SubtractCurrency` → `CosmeticUnlocks.Unlock(id)` → `OnUnlocksChanged` refreshes the shelf and the customization-screen padlocks.
6. Failure strings surfaced in the popup: `NOT CONNECTED`, `NOT ENOUGH COINS`, `PURCHASE FAILED`.

**Art:** `Assets/Resources/ShopUI/SaltShop/` — `salt_shop_sign.png`, `back_sign.png`, `close_sign.png`, `coin.png`, `lock.png`, `picture_frame_1.png`, `picture_frame_2.png`. The animated shop background comes from `Assets/Resources/Fishingshop2Frames/Fishingshop2_00..05.png` (6 frames, 10 fps).

**Known open asset requests** — see `ASSET_REQUEST.md` at repo root:
1. Shop background is 768×384 (2:1); needs a 1920×1080 16:9 version, same 6 frames / filenames.
2. `FisherMan_Hat_*` icons are 64×64 with art filling only 20–38 %; `cap`/`beret`/`hat`/`hat2`/`paper_boat` are tightly cropped. Currently worked around in code by `SaltShopUI.GetOpaqueBounds` alpha-trimming (which requires **Read/Write Enabled** on the icon importers — it logs a warning and renders undersized otherwise).
3. Open question about dropping the "W.I.P" label under the skull tab.

---

## 7. Cosmetics Pipeline

### Fish
- Two species: **Bass** (`Resources/Fish.prefab`, index 0, scale 1.0) and **Trout** (`Resources/Fish 2.prefab`, index 1, scale 3.3).
- Trout is gated behind `PlayerPrefs["FishUnlocked_Trout"]`, which is auto-granted once **all three** of `GULPER`, `WHAT_A_SNACK`, `SOLO_ARTIST` are unlocked (`LocalPlayManager.AreAllTroutAchievementsUnlocked`).
- Fish hats are applied as animator-controller swaps from `Resources/FishControllers/` (`bass_*_0.controller` / `trout_*_0.controller`, plus `Fish 1 Default` / `Fish 2 Default`), with a child sprite `Applied Fish Hat Cosmetic` as the generic path.
- Shop previews are **composite PNGs** in `Resources/ShopUI/Fish preview/` (`bass.png`, `trout.png`, `Fish Cap Hat.png`, `Trout orange hat.png`, …). Plain `bass`/`trout` are force-cached as the species base so an un-hatted fish never renders a hat.

### Fisherman
- Three shipping animator controllers in `Resources/FishermanControllers/`: `FisherMan (Black Hair)`, `FisherMan (Red Hair)`, `FisherMan Yellow Hat`.
- `IsHatPreBaked()` returns true for many more names (backwards, blue, frog/griin, green, headphones, silver, straw, white, yellow/default). When a "pre-baked" controller asset is **missing**, the code falls back to **modular** rendering: clean hair controller on the root plus a `head/hat Cosmetic` child SpriteRenderer at local position `(-0.029, 0.075, -0.9)`, scale `0.73484`, sorting order `root + 10`.
- Hair is red or black; selecting a hair clears the hat selection and vice versa (`SelectFishermanHat` nulls hair, `SelectFishermanHair` nulls hat).
- Preview PNGs live in `Resources/ShopUI/Fisherman Preview/` and `…/Black Hair Hats Preview/`.

### Networking of cosmetics
- **Photon:** instantiation data carries `[hatName, speciesIndex]` for fish and `[hatName, hairName]` for the fisherman; `IPunInstantiateMagicCallback.OnPhotonInstantiate` applies them, with `RpcSetFishHat` / `RpcSetFishermanCosmetics` (`AllBuffered`) as the durable path.
- **Mirror:** `SyncVar`s with hooks, plus `ApplySyncedCosmeticsWhenReady` coroutines (3 s retry window) because the initial SyncVar payload can land a frame after `OnStartClient`.
- Per-player correctness is explicitly enforced: LAN spawning uses the registered base Fish prefab for every connection, and each owning client pushes its own species/hat — the host's choices are never applied to other players.

---

## 8. Score System (host-configurable)

`ScoreSystemSettings` — 16 values, stored as strings so blank input fields are tolerated, parsed with
clamps, and defaulted via `FillBlankValuesWithDefaults()`.

| Setting | Default | Clamp |
|---|---|---|
| Fisherman win points | 15 | 0–999 |
| Fisherman catch-fish points | 3 | 0–999 |
| Fisherman bucket-worm points | 1 | 0–999 |
| Fish win points | 10 | 0–999 |
| Fish eat-worm points | 1 | 0–999 |
| Fish survive points | 5 | 0–999 |
| Golden fish bonus points | 0 | 0–999 |
| Spacebar jam min | 30 | 1–100 |
| Spacebar jam max | 70 | 1–100 |
| Fish timer (s) | 3 | 0.5–60 |
| Hunger worm rate | 15 | 0–100 |
| Hunger depletion rate | 1 | 0–100 |
| Bass speed | 3 | 0.1–100 |
| Golden fish speed | 3 | 0.1–100 |
| Trout speed | 3 | 0.1–100 |
| Worm spawn rate | 5 | 0.25–60 |

- Edited in the host lobby's Score System panel (`HostLobby`), which locates TMP input fields **by GameObject name** and, where a field doesn't exist in the scene, **creates it at runtime** by cloning a template (`Fish Timer`, `Depletion Hunger Rate`, `Trout Speed`, `Bass Speed`) along with a generated "Reset" button.
- Only the host can edit (`CanEditScoreSystemSettings`). Changes broadcast immediately: Photon room custom properties (`ss_*` keys) or a Mirror `ScoreSystemConfigMessage`.
- Worm spawn interval is derived: `clamp(25.0 / wormSpawnRate, 0.25, 60)`; concurrent worm cap = `(int)wormSpawnRate`.

---

## 9. Achievements & Economy

**Seven achievements**, tracked in `PlayerPrefs["Achievement_<ID>"]` and mirrored to both Steam and PlayFab:

| ID | Unlock condition (from code) |
|---|---|
| `SOLO_ARTIST` | Quick Cast (mode 1), you survive and are the **only** fish alive |
| `SURVIVOR` | Deep Sea Fishing (mode 2), you escaped ≥ 15 hooks and survived |
| `EARTH_PRAISER` | Full lobby (≥ 6 players), Fisherman wins using ≤ 6 worms |
| `WHAT_A_SNACK` | Eat the Golden Fish |
| `FISH_SLAYER` | Full lobby, Fisherman catches ≥ 6 fish |
| `WE_COME_IN_SWARMS` | Full lobby, fishes win with **all** original fish still alive |
| `GULPER` | Eat ≥ 30 worms in one round |

Sync paths:
- **Steam** — `GS.UnlockAchievementAndSyncToSteam` does `SetAchievement` + `StoreStats` immediately; `UserStatsStored_t` / `UserStatsReceived_t` callbacks log the result. `GS.Start` also runs a delayed full re-sync, and `GameManager.Awake` prints a verbose achievement debug block and re-pushes every locally-unlocked achievement.
- **PlayFab** — `PlayFabManager.SyncLocalAchievements()` on login writes `Achievement_<ID> = "1"` into user data.

**Currency:** PlayFab virtual currency code **`WC`** ("Worm Coins"). Awarded on the winner screen equal to the winner's score (`ScoreManager.SaveWormCoinsToPlayFab`, guarded by a per-round `hasSavedCoinsThisRound` flag). Spent in the Sal‑T shop.

**PlayFab login:** `LoginWithCustomID` using `SystemInfo.deviceUniqueIdentifier`, suffixed `_EDITOR` under `UNITY_EDITOR` so an Editor instance and a standalone build on the same PC are different accounts.

---

## 10. Controls

| Action | Input |
|---|---|
| Fish move | `Horizontal` / `Vertical` axes (WASD / arrows) |
| Fish drop carried junk | `Q` |
| Fisherman move boat | `Horizontal` axis (clamped to `minX`/`maxX`) |
| Fisherman select rod | `W` (left rod) / `S` (right rod) |
| Fisherman cast | Hold `X` **and** `V` to charge; release either to cast |
| Reel hook in | Right mouse button |
| Mini-game | Type the shown 3-letter sequence |
| Mash phase | `SPACE` repeatedly |
| Back / close menu | `ESC` (via `BackManager` stack) |
| Toggle fullscreen | `F11` |
| Debug: log Photon region | `F9` (`RoomFilterManager`) |

Fullscreen toggle uses `ExclusiveFullScreen` at the current resolution, windowed uses 1280×720.

### Input System status (spec §5)

The project runs with **Active Input Handling = Both**, and input currently exists on two paths:

1. **Legacy (authoritative today).** 15 `Input.GetAxis` / `GetKeyDown` / `GetMouseButtonDown` call
   sites, plus direct `Keyboard.current` reads in `FishermanController`, `MashPhaseManager` and
   `MiniGameManager`. This is what actually drives the game.
2. **New (built, live, not yet authoritative).** `Scripts/Data/PlayerControls.inputactions` now
   describes the real control scheme in two maps — `Gameplay` (`Move`, `Cast`, `Reel`, `DropJunk`,
   `Mash`) and `Global` (`Back`, `ToggleFullscreen`, `ToggleRoomFilter`). `Generate C# Class` is on,
   producing `PlayerControls.cs` in `PanicAtThePond.Data`. `InputManager` wraps it and publishes
   typed C# events; it lives at the root of the Splash scene and persists via `DontDestroyOnLoad`.

`Cast` is a `OneModifier` composite over `X`+`V`, which matches the code exactly: `performed` when
both are held, `canceled` when either is released. Rod selection needs no separate action — it is the
`Move` Y axis.

**To complete the migration:** playtest with two live clients, then set
`InputManager._isAuthoritative = true` and delete the legacy reads. The two paths were kept in
parallel deliberately, because multiplayer match flow cannot be verified without two clients and a
silent input regression would only appear in a real match.

> The existing `Player` and `UI` maps in the asset were left byte-identical on purpose:
> `FishermanController.cs:408` holds a live `InputActionReference` into the `Gameplay/Move` action,
> and rewriting the asset would have nulled it silently.

---

## 11. Asset Inventory

**`Assets/Resources/`** (runtime-loadable)
- Prefabs: `Boot`, `Drop`, `Fish`, `Fish 2`, `FisherMan`, `FisherMan (2) 1`, `Golden Fish`, `HookWorm`, `hookPrefab`, `Tire`, `Worm`
- `FishControllers/` — 15 controllers + `Clips/`
- `FishermanControllers/` — 3 controllers
- `FishermanSprites/` — Arms, Boat, GreenBody, Oars, Rods sheets
- `ShopUI/` — hat icons, `Fish preview/`, `Fisherman Preview/`, `SaltShop/`, `BoxSelected/BoxUnselected`, `Red_Hair`/`Black_Hair`, `daigram preview`, `sal-t shop icon`, `Fishingshop1.gif`, `Fishingshop2.gif`
- `Fishingshop2Frames/` — 6 extracted GIF frames
- `Fisherman created/`, `rope.mat`, `DOTweenSettings.asset`

**`Assets/Prefebs/`** (note the spelling) — `Create And Join Panel`, `GameModeDropDown`, `How to Play`, `InGameMenu`, `PasswordPopup`, `PreloderUI`, `TiePreloderUI`, `Quit`, `Settings`, `buble1_0`, `buble2_0`, `playerRowPrefab`, `playerRowPrefab 1`, `roomRowPrefab`

**`Assets/Animations/`** — Bubble, DropDown, Fish, Fisher Man, Golden Fish (×2), Hats, PatchOfKelp, Plant2, Plant4, WaterAnimation, Waterlight, Worm

**`Assets/UI/`** — `Acheivements`, `Dash UI`, `Game UI`, `ShopUI`, `UI`

**`Assets/Panic at the Pond SFX/`** — `Fisherman`, `LakeAmbience`, `Water SFX`, `enviroment`, `Used Sounds`, plus a bubble WAV

---

## 11b. Scene Hierarchy (spec §2)

All three scenes carry the six mandated root separators:

```
--- MANAGERS ---
--- SYSTEMS ---      Main Camera, EventSystem
--- UI ---           the scene's root Canvas
--- ENVIRONMENT ---
--- GAMEPLAY ---
--- DEBUG ---
```

| Scene | MANAGERS | ENVIRONMENT | GAMEPLAY | DEBUG | pinned to root |
|---|---|---|---|---|---|
| Splash | — | — | — | — | `GS`, `InputManager` |
| Dash | Steam Integration, LAN discovery | — | — | RoomFilterManager | `RegionManager`, `Reporter` |
| Play | Managers (GameManager, InGameMenu, ScoreManager), BackManager | Environment, WaterObject, clouds ×2, BG ×2 | WormSpawner, JunkSpawner, JunkGeneratePoint | — | — |

> ### ⚠ `DontDestroyOnLoad` objects must stay at scene root
>
> Unity's `DontDestroyOnLoad` **only works on root GameObjects**. Any object whose script calls it
> cannot be placed under a `--- SECTION ---` separator — doing so either kills the object on scene
> change or drags the whole separator (and everything under it) across scenes.
>
> Currently root-pinned for this reason: `GS`, `InputManager` (Splash), `RegionManager`, `Reporter`
> (Dash). Scripts that call it: `GS`, `InputManager`, `AudioManager`, `UIManager`, `PoolManager`,
> `PlayFabManager`, `RegionManager`, `SaltShopPhotonSync`, and third-party `Reporter`.
>
> **This was learned the hard way:** placing `Reporter` under `--- DEBUG ---` produced four
> `NullReferenceException`s per frame from `Reporter.Update()`. The proper fix is a
> `Persistent.unity` scene (spec §13), which this project does not yet have.
>
> `CustomNetworkManager` under `GS` is a safe exception — Mirror force-parents itself to root before
> its own DDOL call (`NetworkManager.cs:696`).

Because `Reporter` must stay at root, `--- DEBUG ---` does not currently hold everything
development-only, so the spec's "strip DEBUG in release builds" step is still outstanding.

---

## 12. Repo Hygiene & Tooling Notes

- **Working tree is dirty** as of this audit: modified `Dash.unity`, `FishController_Mirror.cs`, `SaltShopService.cs`, `SaltShopUI.cs`, `ShopManager.cs`, several `ShopUI/*.png.meta` files, `Packages/manifest.json`, `packages-lock.json`, `ProjectSettings.asset`, the NuGet DLLs, and `build_info`. `ASSET_REQUEST.md` is untracked.
- **~130 `.csproj` files and 8 `.slnx`/`.sln` files are committed at the repo root** alongside Python helper scripts (`generate_all_fisherman_hats.py`, `fix_all_layering.py`, `find_head_offsets.py`, `scratch.py`, …), reference PNGs (`ref_debug.png`, `ref_idle.png`, `ref_move.png`, `ref_rows.png`) and a 476 KB `temp_log.txt`. These are build/scratch artifacts, not sources.
- **Commit messages are largely non-descriptive** ("PATP", "Everything Uptodate", "completed bug"), so git history is not a reliable changelog. That is precisely why this document exists.
- `.mcp.json` registers an HTTP MCP server `ai-game-developer` at `https://ai-game.dev/mcp` **with a bearer token committed in plain text**. Treat that token as exposed.
- `.claude/skills/` contains ~90 Unity-MCP skill definitions (asset/gameobject/animator/scene/script/profiler tools) generated by the `com.ivanmurzak.unity.mcp` package.
- Two `MonoBehaviour`s named `CraditsManager` and `CreaditsManager` both exist and do the same thing. `ButPanelManager.cs` declares an empty class named `BuyPanelManager` (filename ≠ class name).
- Debug logging is very heavy across the networking and cosmetic code paths (emoji-prefixed `Debug.Log` on nearly every RPC), and several log strings are in Hindi/Hinglish.

---

## 13. Maintenance Protocol

Whenever this project changes:

1. **Code change** → update the relevant table in §5 (line count + role) and any behaviour described in §3/§4/§6/§7/§8.
2. **New/removed script** → add or remove its row in §5 and refresh the total in the header.
3. **`shop_config.json` edit** → update the hat table in §6.
4. **`ScoreSystemSettings` default/clamp change** → update the table in §8.
5. **New achievement** → update §9 (and check `GS.SteamAchievementIds`, `PlayFabManager.SyncLocalAchievements`, and the `GameManager.Awake` debug array — the ID list is duplicated in all three).
6. **Package / Unity version / project setting change** → update §1 and §2.
7. **New scene, prefab, or Resources folder** → update §1 and §11.
8. Bump **Last full audit** in the header when a fresh end-to-end pass is done; note incremental edits by date under the relevant section instead.

### Change log
| Date | Change |
|---|---|
| 2026-07-29 | Initial full-project audit and creation of this document. |
| 2026-07-29 | **Safe-tier restructure to `Assets/_Project/`** (§0). All 97 own scripts sorted into Managers/Controllers/Gameplay/UI/Shop/Data/Utilities/Editor; Mirror, PlayFab, Steamworks (incl. `SteamManager.cs`), Unity-Logs-Viewer and Plugins moved to `Assets/ThirdParty/`; Resources, Scenes, Art, Audio, Prefabs moved under `_Project`. Photon, TextMesh Pro and StreamingAssets deliberately left in place with reasons recorded. Verified 0/97 script GUIDs changed. Build-settings scene paths updated. `project_overview.md` relocated to `Assets/_Project/`. |
| 2026-07-29 | Tooling: installed Node.js 24.18.0 LTS (enables `unity-mcp-cli`); switched Unity MCP plugin `connectionMode` from `Cloud` (which had a null `cloudToken` and so never connected) to `Local`. |
| 2026-07-29 | **Namespaces**: all 96 own scripts wrapped in `PanicAtThePond.<Module>` with blanket cross-module `using`s. Verified — `Assembly-CSharp.dll` + `Assembly-CSharp-Editor.dll` rebuilt 09:39:29, 0 compile errors, 0 missing script references. |
| 2026-07-29 | **Infrastructure Managers added** (additive, nothing wired to them yet): `IPoolable`, `PoolManager`, `AudioManager`, `UIManager`, `InputManager`. Verified — assemblies rebuilt 09:48:00, 0 errors, 0 warnings. |
| 2026-07-29 | **Field encapsulation**: 153 public fields → `[SerializeField] private` (name unchanged, so all Inspector wiring preserved). 161 left public pending property accessors; 154 skipped as static/const/SyncVar; 11 DTO fields reverted after `CS0122`. Verified — assemblies rebuilt 10:06:53, 0 errors, 0 missing references. |
| 2026-07-29 | **Playmode functional test (Splash → Dash).** Verified live: all manager singletons alive; PlayFab `IsLoggedIn=true`; **every `Resources.Load` path resolves after the folder move** (10/10 prefabs, 5/5 animator controllers, 11/11 named sprites, 159 `ShopUI` sprites, 6 GIF frames); `StreamingAssets` path correct; `ShopConfig` loads 13 hats / 3 slots / currency `WC`; `SaltShopService` resolves a 12-item rotation; all 5 `DashManager` button handlers execute without exception and activate their panels. Game view renders the full Dash UI and the Create/Join panel correctly. |
| 2026-07-29 | **Bug found & fixed:** blank white shop screen. `ShopManager.ResizeToSpriteAspect` divided canvas-space width by *world-space* `lossyScale`, which only works on a ScreenSpaceOverlay canvas; this project uses ScreenSpaceCamera, so the Sal-T sign inflated to 22809×8236 and covered the screen. Normalised the divisor against the canvas scale. Pre-existing (method byte-identical to backup; `Dash.unity` SHA256-identical). Verified: sign now 211×76, full shop UI renders. |
| 2026-07-29 | **Bug found & fixed:** `BackManager.instance` was null for the whole Dash screen, making "Play as first click" throw `NullReferenceException`. Pre-existing (only the namespace wrapper had been added to that file). Fixed by re-running `EnsureInstance()` on `SceneManager.sceneLoaded`. Re-verified end-to-end. |
| 2026-07-29 | **Runtime verification via the Unity MCP bridge** (`unity-mcp-cli --url http://localhost:29620`). `Dash.unity` opens clean (8 root objects, valid, buildIndex 1); `Play.unity` opens clean (14 root objects, valid, buildIndex 2). Zero missing script references. Console stack traces confirm the migration end-to-end: vendor code runs from `Assets/ThirdParty/com.rlabrecque.steamworks.net/SteamManager.cs` and game code from `PanicAtThePond.Managers.SteamIntegration` at `Assets/_Project/Scripts/Managers/SteamIntegration.cs`. Only remaining console errors are environmental — see "Known environmental errors" below. |
| 2026-07-29 | **Session 2 — spec v3.0 pass.** Deprecated-API sweep, scene partitioning, assembly definitions, Input System actions + `InputManager`, scene root hierarchy, audio routing. See §14 and `restructure_report.md`. |
| 2026-07-29 | **Shop cosmetic slot sizes normalised.** 16 of 66 slots under `Fish Cosmetic/Elements` and `Fisherman Cosmetic/Elements` (Dash) had drifted to hand-dragged sizes ranging 48.80×45.82 – 50.00×46.48. All slots set to the canonical **50 × 47** already used by the other 50. Nav buttons (`CloseButton`, `next Button`, `PreviousButton` @ 36.17×34.76) deliberately untouched. Verified safe first: no LayoutGroup/Fitter drives these, and nothing resizes them at runtime (`ResizeToSpriteAspect` applies only to the Sal-T sign; `ShopCosmeticSelector` only swaps sprites). Pivots are centred, so positions did not shift. |
| 2026-07-29 | **Locked-hat skull padlock size made uniform.** `ShopManager.UpdateCosmeticLockOverlays` sized the "Locked Hat Skull" overlay from its parent cosmetic cell (`Mathf.Max(24f, cellRect.rect.height * 0.7f)`). Those cells carry hand-authored, **non-uniform** scales (~0.47×–2.5×, x ≠ y), so skulls rendered anywhere from ~56 to ~172 canvas units tall and were stretched differently in each cell — and differently between the Fish and Fisherman panels. New `ApplyLockedSkullSize` divides out the cell's scale per-axis against the root canvas, so every skull renders at a constant `lockedSkullCanvasHeight` (serialized, default **114** canvas units ≈ 70% of the 163-unit slot box). Size is now re-applied on every refresh rather than only at creation, so already-spawned overlays self-correct. **Verified in playmode: all 10 skulls across both panels render 75.32 × 114.00, spread 0.000, aspect 0.661 matching lock.png (185/280).** |
| 2026-07-29 | **Locked-hat skull rotation fixed.** `cosmeteic Orange hat` carried `localEulerAngles.z = -32.58` (every other cell was 0.00; `cosmeteic polish hat` had a stray -0.27). The skull is a child of the cell, so it inherited the tilt. Two-part fix: (1) `ApplyLockedSkullSize` now sets the overlay's **world** rotation to the root canvas's, so a skull can never inherit a cell's tilt; (2) the two anomalous cell rotations were zeroed in `Dash.unity`. Part (2) was necessary, not cosmetic — counter-rotating a child inside a **non-uniformly scaled** parent (this cell is 0.4931 × 0.6363) always shears it: with rotation alone the Orange skull measured 82.82 × 105.02 at aspect 0.789 instead of 75.32 × 114.00 at 0.661. Zeroing the rotation removes the shear source. **Side effect: the orange hat sprite now renders upright like the other 15.** Reversible — set Z back to -32.58 on that cell if the tilt was intentional. **Verified: all 10 skulls 75.32 × 114.00, aspect 0.661, rotation deviation 0.0000°, spread 0.0000.** |
| 2026-07-31 | **Responsive UI pass.** (a) **702 fixed-position rects re-anchored** across all 3 scenes (Splash 24, Dash 545, Play 133) from point anchors to *proportional* anchors — position now scales with the parent while size stays fixed, so widgets reposition without distorting. Proven non-destructive by capturing every world corner before and after: **max drift 0.000136 world units** (~1/100 px). Elements already stretching (314) and layout-group-driven (130) were correctly skipped. (b) New `SafeAreaFitter` (notch/system-bar insets) and `ResponsiveCanvasController` (drives `matchWidthOrHeight` from the live aspect ratio; `ExpandToFit` guarantees UI is never cropped) added to all 4 root canvases. The controller deliberately does **not** rewrite the authored reference resolution — content is laid out in that space, so changing Splash/Play from 800×600 would rescale everything by ~2.4×. (c) **Verified at 1920×1080, 2560×1080, 2340×1080, 1080×1920, 1170×2532, 1620×2160**: zero content elements off-screen at every aspect. The only overflowing rects are full-bleed backgrounds (e.g. `Canvas/Shop` is 1920×1084 on a 1080-tall screen) and `Shop/Fish 1`, which already overflowed 12px at the reference resolution — pre-existing, not caused by re-anchoring. |
| 2026-07-31 | **Cosmetic/UI asset catalog added — this is what makes art swappable.** Cosmetics were bound by fuzzy matching on sprite filenames (`hatName.Contains("blue_cap")`) plus 45 literal `Resources.Load` paths, and the *same strings* are written to `PlayerPrefs` (`CosmeticRuntimeApplier:2040` saves `sprite.name`), sent over the network (`syncedHatName` Mirror SyncVar) and stored in server-authoritative `shop_config.json`. Renaming art therefore broke saves, cross-client sync and shop config at once. New `CosmeticCatalog` ScriptableObject binds a **stable ID** to explicit `Sprite` / `RuntimeAnimatorController` references, so the ID stays fixed while the artwork is free to change. `CosmeticRuntimeApplier.GetSpriteByName` and the animator resolution now consult the catalog first and **fall back to the old name matching**, so an incomplete catalog degrades instead of losing a cosmetic. `Tools ▸ Panic At The Pond ▸ Rebuild Cosmetic Catalog` auto-populates it (**177 entries**) and preserves manual overrides; sprite-sheet frame suffixes (`TurtleHat_0`) are auto-aliased to their un-suffixed form. **Verified: 13/13 shop_config IDs resolve from the catalog; 3/6 animators, which is correct — only Yellow Hat, Black Hair and Red Hair controllers ship, the rest intentionally fall back to modular rendering.** |
| 2026-07-31 | **Canvas rework + SafeArea wiring.** Canvases renamed for consistency and given **explicit sorting orders** so draw order is declared rather than inherited from hierarchy accident: `splash` → `Canvas_Splash` (0), `Canvas` → `Canvas_Dash` (0), Play's `Canvas` → `Canvas_Play` (10, Overlay), Play's `BG` → `Canvas_PlayBackground` (−100, ScreenSpaceCamera). Verified safe first — **zero code references any canvas by name**. A `SafeAreaRoot` (with `SafeAreaFitter`) was inserted into every root canvas and all **interactive** panels moved inside it (Splash 1, Dash 16, Play 9); pure backdrops stay outside so notches inset the UI without leaving gaps at the screen edge. Selected by presence of a `Selectable` in the subtree, not by size — an earlier size-only rule wrongly classed full-screen *panels* as backdrops. Non-destructive: **max world drift 0.000305**. Verified in playmode: `SafeAreaRoot` = 1920×1080 with 19 children on desktop (where `Screen.safeArea` is the full screen), shop screens 5/5 OK, clean Dash renders correctly, console clean apart from the two known Steam errors. |
| 2026-07-31 | **GameObject names decoupled (step 1 of the rename).** Auditing what a rename would break turned up **20+ hard-coded GameObject names** spread across a dozen files — `transform.Find("head"/"Head"/"chest"/"oar"/"hat Cosmetic")` (fisherman rig), `name.Contains("Golden Fish")` (collision), `go.name == "clouds_1_5"/"clouds_1_0"` (parallax), `name.Contains("Name"/"Score")` (score table), `label.name == "Depletion"`, and `ShopManager.FindGameObjectByNames(..., "Shop", "Fish 1", "Fish 2", "Fish Cosmetic", "hat", "hair")`. None of these produce a compile error when the scene object is renamed — they fail silently at runtime. All literals are now replaced by constants in the new `PanicAtThePond.Data.SceneObjectNames`, so a rename is a one-line change there plus the scene edit, and every dependent site moves with it. `ShopManager.LockedHatSkullName` now aliases the same constant. **Verified: compiles clean; shop screens 3/3; all 10 locked-hat skulls still resolve; catalog 177 entries; no new console errors.** The renames themselves (`SP_`/`M_`/`ANIM_`/`Panel_`/`Btn_`/`TXT_`) are the next step and are now safe to perform. |
| 2026-07-31 | **Shop previews rebuilt as layered renders (`CosmeticPreviewRig` + `LayeredCosmeticPreview`).** The previews were one hand-painted image per combination — 18 fisherman (2 hair × 8 hats) + 14 fish (2 species × 6 hats) — so a new hat needed new art per hair colour and species, and because each was painted separately the character came out a different size in each (measured drawn content 251×228 → 293×253, a **17% spread**). The preview now renders the **real prefab** off-screen to a RenderTexture with cosmetics applied by `CosmeticRuntimeApplier`, the same code the in-game character uses. Framing is captured from the pristine prefab **before** cosmetics, so a tall hat extends past the frame instead of shrinking the character. **A new hat now needs no preview art at all.** Four defects found and fixed during the build, each caught by measurement: (1) `Destroy` is deferred, so the previous subject was still present at render time and two characters appeared at once — now `DestroyImmediate`; (2) the flat `FisherMan` prefab has no `head` child so hats landed on the face — the modular `FisherMan (2) 1` prefab is used instead; (3) fish species is part of the *base*, so it must be applied before framing is captured — otherwise trout filled the whole texture; (4) the default fishing hat takes a **pre-baked** path that disables every modular renderer and relies on an animator, rendering blank in a still preview — `ForceModularRendering` now guarantees the modular form for every hat. **Verified across all 32 combinations: 0 empty renders; fisherman constant at width 305 / baseline 103 for all 18; bass constant 305/158; trout 314–318/160. Height varies only with hat height, which is correct.** The pixel-diff placement approach tried first was **disproved and removed** — base and composite share 0.0% identical pixels, so the composites are independent drawings, not "base + hat". |
| 2026-07-31 | **Asset renaming — 340 assets renamed (spec §9).** New `Tools ▸ Panic At The Pond ▸ Rename Assets` menu items use `AssetDatabase.RenameAsset`, which preserves GUIDs so scene/prefab/catalog references follow automatically. **Safe batch: 310** — 27 audio → `SFX_`, 283 animation clips → `AC_`, 1 material → `M_` (verified zero string-literal dependencies first). **Animator controllers: 26** → `ANIM_`, with the 4 live `Resources.Load` literals rewritten in the same pass; 4 pre-existing `AC_`-prefixed controllers were de-double-prefixed. One benign failure: a second `LeftPoleToOar.anim` collided after sanitising — it is referenced by nothing (the referenced one renamed fine), so it became `AC_LeftPoleToOar_Unused`. **Deliberately excluded, with reasons:** (a) **Prefabs** — `PhotonNetwork.Instantiate(prefab.name, …)` resolves prefabs by name across clients, so renaming is a networking-visible change unverifiable without two clients; they are already PascalCase. (b) **`Resources/FishControllers/*`** — named to match hat *sprite* names and resolved dynamically as `Resources.Load("FishControllers/" + sprite.name)`; prefixing breaks that coupling. (c) The 8 `FishermanControllers/FisherMan (…)` literals for controllers that **do not exist** were left as-is — they already returned null and fell through to modular rendering by design. **Verified: 0 compile errors; 42 controllers with 410 clip references and 0 nulls; all 4 renamed controllers load under new names; FishControllers still load unchanged; shop 3/3; 10 skulls; catalog 177; `Fish` prefab + 159 ShopUI sprites intact; only the 2 known Steam errors.** |
| 2026-07-31 | **Two regressions found and fixed after user report of wrong visuals / missing sprites.** (1) **`ResponsiveCanvasController` was silently resizing Splash and Play.** Its `ExpandToFit` default overrode `matchWidthOrHeight`, and where the reference resolution does not match the display that *changes the scale factor*: on an 800×600 reference at 1920×1080 it scales **1.80 instead of the authored 2.078 — 87% of authored size**. Dash was unaffected because its reference already equals 1920×1080, which is why every screenshot taken during development looked correct and the regression went unnoticed. Default is now `Authored` (keeps the scene's own match, zero visual change) with `ExpandToFit` opt-in per canvas and its size effect documented in the tooltip; all 4 live controllers were set to `Authored`, restoring match 0.50. (2) **Catalog normalisation merged distinct sprites** — stripping spaces/underscores collapsed `black hair` and `Black_Hair` to one key, so one resolved to the other's art. `TryGetEntry` now tries the exact key before the normalised one, keeping loose matching without merging real assets. **Also reverted:** the `SafeAreaRoot` wrapper was removed from all canvases and baseline sibling order restored — its reparenting reordered children (`ShopManagement` moved from last to 3rd), and safe area is a no-op on this PC/Steam target anyway. **Verified: sprite resolution 149 checked / 0 mismatched; canvas match back to authored 0.50; buttons behave identically to a git-baseline scene tested side by side (both 6/36 reachable when the shop is opened programmatically — a test artifact, not a defect); only the 2 known Steam errors.** |

### Bug found and fixed during runtime testing — `BackManager` null on first scene

**Symptom.** Clicking **Play** as the very first action on the Dash screen threw a
`NullReferenceException`. Reproduced deterministically, then fixed and re-verified.

**Root cause.** `BackManager.InitializeOnSceneLoad` is a
`[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`, which fires **once per play session**. It created a
`BackManager` GameObject inside whatever scene was active at the time (Splash). That object is
deliberately *not* `DontDestroyOnLoad`, so it was destroyed the moment Splash unloaded — leaving the
static `BackManager.instance` a dead reference for the whole of the Dash screen.

**Why it looked intermittent.** 15 files dereference `BackManager.instance` directly, while only 5 use
the self-healing `BackManager.EnsureInstance()`. Opening Settings/Hints/Credits first happens to call
`EnsureInstance()` via their `OnEnable`, which repaired the singleton — so the crash only appeared when
**Play was the first thing clicked**.

**Fix.** `InitializeOnSceneLoad` now also subscribes to `SceneManager.sceneLoaded` and re-runs
`EnsureInstance()` after every scene load. This preserves the existing "fresh back-stack per scene"
semantics (as opposed to making it `DontDestroyOnLoad`, which would carry a stale stack of destroyed
Buttons between scenes and break ESC).

**Pre-existing, not caused by the restructure** — `git diff` on `BackManager.cs` shows only the
namespace wrapper was added; no logic changed.

| Check | Before | After |
|---|---|---|
| `BackManager.instance` null on Dash | **true** | **false** |
| `BackManager` objects in scene | **0** | **1** |
| Play as first click | **NullReferenceException** | **OK** |

### Bug found and fixed — blank white shop screen (`ResizeToSpriteAspect` unit mismatch)

**Symptom.** Clicking the shop button on the Dash screen showed an almost entirely white screen with
one small parchment fish icon. No console errors.

**Root cause.** `ShopManager.ResizeToSpriteAspect` sized the Sal-T sign with:

```csharp
width = (canvasRect.rect.width * screenWidthFraction) / rect.lossyScale.x;
```

`canvasRect.rect.width` is in **canvas units** (1920), but `rect.lossyScale.x` is **absolute world
scale**. On a `ScreenSpaceOverlay` canvas `lossyScale ≈ 1`, so the mismatch was invisible. This
project's canvas is **`ScreenSpaceCamera`**, where the canvas transform is scaled to ~0.01 to map
1920 px onto ~19.2 world units. Dividing canvas units by that world scale inflated the sign to
**22809 × 8236**, so its opaque `salt_shop_sign` sprite covered the entire screen and hid every other
shop element. The one visible parchment icon was `Hat IconI`, which happens to be a later sibling and
therefore drew on top.

**Fix.** Normalise the divisor to the rect's scale *relative to the canvas*, which is correct in both
render modes:

```csharp
float canvasScale = canvasRect.lossyScale.x;
float totalScale  = rect.lossyScale.x;
if (canvasScale > 0.0001f) { totalScale /= canvasScale; }
```

**Pre-existing, not caused by the restructure** — `ResizeToSpriteAspect` is byte-identical to the
copy in `_PATP_BACKUP_pre-restructure`, and `Dash.unity` is SHA256-identical to its backup, so no
serialized data changed either.

| Check | Before | After |
|---|---|---|
| `Sal - TButton` sizeDelta | **22809.60 × 8236.80** | **211.20 × 76.27** |
| World extent (canvas spans ≈ −9.6…9.6 X) | −413.60 … 8.80 | 4.89 … 8.80 |
| Shop screen | blank white | renders fully |

### Known environmental errors (baseline — not regressions)
1. `SteamAPI_Init() failed` / `Steam is not initialized!` — the Steam client is not running and
   `steam_appid.txt` holds `480` (Valve's Spacewar test id). Expected outside Steam; achievements fall
   back to PlayerPrefs, which the code already handles.
2. `McpManagerClientHub ... Authorization failed` / `Version handshake failed` — the MCP plugin
   retrying its **cloud** hub with a stale token. Unrelated to the game; the local endpoint works.

With a scene open and not in playmode, these two are the only errors that should appear. Anything
else is new.

---

## 14. Session 2 changes (2026-07-29) — spec v3.0 pass

Full detail lives in `restructure_report.md` (§ "Session 2"). Summary of what changed in the project:

| Area | Change |
|---|---|
| Deprecated APIs (§20) | 17 call sites modernised + 3 dead `using` directives removed. **0 obsolete-API warnings left in `_Project`.** |
| Scene partitioning (§1.3) | 11 scene-exclusive scripts moved into `Scripts/Dash/`, `Scripts/Play/`, `Scripts/Splash/` with matching namespaces. 70 of 101 scripts proven genuinely shared. |
| Assemblies (§1.6) | 5 asmdefs added. `Assembly-CSharp.dll` 629 KB → 116 KB. Required moving vendor `SteamManager.cs` into the Steamworks `Runtime/` folder and adding `DOTween.Modules.asmdef`. |
| Input System (§5) | Actions asset extended with 8 real actions; C# wrapper generated; `InputManager` rewritten around it and added to Splash. Legacy `Input.*` reads retained as the authoritative path pending a 2-client playtest. |
| Scene hierarchy (§2) | Six root separators in all 3 scenes; 22 objects reparented. `DontDestroyOnLoad` objects pinned to root (see §11b). |
| Audio (§6) | 9 real call sites routed through new `AudioManager.PlaySource` / `StopSource`. Behaviour byte-identical. |
| Domain reload (§7) | `InputManager` statics reset via `[RuntimeInitializeOnLoadMethod]`. Note the project currently has `m_EnterPlayModeOptions: 0`, i.e. domain reload is still **enabled**. |

**Verified:** 0 compile errors · 0 missing script references across **3,853 components** in all three
scenes · 11/11 moved-script GUIDs still resolving · all 8 new input actions binding correctly ·
9 active Dash buttons fired with 0 throws · `Resources.Load` and StreamingAssets intact · game runs
Splash → Dash with only the two known Steam-not-running errors.

**Not verified (and not verifiable in one session):** multiplayer match flow, which needs two live
clients plus Steam + PlayFab + Photon.

### Corrections to earlier documentation

Two claims in the previous report were wrong and are corrected here:

1. `PlayerControls.inputactions` did **not** contain only Unity's default template. It already had a
   `Gameplay` map with a live `Move` action that `FishermanController.cs:408` reads.
2. The "12 direct `.Play()` audio call sites" figure counted an **Animator** call
   (`ShopManager.cs:1868`) and two calls inside `AudioManager` itself. There are 9 real ones.

Also note the user's spec (`D:\Unity_ai_Project_Structure_prompt.md` §20) is itself stale on one
point: it recommends `FindObjectsByType(..., FindObjectsSortMode.None)`, but Unity 6.5 marks
`FindObjectsSortMode` obsolete. Use `FindObjectsByType<T>()` / `FindObjectsByType<T>(FindObjectsInactive)`.

---

## 15. Session 3 changes (2026-08-07) — hat/body animation desync + client art drop

### 15.1 Bug fix — cosmetic hats animated against the body (fisherman **and** fish)

**Symptom reported:** the fisherman's hat and his body moved in *different* directions; the same
desync was visible on fish wearing hats.

**Root cause (measured, not inferred).** `CosmeticRuntimeApplier` positions every hat each
`LateUpdate` by looking the current animation frame up in a bob table. All those tables are
**0-based columns**, but every shipping animation clip names its frames **1-based**:

- `ANIM_FisherManRedHair` — 26 clips, all `…1 …2 …3 …4` (`IdleLeft1`…`IdleLeft4`)
- `Fish 1 Default` — `Idle1`…`Idle4`, `Move1`…`Move4`, `Eat1`…`Eat4`, `Fight1`…`Fight4`

`GetCurrentSpriteFrameIndex()` returned `trailingNumber % 4`, feeding the 1-based number straight in.
Every lookup was one frame late **and the final frame wrapped back onto the first**.

Ground truth was measured from the art itself (decoded PNG, topmost/centre opaque pixel per cell).
`FishermansAnimations-Head_Sheet.png` is 256×1536 = 4×24 cells at 100 PPU, and the measured
head-centre Y per frame matches `HeadCenterYGrid` **exactly** (row 0 → 19,19,20,20; row 10 →
20,19,21,21; row 11 → 21,20,21,21; row 14 → 20,19,19,20). So the table and its sign convention
(top-down pixels, `bob = (cy0 - cy) * 0.01`) were already correct — only the index was wrong:

| Body shows | True column | Measured head | Old index | Old bob | Hat moved |
|---|---|---|---|---|---|
| `IdleLeft1` | 0 | 20 (rest) | 1 | +0.01 | up ✗ |
| `IdleLeft2` | 1 | 19 (**up** 1px) | 2 | −0.01 | **down — opposite** ✗ |
| `IdleLeft3` | 2 | 21 (down 1px) | 3 | −0.01 | down ✓ |
| `IdleLeft4` | 3 | 21 (down 1px) | 0 (wrap) | 0 | flat ✗ |

**Fix.** `GetCurrentSpriteFrameIndex()` now distinguishes the project's two naming conventions:
digits preceded by `_` are a **0-based sheet slice** (`FishermansAnimations-GreenBody_Sheet_6`),
anything else is a **1-based animation frame** (`IdleLeft2`). It also parses in place instead of
allocating a `Substring` every `LateUpdate`, and the old `EndsWith("_0")` special case is gone.

`GetFishHatBobOffset` was additionally re-derived from the fish art. Measured head top across the
swim cycle (crown band, top-down px @ 50 PPU) is `1, 1, 1, 0` with the band behind it `2, 3, 3, 2` —
the head dips mid-cycle and **rises** on the final frame, but the old table dipped there. Table is
now `0, −0.01, −0.01, +0.01`, which travels with the head on every frame. This is a smaller
amplitude than before; it matches the art, but it is an art-feel change worth a look.

**Note on scope.** The modular fisherman rig (`FishermanChildAnimatorSync`,
`FishermanAnimationManager`, `head`/`chest`/`oar` children) is **not** on the shipping
`Resources/Fisherman` prefab — that prefab is a single `SpriteRenderer` driven by one Animator.
The modular path is dead code for in-game play and was left untouched; the fix covers both paths
because the sheet-slice branch preserves the old 0-based behaviour.

**Tests.** `Assets/_Project/Scripts/Tests/EditMode/` was created (first test assembly in the
project: `PanicAtThePond.Tests.EditMode.asmdef`) with `CosmeticFrameIndexTests` — 27 cases covering
the 1-based mapping, the 0-based sheet-slice mapping, the no-wrap guarantee, null/no-digit inputs,
and assertions that both bob curves follow the measured head motion. **27/27 pass.** They fail
against the old `n % 4` by construction (it maps `IdleLeft1`→1 and `IdleLeft4`→0).

### 15.2 Client art drop (2026-08-07)

Five files received. The three shop signs were **blurry upscales** in-project; the client supplied
the crisp native-resolution pixel-art originals. Replaced in place, keeping each `.meta` so the
GUIDs — and every reference — survive. Importers set to Point filter, uncompressed, no mipmaps,
Read/Write enabled (`SaltShopUI.GetOpaqueBounds` needs readable pixels).

| Client file | Destination | Was | Now | Status |
|---|---|---|---|---|
| `Back Sign with chains.png` | `Resources/ShopUI/SaltShop/back_sign.png` | 320×210 | 38×23 | replaced |
| `Closed Sign With Chains.png` | `Resources/ShopUI/SaltShop/close_sign.png` | 370×210 | 42×23 | replaced |
| `fishing sign _ sal t shop.png` | `Resources/ShopUI/SaltShop/salt_shop_sign.png` | 720×260 | 132×42 | replaced |
| `shelf..png` | `Resources/ShopUI/SaltShop/shelf.png` | — | 256×128 | wired as Sal-T shelf backdrop |
| `fisher-Sheet.png` | `Art/Animations/Fisher Man Animations/Sprite Sheets/` | — | 444×468 | **staged, not wired** |

On-screen size is safe: `SaltShopUI.CreateSignButton` calls
`SizeForSprite(sprite, SignHeight, …)`, which derives width from *the sprite's own aspect ratio* at
a fixed `SignHeight` of 150 units, so the new proportions (1.65 / 1.83 / 3.14 vs 1.52 / 1.76 / 2.77)
adapt automatically rather than stretching.

**Naming deviation, deliberate:** these keep the existing lowercase literal names rather than the
spec's `SP_PascalCase`, because they are reached by `Resources.Load` string literals
(`ShopManager.cs:310`, `SaltShopUI.cs:38-41`). This is the same documented exception as §0.

**Open questions (not guessed at):** `shelf.png` has no existing counterpart and nothing loads it —
where does it belong? `fisher-Sheet.png` is a **different fisherman design** (seated, with cooler)
at 444×468 with no transparent gutters, so its grid cannot be derived automatically; an even 3×4
split would be 148×117 cells. It does not fit the existing 4×24 / 64px / 100 PPU rig or the 26-clip
`ANIM_FisherManRedHair`. Both need direction before wiring.

### 15.3 Verification performed

- Compile: **0 errors, 0 new warnings** (Unity 6000.5.7f1, via MCP `assets-refresh` + console read).
- EditMode Test Runner via MCP: **27/27 passed**, 1.30 s.
- Art measurements taken by decoding the actual PNGs in-Editor, not assumed.
- Sign re-import confirmed: correct dimensions, `filterMode=Point`, `readable=True`, GUIDs unchanged.

**Not verified:** in-game visual confirmation of the hat bob during a live match, and the new sign
art rendered in the Sal-T shop. Both need the two-client run (Editor + desktop build). The Unity MCP
build in this project has playmode control disabled (44 of 79 tools enabled), so Play Mode could not
be driven from here.

### 15.4 Shelf wiring (2026-08-07)

`shelf.png` is now drawn by `SaltShopUI.BuildShelf()`. It is built **first** in `Build()` so uGUI's
sibling draw order puts it behind the picture frames, signs and the cosmetics standing on it, and it
degrades gracefully — a missing sprite or `ShelfHeight <= 0` simply skips it, same as the picture
frames. Two new Inspector fields on `SaltShopUI`:

| Field | Default | Meaning |
|---|---|---|
| `ShelfAnchor` | `(0.745, 0.275)` | Screen-normalized centre of the shelf unit — the midpoint of the three `SlotAnchors`. |
| `ShelfHeight` | `432` | Height in 1920×1080 units; width follows the sprite's 2:1 aspect. `0` hides it. |

Those two defaults are a **starting estimate derived from the slot anchors, not a verified visual
match** — the shelf boards need eyeballing against the hats once the shop is on screen, which is an
Inspector nudge, no code change.

`fisher-Sheet.png` remains staged and unwired at the user's direction (2026-08-07).

### 15.5 Correction — `shelf.png` is probably a background plate, not an overlay (2026-08-07)

Rendering the client art for inspection changed the reading of it. `shelf.png` is **not** a bare
shelf unit: its left ~40% is landscape (sky, mountain, water, grass) and only the right side is
shelving. At 256×128 it is exactly **one third** the size of the existing animated shop background
`Resources/Fishingshop2Frames/Fishingshop2_00..05.png` (768×384) and shares its 2:1 aspect.

That makes "background plate" the more likely intent than "overlay behind the cosmetics". Note it is
**not** the 16:9 1920×1080 background asked for in `ASSET_REQUEST.md` item 1 — that request is still
outstanding.

`SaltShopUI.BuildShelf()` and its two Inspector fields are kept, but **`ShelfHeight` now defaults to
`0`, i.e. hidden**, so nothing renders incorrectly while the intent is unconfirmed. Two ways forward,
both one change:

- **Overlay behind the hats** — set `ShelfHeight` to ~432 in the Inspector and nudge `ShelfAnchor`.
- **Shop background** — do not use `BuildShelf`; point the shop background at this art instead
  (it is the same aspect as the current 6-frame animation, so it would replace `Fishingshop2_*`
  rather than sit on top of it), and confirm with the client whether it is a static replacement or
  the first frame of a new animated set.

### 15.6 Runtime verification (2026-08-07)

A PlayMode suite was added at `Scripts/Tests/PlayMode/` (`PanicAtThePond.Tests.PlayMode.asmdef`,
`HatFollowsBodyTests`). It instantiates the **real shipping prefabs**, applies a **real cosmetic
through the real public API** (`ApplyFishermanCosmeticsByName` / `ApplyFishHatByName`), lets the
**real Animator** play a full cycle, and samples the hat's actual local Y every frame. Gameplay
MonoBehaviours are disabled on the instance because they expect a live network session.

Measured hat `localPosition.y` per body frame, against the head motion measured from the art:

| Fisherman `AC_IdelLeft` | hat Y | head (top-down px) | Fish `AC_Fish1Idel` | hat Y | head |
|---|---|---|---|---|---|
| `IdleLeft1` | 0.6700 (rest) | 20 baseline | `Idle1` | 0.2320 (rest) | 1 |
| `IdleLeft2` | 0.6800 ↑ | 19 (up 1px) | `Idle2` | 0.2220 ↓ | dips |
| `IdleLeft3` | 0.6600 ↓ | 21 (down 1px) | `Idle3` | 0.2220 ↓ | dips |
| `IdleLeft4` | 0.6600 ↓ | 21 (down 1px) | `Idle4` | 0.2420 ↑ | 0 (rises) |

Every frame now moves in the same direction as the head.

**Results:** EditMode 27/27 passed (1.30 s) · PlayMode 3/3 passed (1.95 s) · 0 compile errors.

All 79 Unity-MCP tools were enabled (was 45/79) and the state persisted through an Editor restart.
It lives in `UserSettings/AI-Game-Developer-Config.json`, which is gitignored, so it is per-machine.

---

## 16. Two-client verification (2026-08-07)

Run on `MP_LEGION`: **Unity Editor as Mirror LAN host + standalone `Build/` player as client.**

### 16.1 Build

`Assets/_Project/Scripts/Editor/ProjectBuilder.cs` added — `Panic At The Pond ▸ Build Windows Player`
builds the enabled Build Settings scenes into `Build/`. It exists as a compiled Editor script on
purpose: a delegate queued via `EditorApplication.delayCall` from a **dynamically compiled** assembly
(an MCP `script-execute` snippet) is silently dropped when that assembly unloads, so the build never
fires. Calling into a real assembly, or calling the method synchronously, both work.

Result: `RESULT=Succeeded errors=0 warnings=0 size=236MB`. Note the launcher `.exe` mtime does **not**
change on an incremental build — watch `Build/Panic At The Pond_Data/Managed/PanicAtThePond.dll`
instead. That assembly (which contains `CosmeticRuntimeApplier`) rebuilt at 11:48, after the fix.

### 16.2 What the two-client run confirmed

| Check | Result |
|---|---|
| LAN host/discovery/join across two processes | Room found and joined; both players listed in lobby |
| Match start, both fish spawn | OK, Clear Waters |
| Fish hat applied locally (host's own fish) | Cap rendered |
| **Fish hat synced to the remote client** | Cap rendered on the remote view of the host's fish |
| **Hat seated on the head across swim frames** | Flush on all sampled frames, no gap and no drift |
| New Sal-T shop art in the real build | `salt_shop_sign`, `back_sign`, `close_sign` all crisp |

### 16.3 Gotchas worth recording

- **Editor and player PlayerPrefs are separate stores.** The Editor writes to
  `HKCU\Software\Unity\UnityEditor\<Company>\<Product>`; a build writes to
  `HKCU\Software\<Company>\<Product>`. Setting a cosmetic from the Editor does **not** affect the
  standalone. This is why the first attempt showed two hatless fish.
- Fish hats are all `unlockedByDefault: false` and the test account has 0 WC, so a standalone client
  has no selectable fish hat. The Editor client was used as the hatted player instead.
- Screen capture must be DPI-aware (this machine reports 1707x1067 logical for a 2560x1600 panel) or
  every window screenshot comes out offset and cropped.
- `CopyFromScreen` captures the screen, not the window surface — an occluded game window captures
  whatever is on top of it. Raise the window before capturing.

### 16.4 Still not covered

- Nobody actually **plays**, so the round ends in "You all Starve!". The Golden-Fish → Fisherman
  transition was not reached, so the **fisherman** hat was not observed in a live networked match.
  Its positioning is covered by the PlayMode tests and the render comparison (§15.6), and its
  networking is untouched code, but it has not been seen end-to-end in a match.
- Two *different* hats on two players in the same match — blocked by the shared-prefs/unlock
  situation above.

---

## 17. Shop UI pass (2026-08-07)

### 17.1 Sal-T shop sign — FIXED

The client's `fishing sign _ sal t shop.png` is **two signs stacked in one 132×42 file**, not one
sign. Measured layout (top-down): "fishing supplies" occupies y 0–18 across x 0–131; "sal-T shop"
occupies y 19–41 across x 0–68. Importing it whole and assigning it to the customization-screen
button meant the button drew *both* signs and was sized to the combined 3.14 aspect — it rendered
oversized and overhung the right edge of the tank.

Split into two assets:

| Asset | Size | Aspect | Used by |
|---|---|---|---|
| `ShopUI/SaltShop/salt_shop_sign.png` | 69 × 23 | 3.00 | customization-screen shop button |
| `ShopUI/SaltShop/fishing_supplies_sign.png` | 132 × 19 | 6.95 | available; not yet placed |

Two changes make it sit correctly:
- `ShopManager.SaltShopSignScreenWidth` **0.22 → 0.15**. The 0.22 was tuned for the old 720×260
  sign; the replacement crop is tighter, so the same fraction rendered visibly larger.
- `Dash.unity` → `--- UI ---/Canvas_Dash/Shop/Sal - TButton` `anchoredPosition` **(66, 56) → (−120, −10)**.
  The old value pushed the sign past the right edge of the tank frame. Note this object also carries
  a baked `localScale = 2`; `ResizeToSpriteAspect` already compensates for it, so it was left alone.

Verified on screen in Play Mode: sign renders inside the tank, right-aligned, matching the client's
reference mockup. Scene saved.

### 17.2 Fish vs fisherman preview size — DIAGNOSED, NOT YET FIXED

Measured from the live customization screen (canvas units, 1920×1080 reference):

| Preview object | Sprite | Rect | localScale | Rect in canvas units | Opaque fill |
|---|---|---|---|---|---|
| `Fish 1` / `Fish 2` | `bass` / `trout` 500×500 | 500×500 | 2.20 | **1100** | 18 % (91 px of 500) |
| `FisherMan cycling hat` | `Winning1` 64×64 | 600×600 | 1.00 | **600** | 83 % (53 px of 64) |

The two previews are sized by unrelated hand-set numbers, and the source sprites have wildly
different padding (18 % vs 83 % fill), so matching the rects would *not* match the apparent size.
The correct fix is to normalise on **opaque content**, not rect: measure the shown sprite's opaque
bounds and scale so its largest content dimension hits one shared target — the same technique
`SaltShopUI.GetOpaqueBounds` already uses for shop icons. Not yet implemented.

### 17.3 Hat fit on heads — NOT FIXED (deliberately deferred)

`CosmeticRuntimeApplier` places each hat from a hand-maintained per-hat, per-state table of
position / rotation / scale (separate branches for ranger, turtle, blue cap, …). That table is the
direct cause of "some too small, some a little up, some tight", and every new hat needs a new
hand-tuned entry.

Tuning 13 hats × 26 states against the **current** art would be throwaway work: the client is
mid-way through replacing all character art at higher resolution. The structural fix has been put
into the art spec instead — see `ASSET_REQUEST_v2.md`: hats authored on the **full character canvas**
at the same grid as the body, so they composite at 0,0 and need no offset table at all.

### 17.4 New file

`ASSET_REQUEST_v2.md` at repo root — the art spec / client reply covering aspect ratio, cell sizes,
the modular-layer approach for hair / clothes / oar / boat / rods, row order, and icon sizing.

---

## 18. Sign scaling + future-proofing pass (2026-08-07)

### 18.1 Sal-T shop signs — sized by pixel scale, not height

`SaltShopUI` sized every sign to a fixed `SignHeight = 150` reference units. With the client's art
that drew "back" (38 × 23 source) at **248 × 150** — about 13 % of screen width for a small button,
far larger than the client's reference mockup.

Fixed height is also the wrong model: it forces a 19 px-tall sign and a 23 px-tall sign to render at
the same height, silently rescaling the pixel art relative to its neighbours.

Replaced with `SignPixelScale` (reference units per **source pixel**, default **2.4**) plus a new
`SizeForSpriteAtPixelScale` helper. All signs now share one scale, so:

| Sign | Source | Old (height 150) | New (scale 2.4) |
|---|---|---|---|
| back | 38 × 23 | 248 × 150 | **91 × 55** |
| close | 42 × 23 | 274 × 150 | **101 × 55** |
| salt_shop | 69 × 23 | 450 × 150 | **166 × 55** |
| fishing_supplies | 132 × 19 | 1042 × 150 | **317 × 46** |

New sign art of any size now lands at the same visual scale with no tuning.

**UNVERIFIED** — the Unity MCP server disconnected before this could be run. Needs a Play Mode check
of the Sal-T shop against the client's reference.

### 18.2 The shop title is painted into the background

`back` and `close` are real sprites and are now resizable. **"fishing supplies" and "sal-T shop" in
the shop screen are not** — they are painted into the shop background art (`Fishingshop2Frames`,
768 × 384). See the long-standing comment on `BackSignAnchor`: *"Sits directly under the 'sal-T shop'
sign painted into the background art."*

So their apparent size is whatever the background happens to be scaled to and **cannot be fixed in
code**. A background plate with no signs baked in has been added to the art spec; once that exists,
both titles become ordinary sprites placed at `SignPixelScale` like the others.

### 18.3 Measured asset inconsistency (the reason for all the sizing hacks)

| Asset | Size | Note |
|---|---|---|
| Fisherman hat icons | 64 × 64 | art fills only 20–38 % |
| Fish hat icons | **18 × 13**, **24 × 15** | ~12× smaller than the fisherman icons |
| lock | 185 × 280 | |
| picture_frame_1 / _2 | 365 × 350 / 360 × 345 | not even equal to each other |
| coin | 64 × 64 | |
| signs | 38 × 23 … 132 × 19 | |
| Fisherman frame | 64 × 64 @ 100 PPU | |
| Fish sprite | 55 × 35 @ 50 PPU | |
| Shop background | 768 × 384 (2:1) | game is 16:9 |

Nothing shares a canvas convention, which is why the code carries runtime alpha-trimming, a
`HatContentHeight` override, and a per-hat placement table. `ASSET_REQUEST_v2.md` replaces this with
one contract: in-game cosmetics on the **full character canvas**, shop icons on a **128 × 128**
canvas at ~90 % fill.

### 18.4 Future-proofing — decision and sequencing

Target: adding a cosmetic is *drop in a PNG*, no code.

That is mostly an **art contract**, not a code problem. Once every hat/hair layer is authored on the
full character canvas, the correct renderer is a zero-offset layer stack and the entire per-hat
placement table in `CosmeticRuntimeApplier` is deleted rather than extended.

Deliberate sequencing: **spec first, refactor second.** Rewriting a 2 197-line applier to a catalog-
driven layer renderer should be compiled and tested against real conforming assets, not written
blind — and the MCP server is currently down, so nothing can be compiled or play-tested this session.

---

## 19. Resolution audit (2026-08-08) — corrects §18 and ASSET_REQUEST_v2

Measured in-editor, not estimated. **Two figures in the earlier spec were wrong.**

| Claim | Previously stated | Measured | Source |
|---|---|---|---|
| Fisherman PPU | 100 | **25** | `Resources/Fisherman` SpriteRenderer |
| Fisherman target frame | 256 × 256 | **320 × 320** | derived below |
| Fish target frame | 220 × 140 | **110 × 70** | derived below |

The 100 PPU figure came from the *head sheet* importer; the shipping fisherman prefab uses a
different sprite at 25 PPU.

### Measured chain

```
Camera (Play + Dash): orthographic, size 5  ->  visible world height 10 units
Build resolution:     1920 x 1080           ->  1 world unit = 108 px on screen
```

| Character | Sprite | PPU | Prefab scale | World height | On screen | Scaling |
|---|---|---|---|---|---|---|
| Fisherman | 64 × 64 | 25 | 1.25 | 3.20 units | **346 px** | **5.40× upscale** |
| Fish | 55 × 35 | 50 | 1.0 | 0.70 units | **76 px** | **2.16× upscale** |

The 5.40× is the root cause of the soft/blocky fisherman, and because it is not an integer some
source pixels render 5 screen-pixels wide and others 6 — visible shimmer during movement.

### Proposed normalisation

Set **all character sprites to 100 PPU** and camera `orthographicSize` **5 → 5.4**. Then
1 world unit = exactly 100 px at 1080p, prefab scales become 1.0, and every future asset's pixel
size is simply *world size × 100*.

| Character | World size (unchanged) | Required source | Result |
|---|---|---|---|
| Fisherman | 3.2 × 3.2 units | **320 × 320** | exact 1:1 |
| Fish | 1.1 × 0.7 units | **110 × 70** | exact 1:1 |

Trade-off: `orthographicSize` 5 → 5.4 reveals ~8 % more world vertically. Not yet applied — it is a
framing/gameplay decision, and the art has to land first.

### Canvas inconsistency found

| Canvas | Reference resolution |
|---|---|
| `Canvas_Dash` (menus, shop) | **1920 × 1080** |
| `Canvas_Play`, `Canvas_PlayBackground` | **800 × 600** |

In-game UI therefore scales differently from menu UI. `SaltShopUI`'s hardcoded
`ReferenceResolution = 1920x1080` is correct for Dash, which is the only place it runs. Unifying Play
to 1920 × 1080 would rescale every element in that scene and needs its own verification pass — logged,
not done.

### Verified this session

- MCP reconnected; 79/79 tools available.
- §18.1 `SignPixelScale` edit **compiles** — 0 errors, EditMode **27/27 passed**.

### 19.1 Full asset inventory (measured 2026-08-08)

Every Resources prefab with a SpriteRenderer, with its target size under the 100 PPU rule
(target = world size × 100):

| Prefab | Source | PPU | Scale | World units | Target @100 PPU | Current scaling |
|---|---|---|---|---|---|---|
| FisherMan | 64 × 64 | 25 | 1.25 | 3.20 × 3.20 | **320 × 320** | 5.40× up |
| Worm / HookWorm | 15 × 21 | 35 | 1.00 | 0.43 × 0.60 | **43 × 60** | 2.86× up |
| Fish (Bass) | 55 × 35 | 50 | 1.00 | 1.10 × 0.70 | **110 × 70** | 2.00× up |
| Golden Fish | 55 × 35 | 50 | 1.00 | 1.10 × 0.70 | **110 × 70** | 2.00× up |
| Fish 2 (Trout) | 48 × 30 | 100 | 1.70 | 0.82 × 0.51 | **82 × 51** | 1.70× up |
| hookPrefab | 274 × 423 | 500 | 1.00 | 0.55 × 0.85 | 55 × 85 | 4.98× **down** |
| Drop (bubble) | 164 × 164 | 200 | 1.00 | 0.82 × 0.82 | 82 × 82 | 2.00× **down** |
| Boot (junk) | 58 × 64 | 100 | 1.00 | 0.58 × 0.64 | 58 × 64 | **1.00× exact** |
| Tire (junk) | 62 × 64 | 100 | 1.00 | 0.62 × 0.64 | 62 × 64 | **1.00× exact** |

**Six different PPU values in use: 25, 35, 50, 100, 200, 500.** `Boot` and `Tire` are already
authored at 100 PPU and already render 1:1 — they are the precedent for standardising on 100.

Only the **upscaled** rows need remaking; downscaled assets are merely oversized on disk.

### 19.2 New document

`ASSET_SPECIFICATION.md` at repo root — the complete, measured asset spec: the 100 PPU rule, per-object
target sizes, animation strip layout, the full-canvas layer rule for cosmetics, UI/sign/background
sizes, delivery conventions, the "adding a cosmetic" workflow, and the four open decisions
(camera orthoSize 5 → 5.4, PPU unification, `Canvas_Play` 800 × 600 → 1920 × 1080, and replacing the
per-hat placement table).

It supersedes the size tables in `ASSET_REQUEST_v2.md`, which remains as the client-facing message.

---

## 20. Rendering standards applied (2026-08-08)

All four decisions from §19 were approved. Two landed; two are gated on the new art by their own
definition.

### 20.1 DONE — Camera `orthographicSize` 5 → 5.4

Applied to `Main Camera` in **Play** and **Dash**, both scenes saved. Gives exactly
**100 px per world unit at 1080p**.

Immediate benefit even before new art: the current 64 × 64 fisherman now renders at **exactly 5.00×**
instead of 5.40×. Integer scaling with Point filtering means each source pixel becomes exactly 5
screen pixels, so the uneven-pixel shimmer during movement is gone. The 320 × 320 art will render at
1.00×.

Verified Dash has **no world-space SpriteRenderers** (pure UI, `ScreenSpaceCamera`), so the camera
change cannot affect its layout.

### 20.2 DONE — Canvas reference resolution unified to 1920 × 1080

`Canvas_Play` and `Canvas_PlayBackground` moved from **800 × 600 → 1920 × 1080**.
`Canvas_Dash` was already correct.

`CanvasScaler` with `match = 0.5` computes `scaleFactor = √(W/refW × H/refH)`. Changing refRes divides
that factor by exactly `√(2.4 × 1.8) = 2.0784609`, so each of `Canvas_Play`'s **15 root children** had
`localScale` multiplied by 2.0784609 to cancel it. This is exact **at every resolution**, not just
1080p — the resolution terms cancel algebraically.

Verified: all 15 children report an effective scale of 2.07846, identical to the pre-change value.
`Canvas_PlayBackground` had no direct children, so nothing to compensate.

### 20.3 GATED — PPU unification and the placement-table removal

Both must land **with** the first batch of conforming art:

- **PPU → 100** changes every sprite's world size. Doing it now would require compensating every
  prefab scale, then compensating them back when the art arrives — two risky passes instead of one.
- **The per-hat placement table** gets **deleted**, not edited. That only makes sense once
  full-canvas layers exist to verify against.

### 20.4 SaltShopUI — live Inspector tuning

The shop front is generated at runtime (`BuildOnce()` creates "SaltShop Overlay" under the root
canvas), so it is **not in the scene and not a prefab** — which is why it cannot be found in the
Hierarchy. Rather than convert it to scene-authored (a large refactor of working code), the tuning
loop was made live:

- `RebuildOverlay()` — tears down and rebuilds from current Inspector values. Exposed as a
  **`Rebuild Shop Overlay`** context-menu command on the component.
- `OnValidate` queues a rebuild while playing; `Update` performs it. Editing any field while the shop
  is open now updates it **immediately**.

Component location: `Canvas_Dash / ShopItemsPanel / Sal -t Image BackGround`.
Full field reference in `ASSET_SPECIFICATION.md` §10.

### 20.5 Asset spec — all 9 fisherman parts enumerated

`ASSET_SPECIFICATION.md` §3 now lists every part with draw order and which already exist:

| Part | Status |
|---|---|
| Boat, Oars, Body, Arms, Head, Rods | ✅ sheets exist |
| Clothes | ❌ new (client requested) |
| Hair | ⚠ only static 64 × 64 `Red_Hair` / `Black_Hair` — cannot animate with the head |
| Hat | ❌ only cropped icons |

9 parts × 24 animations = **216 files** for the base fisherman, **24 per swappable variant**. Unity
packs them into a sprite atlas at build time, so the file count carries no runtime cost — and the
alternative (one grid sheet per part at 1280 × 7680) exceeds the safe texture limit.

**Verified:** 0 compile errors · EditMode **27/27 passed** · both scenes saved.

---

## 21. Sal-T shop overlay is now scene-authored (2026-08-08)

Supersedes §20.4. The shop front was generated at runtime and therefore did not exist in the
Hierarchy. It is now **real scene objects** that can be selected, moved and resized by hand.

### What changed in `SaltShopUI`

| Member | Purpose |
|---|---|
| `authoredOverlay` (serialized) | When set, the shop uses this scene hierarchy **as-is** and the numeric layout fields are ignored. |
| `AdoptAuthoredOverlay()` | Re-binds the code side to the authored objects — caches `Coin Amount`, `Price`, `Status`, finds/creates `SaltShop Items`, and re-attaches click handlers to `Back Sign`, `Close Sign`, `Yes Button`, `No Button`. |
| `BakeOverlayIntoScene()` | Editor context-menu command. Generates the overlay once as scene objects, assigns `authoredOverlay`, leaves it **inactive**, marks the scene dirty. |
| `RebuildOverlay()` | With an authored overlay, re-binds instead of regenerating — it never destroys the designer's objects. |

Button listeners are added from code and are **not serialized**, so a baked overlay always arrives
with zero persistent listeners. `AdoptAuthoredOverlay` re-binding them on every open is what makes
scene authoring safe. Objects are matched **by name** — renaming a child breaks its wiring.

`OnDestroy` and `RebuildOverlay` both guard against destroying `authoredOverlay`, and `OnValidate`
skips its live-rebuild when one is present (the fields no longer drive layout).

### Baked hierarchy — `--- UI ---/Canvas_Dash/SaltShop Overlay`

```
SaltShop Overlay          (inactive until the shop opens)
├── Picture Frame 1       229 × 220
├── Picture Frame 2       230 × 220
├── Back Sign              91 × 55   [Button → OnBackSign]
├── Close Sign            101 × 55   [Button → OnCloseSign]
├── Coin Icon              64 × 64
├── Coin Amount           180 × 70   [TMP]
├── SaltShop Items                   ← dynamic; item cells generated here each refresh
└── Buy Popup             500 × 430  (inactive)
    ├── Title / Coin / Price / Status
    ├── Yes Button        [Button → OnConfirmPurchase]
    └── No Button         [Button → HideBuyPopup]
```

`Shelf` is absent because `ShelfHeight` is 0 — see §15.5.

### Gotcha found and fixed

`SaltShopUI` was **added at runtime** by `ShopManager.OpenSaltShopStoreFront()`, so it did not exist
in the scene in Edit Mode and the bake command could not be reached. The component is now a
permanent part of `Canvas_Dash/ShopItemsPanel/Sal -t Image BackGround`; `ShopManager`'s
`GetComponent ?? AddComponent` path still works unchanged.

The baked overlay is saved **inactive** — active would draw the shop front over the Dash menu before
the player opens the shop.

### Verified

Entered Play Mode and opened the shop:
- **1** `SaltShop Overlay` instance, not 2 → the authored one was adopted, not duplicated
- `SaltShop Items` populated with **12** children from the live rotation
- `Back Sign` / `Close Sign` carry Buttons and re-bound handlers
- `Buy Popup` correctly inactive
- Renders identically to before · 0 compile errors · EditMode **27/27**

### How to edit it

Select `--- UI ---/Canvas_Dash/SaltShop Overlay` in the Dash scene and move/resize any child
normally. To regenerate from the numeric fields, use **Rebuild Shop Overlay**; to start over, clear
`authoredOverlay` and run **Bake Overlay Into Scene** again.

---

## 22. Full asset audit (2026-08-08) — corrects §19/§21 specs

Swept **all 643 image files** under `Assets/_Project`. Three errors found in the v1.0 spec.

| # | v1.0 said | Actually | Impact |
|---|---|---|---|
| 1 | 24 animations | **26** | `LeftToRightPole` + `RightPoleToOar` live in the BoatFacingLeft set, not the Default folder. Matches `ANIM_FisherManRedHair`'s 26 clips. |
| 2 | 4 frames each | **4, but `Dead` = 6 and worm `Dance` = 5** | a flat "4 frames" brief would have under-delivered |
| 3 | Fisherman = 6 part sheets | also **336 loose 64 × 64 frames** | this is the real source art |

### The 336-frame duplication

`UI/UI/Game UI/Fisherman/` holds the animation set **redrawn once per hair colour**:

| Variant | Animations | Frames |
|---|---|---|
| `Used animation ui` (default) | 26 | 112 |
| `Default animation ui (Black Hair)` | 26 | 112 |
| `Default animation ui (Red Hair)` | 26 | 112 |
| | | **336** |

**Adding a hair colour today costs 112 new frames. With layers it is 26.** This is the strongest
single argument for the layered contract and is now the headline of the client message.

`New Fisherman/` holds the 5 newer part sheets (Arms, Boat, GreenBody, Oars, Rods) at 256 × 1536.

### Categories previously missing from the spec

| Category | Count | Size | Verdict |
|---|---|---|---|
| `BG2_Frames` | 8 | **1920 × 1080** | ✅ already correct — precedent for 16:9 |
| Achievements | 8 | 256 × 256 (×7), 64 × 64 (×1) | ✅ bar one outlier |
| Regions / Flags | 6 | 500 × 500 | ✅ consistent |
| Preview composites | 32 | 500 × 500 | ✅ consistent |
| Golden Fish | 18 | 55 × 35 | 🟠 needs 110 × 70 |
| LargeMouthBass | 26 | 55 × 35 | 🟠 needs 110 × 70 |
| Worm (For Hook) | 9 | 15 × 21 | 🔴 needs 43 × 60 |
| Environment tiles | 15 | 32 × 32, 160 × 96, 32 × 256 | 🟡 world size unverified |
| Bucket of worms | 3 | 64 × 64 | 🟡 world size unverified |
| ExtractedPDFSprites | 23 | mixed (818 × 762 …) | reference art, not shipped |
| Dash UI | 58 | mixed | UI, sized by canvas |

Environment tiles and the worm bucket are the only two whose world size was not measured — they are
not on a Resources prefab with a SpriteRenderer, so the ×100 rule cannot be applied to them without
opening the scene they are used in. Flagged 🟡 rather than guessed.

### Documents

- **`ASSET_SPECIFICATION.md` v2.0** — the internal authority. Every asset, measured, with targets.
- **`CLIENT_MESSAGE.md`** — new, paste-ready client message built from the same data.

`ASSET_REQUEST_v2.md` is now superseded by both and can be archived.

---

## 23. Hat sync — the real remaining bug (2026-08-08)

User reported the fisherman hat was **still** out of sync after §15.1. They were right, and my §15.6
verification was insufficient.

### Root cause: bob magnitude, not direction

§15.1 fixed the frame *index*. It did not fix the pixel→world conversion.

`CosmeticRuntimeApplier` converted its pixel-measured bob tables with a hard-coded
`* 0.01f`, i.e. **1 pixel = 0.01 world units, which is only true at 100 PPU**:

| Character | Sprite PPU | 1 px = | Code used | Error |
|---|---|---|---|---|
| Fisherman | **25** | 0.0400 units | 0.0100 | **4× too small** |
| Fish | **50** | 0.0200 units | 0.0100 | **2× too small** |

So the hat moved the right way but a quarter of the distance the head moved — which still reads as
"out of sync".

### Why the tests missed it

`FishermanHeadBob_FollowsMeasuredHeadMotion` and the PlayMode suite asserted only **direction**
(`Is.GreaterThan` / `Is.LessThan`). They never checked **magnitude**, so a 4×-too-small bob passed
every one of them. That is a genuine gap in the original test design, not a flaky test.

### Fix

Added `UnitsPerPixel`, read from the body sprite at runtime:

```csharp
private float UnitsPerPixel =>
    rootRenderer?.sprite != null && rootRenderer.sprite.pixelsPerUnit > 0f
        ? 1f / rootRenderer.sprite.pixelsPerUnit
        : DefaultUnitsPerPixel;
```

`GetFishermanHeadOffset`, `GetFishermanHeadBobOffset` and `GetFishHatBobOffset` now take
`unitsPerPixel`, and `GetFishHatBobOffset`'s table was rewritten in **pixels** (`0, -1, -1, +1`)
rather than pre-multiplied units. Deriving it from the sprite also means it stays correct after the
project migrates to 100 PPU — the value simply becomes 0.01 on its own.

Measured after the fix (fisherman, `AC_IdelLeft`): bob is now exactly **±0.0400** = 1 source pixel,
was ±0.0100.

### Tests added

Five magnitude cases, parameterised over 25 / 50 / 100 PPU, asserting that a 1 px head movement
produces exactly 1 px of hat movement in world units. **EditMode 32/32 pass** (was 27).

### STILL OPEN — fish hat disappears

Reported: the fish hat shows the first time, is only visible to the other player / host, and is gone
after a round restart. **Not fixed — not yet reproduced.**

What is established so far:
- `ApplyFishHatByName` removes the hat when the name is empty **or the sprite fails to resolve**
  (`CosmeticRuntimeApplier.cs:689-700`), so both "never applied" and "silently dropped" land in the
  same place.
- `ApplyFishSpeciesByName` does **not** delete the hat child — it only swaps sprite, animator and
  scale on the fish root. Species is not the culprit.
- `ShopManager.ClearFishHatAfterDelay` only resets the shop *preview*, not the saved selection.
- The owner path (`OnStartClient` → `isLocalPlayer` → `ApplyFishHatByName` + `CmdSetHat`) and the
  remote path (SyncVar hook → `ApplySyncedHat`) call the same function with different name sources:
  `GetSelectedFishHatName()` locally vs `syncedHatName` remotely. A divergence between those two is
  the most likely explanation for "visible to others but not to me".
- `FishController_Mirror` already carries an `ApplySyncedCosmeticsWhenReady` coroutine with a 3 s
  retry, which suggests known spawn-timing races on this path.

Next step is a two-client run with logging on both name sources at spawn and at round restart —
diagnosis by reading alone would be guesswork.

---

## 24. Fish hat root cause found and fixed (2026-08-09)

Diagnosed on the user's machine with two live clients. **Both bugs were real and are now fixed.**

### The smoking gun

The build's saved PlayerPrefs contained:

```
SelectedFishHatCosmetic = FisherMan_Hat_-Default_-_Fishing_Hat
```

A **fisherman** hat parked in the **fish** hat slot. It can never resolve to a valid fish cosmetic,
and `ApplyFishHatByName` removes the hat when the sprite fails to resolve
(`CosmeticRuntimeApplier.cs:689-700`) — so the fish rendered bare. That is the whole "fish hat not
showing up" report.

### How it got there — a routing fall-through

`ShopManager.OnCosmeticItemSelected` routes a click by hierarchy:

```csharp
if (belongsToFishCosmetics && !belongsToFishermanCosmetics) isFishermanCosmetic = false;
else if (belongsToFishermanCosmetics)                       isFishermanCosmetic = true;
// button in NEITHER root -> flag silently keeps its value from the PREVIOUS click
```

A button under neither root inherits the last click's category, so a fisherman hat can be written via
`SelectFishHat`. The comment above that block claims fish and fisherman "can never" cross-contaminate
— the fall-through defeated it.

### Three-layer fix

1. **Routing** (`ShopManager`) — the `else` branch now falls back to the sprite's own
   `shop_config.json` category via `IsFishermanCategoryCosmetic`, and warns. A button with no sprite
   and no root is ignored rather than filed by stale state.
2. **Setter guard** (`CosmeticRuntimeApplier.SelectFishHat`) — refuses a fisherman-category sprite
   outright, so the mistake is impossible however the caller was routed.
3. **Self-heal** (`EnsureSelectionsLoaded`) — an already-corrupted save is detected on load, cleared,
   and warned about, instead of silently rendering no hat forever.

### A false positive caught during verification

The first version of `IsFishermanCategorySprite` used `AreSpritesMatching(..., exactOnly: false)`.
Loose matching classified the **fish** hat `cap` as the **fisherman** hat `FisherMan_Hat_-Blue_Cap`,
because one name contains the other once separators are stripped — which would have rejected a valid
fish hat. Changed to exact normalised-id comparison. This is pinned by a test.

### Hat/body sync — magnitude, not direction (see §23)

Same session: the bob was converting pixels to world units with a hard-coded `0.01`, correct only at
100 PPU. The fisherman is 25 PPU and the fish 50, so hats moved **4×** and **2×** too little. Now
derived from the body sprite's own PPU at runtime. Measured after: ±0.0400 = exactly 1 source pixel.

### Verification

- EditMode **42/42 pass** (was 27) — added 5 magnitude cases and 10 category cases.
- Rebuilt the player (`PanicAtThePond.dll` 00:13, 0 errors).
- Two live clients, LAN host + join, Clear Waters: **both fish wear the cap, on both clients**, hat
  correctly seated on the head. Screenshot evidence captured.
- The corrupted pref in the user's save was repaired to `cap` directly so the test was meaningful;
  the self-heal would otherwise have cleared it and left no hat until re-selected.

### Not covered

The **fisherman** hat was not observed in a live match — reaching it requires eating the Golden Fish.
Its magnitude fix is verified by measurement, unit tests and an old-vs-new render, but not in-match.

---

## 25. Hat sync — actual root cause, and a real verification method (2026-08-09)

User pushed back that the hat was still out of sync and that screenshot sampling could easily catch a
lucky frame. Both points were correct. §15.1 and §23 each fixed a real bug but neither was the main
one.

### The real cause: the offset table describes the wrong artwork

`HeadCenterYGrid` was measured from `FishermansAnimations-Head_Sheet.png`. The shipping fisherman
renders **composited** sprites (`IdleLeft1`, `CastingLeft2`, …) whose head moves differently:

| Clip | Real head Δ per frame | Table Δ | |
|---|---|---|---|
| `AC_IdelLeft` | 0, −1, −1, 0 | 0, −1, **+1, +1** | ✗ |
| `AC_CastingLeft` | 0, **+6**, +1, 0 | 0, **0**, +1, +1 | ✗ 6 px out |
| `AC_IdelRight` | 0, −1, **−1**, 0 | 0, −1, **0**, 0 | ✗ |
| `AC_MoveForward` | 0, −1, −1, 0 | 0, −1, −1, 0 | ✓ coincidence |

No amount of index or magnitude correction fixes a table that describes different art.

### Isolating the head

A plain topmost-opaque-pixel scan does not work — the **fishing rod and line reach above the head**
in the casting frames and would report the rod tip, swinging the hat wildly. Requiring the first row
whose widest continuous opaque run is **>= 6 px** skips those thin structures and finds the head
reliably. With that, most animations rest at crown row 12 and casting genuinely leans (0, +6, +1, 0).

### Fix: measure the sprite that is actually drawn

New `HeadCrownTable` ScriptableObject (`Scripts/Data/`) holds sprite-name -> crown-row, baked by
`HeadCrownTableBuilder` (`Scripts/Editor/`, menu **Panic At The Pond ▸ Rebuild Head Crown Table**).
**149 sprite crowns baked.**

Baked rather than measured at runtime because the frame textures are not Read/Write enabled — there
are 104 distinct fisherman frame textures and flipping them all readable to compute a constant would
be wasteful.

`CosmeticRuntimeApplier` now captures a `baseCrownRow` when the cosmetic is placed and computes
`bob = (baseCrownRow - currentCrownRow) * UnitsPerPixel` from the live sprite, falling back to the
legacy table only for sprites missing from the bake. **The hand-maintained 24x4 grid no longer drives
the vertical axis**, and the whole thing self-corrects when the art is replaced — one menu click.

### Verification that actually holds

Replaced screenshot spot-checks with an exhaustive per-frame audit: walk every clip on the shipping
prefabs, compare how far the head really moved against how far the hat moved, in pixels.

```
Fisherman : 102 frames checked   WORST mismatch 0 px
Fish      :  24 frames checked   WORST mismatch 0 px
```

Kept as `HatTracksHeadTests` so it cannot regress. **EditMode 45/45 pass** (was 27 at the start of
this thread).

### Why the earlier tests passed a broken build

- The first suite asserted only **direction** (`Is.GreaterThan` / `Is.LessThan`) — a 4x-too-small bob
  passes that.
- The PlayMode suite sampled **one** animation (`AC_IdelLeft`), which happens to be one of the clips
  where the wrong table coincidentally has the right sign.
- Screenshots sample whichever frame the renderer happened to be on.

The lesson is recorded here because it applies to the next cosmetic bug too: assert the *quantity*
against independently measured art, across *all* states, not a sampled frame.

### Still not covered

The fisherman has not been observed mid-match — reaching him needs the Golden Fish eaten. The audit
above drives every one of his 26 animations directly, which is stronger evidence than a single
in-match look, but it is not the same as seeing it in play.

### 25.1 Handoff document created

`HANDOFF_HatSync.md` at repo root — self-contained brief for the next session covering the problem,
the three fixes that were real but insufficient, why the reconstruct-in-LateUpdate approach cannot
reach 100 %, the animated-`HeadAnchor` plan, an explicit do-not-do list, the verification method and
why earlier verification failed, project facts (PPUs, prefab structure, MCP quirks), and a definition
of done.

**Decision recorded:** the fix is to key the hat attachment point *inside* the AnimationClips rather
than reconstruct it each frame. The anchor and the sprite are then sampled by the same evaluator on
the same timeline, so desync is structurally impossible. Success is measured by the placement code
getting *smaller* — the ~2,200-line `CosmeticRuntimeApplier` placement logic collapses to parenting.

Not implemented this session, by request.

## 26. HeadAnchor implemented — hats are positioned by the animation (2026-08-09)

Implements the §4 plan from `HANDOFF_HatSync.md`. Two things happened: the verification method was
rebuilt first, then the fix.

### 26.1 The previous audit could not fail — it asserted `A == A`

`HatTracksHeadTests` was described in §25 and in the handoff as the verification that works. It was
not. The runtime positioned the hat from `HeadCrownTable`:

```
hat.localPosition.y = base.y + (baseCrownRow − crown_f) × upp
```

and the test computed the expected head movement from the *same* table. Substituting one into the
other cancels `baseCrownRow` and leaves `headMoved ≡ hatMoved` identically, for every sprite in every
clip. **A table baked from the wrong artwork — the §25 bug — would still have reported 0 px.** Two
further gaps: the error was rounded to whole pixels, so anything under 0.5 px passed silently, and
only one hat per character was sampled.

Rewritten to measure the crown rows from the PNGs on disk, compare in sub-pixel units, and cover
every hat in `shop_config.json`. A coverage guard fails if a new shop hat is not in the audit list,
and `HeadCrownTable_MatchesSourceArt` now checks the baked table against the art independently.

**What the honest audit found on the then-current code**, all of which the old one reported as 0 px:

| Hat | Result |
|---|---|
| Blue Cap, Ranger, Soda | 0 px |
| Red Cap, Chef, Fish Hat | 52/102 frames, **0.12 px** out (hardcoded `0.035f` vs a real `0.04`) |
| TurtleHat | 57/102 frames, **2.00 px** out — visibly detached |
| Default Fishing Hat | no child hat; correctly pre-baked into `ANIM_FisherManYellowHat` |

The 0.12 px cases are exactly what whole-pixel rounding had been hiding.

### 26.2 The fix

`HeadAnchorBuilder` (menu: **Panic At The Pond ▸ Rebuild Head Anchors**) adds a `HeadAnchor` child to
each character prefab and writes one `m_LocalPosition.y` key per sprite keyframe into every clip that
character plays, measured from the art. Result: **4 prefabs, 174 frames across 42 clips**; one frame
skipped (`AC_Dead/Dead6_0`, no head-sized opaque run — a dead fish is upside down).

The keys use **constant tangents**. Sprite swaps are stepped, so an interpolating anchor would glide
while the head jumped — which is the drift being fixed. `HeadAnchor_TracksHead_InEveryFrameOfEveryAnimation`
samples at arbitrary times, not only on keyframes, specifically to catch that.

Cosmetics are now parented to the anchor and their placement code is gone. What the caller authors
relative to the root becomes a one-time sit offset (`localPosition − anchor.localPosition`), so no
hat needed re-authoring.

### 26.3 What was deleted

`CosmeticRuntimeApplier` **2,430 → 1,915 lines**. Gone: `ApplyFishermanAnimationOffset` with all six
per-hat branch families (ranger, turtle, blue cap, red cap, chef, soda, fish/frog),
`ApplyFishAnimationOffset`, `GetFishHatBobOffset`, `TryGetMeasuredHeadBob`, `baseCrownRow`. A copy of
the pre-change file is at `Backup/CosmeticRuntimeApplier.pre-anchor.cs.bak`.

**Kept deliberately:** `HeadCenterYGrid`, `GetFishermanHeadOffset`, `GetFishermanHeadBobOffset` and
`GetCurrentSpriteFrameIndex` are still reached by the *hair* path, which selects a slice of the
animated head sheet rather than sitting on top of the head. That is a different mechanism and there
was no measurement basis for changing it, so it was left alone. `HeadCrownTable` is no longer a
runtime dependency for hats but is still the reference both audits measure against.

### 26.4 Known regression — needs an artist

The deleted branches carried per-state data that was **not** head tracking: X offsets, Z rotations up
to ±25°, a 160° Y-flip on the chef hat, and per-state scale. Six fisherman hats now use their rest
rotation and scale in the move / pole / winning states. Tracking is exact; the tilt is not restored.
Restoring it is §4.4 step 3 — key `localRotation` on the anchor in the Animation window. This cannot
be verified by measurement and was not attempted.

### 26.5 Verification

- EditMode **58/58**, PlayMode **3/3**.
- All 13 shop hats × every clip, sub-pixel: **0 px worst mismatch**.
- The three PlayMode tests asserted hand-written per-frame directions taken from the old wrong-artwork
  `HeadCenterYGrid` — they would have *rejected* the correct behaviour. Rewritten to compare against
  `HeadCrownTable`, which is legitimate now that the hat's position comes from the clip curve instead.
- Three `CosmeticFrameIndexTests` cases that reflected on the deleted `GetFishHatBobOffset` were
  removed; `HatTracksHeadTests` covers the same ground far more thoroughly.

Not done: a visual pass on casting and fighting in a running build, and §4.2 (one clip, many curves)
and §4.3 (catalog) remain open.

### 26.6 Follow-up — the anchor rest height (2026-08-09)

Reported from a build: the hat floated a full head above the fisherman. Cause was in §26.2's own
change, not the old code.

A hat's sit offset is computed at apply time as `authoredPosition − anchor.localPosition`, but
cosmetics are applied *before* the Animator has evaluated anything, and `HeadAnchorBuilder` created
the anchor at zero. So the subtraction was a no-op, the hat kept its root-relative Y as though it
were anchor-relative, and the moment a clip started the anchor jumped to the real crown height and
took the hat 0.80 units (20 px) with it:

```
prefab anchor.y = 0.0000   sitOffset.y = 0.6700   after Play, hat-in-root y = 1.4700
```

Fixed by seeding the prefab anchor with the crown height of the character's own rest sprite, so the
subtraction is meaningful whether or not the animation has been sampled. Fisherman anchor rest is now
`y = 0.80`, sit offset `−0.13`, hat lands at the authored `0.67`.

**Why the audit missed it.** `Hat_TracksHead_...` compares frame-to-frame *movement*. A constant
offset is invisible to it — every frame is wrong by the same amount and the deltas still match. It
reported 0 px on all 102 frames while the hat sat 20 px too high.

Added `Hat_SitsOnTheHead_NotMerelyParallelToIt`, which checks the *absolute* gap between the hat and
the measured crown on every frame of every clip. Confirmed it fails on the bug: forcing the fisherman
anchor back to zero fails all 7 fisherman hats while every tracking test still passes.

EditMode **71/71**, PlayMode **3/3**.

**Do not hand-edit `HeadAnchor.localPosition` in the prefab** — *Rebuild Head Anchors* overwrites it.
To move one hat, edit `GetFishermanHatTransform` / `GetFishHatTransform`; to move every hat together,
change `CrownLocalY` in `HeadAnchorBuilder` and re-run the menu item.

## 27. Hat findings from 2026-08-09 — diagnosed, then reverted

Everything in this section was implemented and then **reverted** by the user back to `632ce062`. The
code is gone; the measurements and the diagnoses are not, and every bug listed here is still live in
the tree. Recorded so the next attempt starts from evidence instead of repeating the investigation.

### 27.1 The head finder reports the fishing rod

`MinHeadRunPixels = 6` was chosen because "the rod and line are only a pixel or two wide". Measured
from the art, **the rod is exactly 6 px**, so on casting frames the finder returns the rod:

```
CastingLeft1   ≥6px → row 3,  cx 57, w 6      real head → row 11, cx 30.5, w 11
CastingLeft3   ≥6px → row 4,  cx 18, w 6      real head → row 12, cx 29.5, w 11
```

Raising the threshold does not fix it: `CastingLeft3` has an 18 px arm-and-rod run *above* the head
and `CastingLeft2` an 11 px rod run — the head's own width — at x=58. Width alone and position alone
are both ambiguous; together they are decisive. The fisherman's head is **11 px wide at x≈30.5,
row 11–12** in every frame, so calibrating on the rest sprite and requiring later frames to match in
both width and position works for him.

Consequence: `HeadCrownTable`'s casting entries and the casting anchors are keyed off the rod. The
handoff's §2.3 claim that `AC_CastingLeft`'s head moves "+6 px" was this artefact (row 3→9), never a
measurement of the head.

### 27.2 The audit never drove the animations it named

Every failure line in §26 read `AC_CastingLeft/IdleLeft2` — every clip reporting *IdleLeft* sprites.
`animator.Play(clip.name)` takes a **state** name ("Idel Right"), not a clip name ("AC_IdelRight"),
and silently ignores unknown names, so all 26 clips re-measured the same four idle sprites.
"102 frames checked" was four sprites. `AnimationClip.SampleAnimation` at each real keyframe time
avoids the state machine entirely. The PlayMode suite had the same defect —
`Animator.GotoState: State could not be found` in the console.

### 27.3 The head moves horizontally

1 px in `AC_IdelLeft`, 2 px in `AC_MoveForward`, **4 px in `AC_FightingLeft`** — 0.16 units at
25 PPU. Y-only anchoring cannot cover it. No flip handling is needed when adding X: right-facing
clips use their own artwork (`IdleRight1`) and nothing animates `m_FlipX`.

### 27.4 Deleting the placement code also deleted the mirroring

The fisherman never mirrors through `flipX` — his body renderer's `flipX` is permanently false. Hats
were oriented from `FishermanController.isLeft` in two ways at once:

```csharp
transform.localEulerAngles = isLeft ? new Vector3(x, 0f, z) : baseLocalRotation;
cosmeticRenderer.flipX = !isLeft;
```

The red cap, turtle hat and default fishing hat carry **`y = -160°`** in their authored rotation —
that Y rotation *is* their mirror. §26 replaced both with `flipX = rootRenderer.flipX`, leaving those
three permanently mirrored and the rest never mirrored. Restoring it needs only one direction flag
shared by all hats, not a per-hat table.

### 27.5 Fish cosmetics never stuck

`IsPreviewSprite` matched names starting with `"fisherman "`, `"fishermna "`, `"fishaerman "` or
containing `"preview"`. Every fisherman preview matches one; **no fish preview matches any** — they
are "Fish Cap Hat", "Trout Boat hat", "fish polish hat". So:

```
select 'Fish Cap Hat' -> stored 'Fish Cap Hat' -> rendered 'Fish Cap Hat'
```

The fish wore a picture of a fish wearing a hat, which reads as both "the selection didn't stick" and
"I never see the hat". Identifying previews by their Resources folder rather than by spelling fixes
it.

### 27.6 Species swapping breaks anchor units

`ApplyFishSpeciesByName` swaps sprite and controller onto the **same** GameObject, but the bass is
**50 PPU** and the trout **100 PPU**, so anchor values in sprite-local units halve:

```
bass anchor rest y = 0.3300 → hat at 0.2320
after swap          0.1500 → hat at 0.0520     (0.18 units / ~9 px lower)
```

Separately, `IsTroutFish` read the GameObject *name*, which is stale after a swap — a bass then wears
trout offsets, ~12 px out. The rendered sprite is the reliable signal.

### 27.7 A fish has no head to find

Unlike the fisherman's distinct 11 px band, a fish is a smooth taper (`row1 w=6 → row2 w=10 →
row3 w=12`). When it tilts, the top row is already wider than the reference and nothing matches, so
the scan walks down the body:

```
AC_Fish1Idel   Idle1: row=1   Idle2: row=2   Idle3: row=32   Idle4: row=1
```

Row 32 of a 35 px sprite — the anchor drops 0.62 units onto the belly and snaps back every cycle,
which is the "fish hat animating a lot in Y" report. Bounding how far the head may sit from its
reference row stops the catastrophe, but **four fisherman frames (`AC_CastingRight`,
`AC_ReelingRight`) and the dead-fish frames were still ~12 px out** when the work was reverted.
Silhouette scanning is reliable for a distinct head band and fragile without one.

### 27.8 Coverage gaps found

- The audit only ever instantiated `Resources/Fish`; **`Fish 2` (the trout) is also selectable** and
  has its own hat placement (`GetTroutFishHatTransform`) and dead pose.
- The default fishing hat is pre-baked into `ANIM_FisherManYellowHat` and correctly has no hat child,
  so it cannot be audited the same way as the other seven.
- Tracking tests compare frame-to-frame *movement*, so a constant offset is invisible to them. A hat
  20 px above the head passed every frame.

### 27.9 The verification lesson

Four suites in a row were green while the build was visibly wrong, each because the test derived its
expectation from the same source the code used. See §26.1 for the `A == A` case, §27.2 for the
animation that never played, and §27.7 for the anchor compared against the function that baked it.
**Before trusting a green suite here, break the thing it checks and confirm it goes red.** The one
check that proved genuinely independent was bounding frame-to-frame anchor movement, which needs no
reference measurement at all.

### 27.10 Untracked leftovers

The revert restored tracked files only. Still on disk and now orphaned:
`Scripts/Editor/HeadMeasurement.cs` (referenced by nothing),
`Scripts/Tests/EditMode/FishCosmeticSelectionTests.cs` (asserts the §27.5 fix, so it now fails), and
`Backup/CosmeticRuntimeApplier.pre-anchor.cs.bak` (the pre-anchor file — worth keeping).

### 27.11 Not investigated

Rooms not appearing over the internet. Nothing changed between `632ce062` and the revert touched a
scene or any networking file — only five `.cs` files, the anchor values, the `.anim` curves and
`SO_HeadCrownTable`. Internet rooms go through Photon (`CoustomeRoomManager`); LAN uses Mirror and
was working in the logs. To isolate it, test the room list at `96a5fc99` — the commit before this
work — rather than assuming either way.
