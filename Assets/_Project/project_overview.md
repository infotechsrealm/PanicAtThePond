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
