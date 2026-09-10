using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using PanicAtThePond.Managers;
using PanicAtThePond.UI;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PanicAtThePond.Testing
{
    /// <summary>
    /// Drives one side of a two-player match without a human, and reports what that side actually
    /// observed. Dormant unless <c>-autotest</c> is on the command line, so shipping builds are
    /// unaffected.
    /// </summary>
    /// <remarks>
    /// <para><b>It drives the game, it does not re-implement it.</b> Every step calls the same
    /// method the button calls — <c>CreateCustomeRoom</c>, <c>PhotonNetwork.JoinRoom</c> — and reads
    /// the same state the UI reads. A step that reimplemented the flow would pass while the real
    /// path was broken, which is the failure mode this harness exists to catch.</para>
    ///
    /// <para><b>Every line is prefixed and timestamped</b> so the two players' logs can be merged
    /// and read as one interleaved transcript afterwards.</para>
    ///
    /// <para>Command line:
    /// <c>-autotest host|client [-testroom NAME] [-testnick NAME] [-testtimeout SECONDS]</c></para>
    /// </remarks>
    public sealed class AutoTestDirector : MonoBehaviour
    {
        public const string Prefix = "[AUTOTEST]";

        private const string RoleArg = "-autotest";
        private const string RoomArg = "-testroom";
        private const string NickArg = "-testnick";
        private const string TimeoutArg = "-testtimeout";
        private const string FishHatArg = "-testfishhat";
        private const string FishermanHatArg = "-testfishermanhat";
        private const string HoldArg = "-testhold";

        private string role = "host";
        private string roomName = "AUTOTEST_ROOM";
        private string nickName = "AutoHost";
        private float stepTimeout = 45f;
        private string fishHat;
        private string fishermanHat;
        private bool hold;

        private float startTime;
        private int failures;
        private int checksRun;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (string.IsNullOrEmpty(ReadArg(RoleArg)))
            {
                return; // not a test run
            }

            var go = new GameObject("~AutoTestDirector");
            DontDestroyOnLoad(go);
            go.AddComponent<AutoTestDirector>();
        }

        /// <summary>
        /// Starts the harness inside the Editor, where there is no command line to read. The Editor
        /// side is the one with MCP attached, so this is what makes visual checks possible: it can
        /// be screenshotted and inspected while a built player holds the other seat.
        /// </summary>
        public static AutoTestDirector StartInEditor(string role, string room, string fishHat,
            string fishermanHat = null, bool hold = true)
        {
            var go = new GameObject("~AutoTestDirector");
            DontDestroyOnLoad(go);
            var d = go.AddComponent<AutoTestDirector>();
            d.configured = true;
            d.role = (role ?? "client").Trim().ToLowerInvariant();
            d.roomName = room;
            d.nickName = d.role == "host" ? "EditorHost" : "EditorClient";
            d.fishHat = fishHat;
            d.fishermanHat = fishermanHat;
            d.hold = hold;
            d.stepTimeout = 90f;
            d.Begin();
            return d;
        }

        private bool configured;

        private void Awake()
        {
            if (configured)
            {
                return; // StartInEditor already supplied the configuration and called Begin
            }

            role = (ReadArg(RoleArg) ?? "host").Trim().ToLowerInvariant();
            roomName = ReadArg(RoomArg) ?? roomName;
            nickName = ReadArg(NickArg) ?? (role == "host" ? "AutoHost" : "AutoClient");
            if (float.TryParse(ReadArg(TimeoutArg), out float parsed) && parsed > 0f)
            {
                stepTimeout = parsed;
            }

            fishHat = ReadArg(FishHatArg);
            fishermanHat = ReadArg(FishermanHatArg);
            hold = ReadFlag(HoldArg);

            Begin();
        }

        private void Begin()
        {
            startTime = Time.realtimeSinceStartup;
            Application.runInBackground = true;
            Mute();

            ApplySelection(Shop.CosmeticRuntimeApplier.SelectedFishHatPrefKey, fishHat, "fish hat");
            ApplySelection(Shop.CosmeticRuntimeApplier.SelectedFishermanHatPrefKey, fishermanHat, "fisherman hat");
            if (fishHat != null || fishermanHat != null)
            {
                PlayerPrefs.Save();
            }

            Log("INFO", $"role={role} room='{roomName}' nick='{nickName}' product='{Application.productName}' "
                + $"unity={Application.unityVersion} timeout={stepTimeout}s hold={hold} audio=muted");
            StartCoroutine(RunScenario());
        }

        /// <summary>
        /// Silences the run. Two players start on one desktop and both would otherwise fight over
        /// the audio device and make unattended runs unusable to sit next to.
        /// </summary>
        /// <remarks>
        /// <c>AudioListener.volume</c> is the global master, downstream of every AudioSource and
        /// mixer the game sets, so this cannot be undone by the game's own volume handling. Nothing
        /// in the project writes to it, but <see cref="Update"/> re-pins it anyway: a mute that
        /// silently lapses halfway through a run is worse than no mute at all.
        /// </remarks>
        private static void Mute()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
        }

        private void Update()
        {
            if (AudioListener.volume != 0f || !AudioListener.pause)
            {
                Mute();
            }
        }

        // ------------------------------------------------------------------ scenario

        private IEnumerator RunScenario()
        {
            yield return Step("reach Dash scene", () => SceneManager.GetActiveScene().name == "Dash");

            // Singletons, not FindAnyObjectByType: the Dash scene carries inactive copies of these
            // managers whose serialized fields are unassigned, and picking one of those makes the
            // harness report failures that the running game does not have.
            yield return Step("managers available",
                () => CoustomeRoomManager.Instance != null && CreateJoinManager.Instance != null);

            CoustomeRoomManager rooms = CoustomeRoomManager.Instance;
            CreateJoinManager menu = CreateJoinManager.Instance;
            if (rooms == null || menu == null)
            {
                Fail($"managers missing (rooms={rooms != null}, menu={menu != null})");
                yield return Finish();
                yield break;
            }

            SetNickName();
            VerifySelectionSurvived();
            ForcePhotonMode(menu);

            if (role == "host")
            {
                yield return HostFlow(rooms, menu);
            }
            else
            {
                yield return ClientFlow(rooms, menu);
            }

            yield return Finish();
        }

        /// <summary>
        /// Nothing connects to Photon on its own -- the game connects when the player presses a
        /// button, so the harness presses the same buttons in the same order.
        /// </summary>
        private IEnumerator HostFlow(CoustomeRoomManager rooms, CreateJoinManager menu)
        {
            Click(menu, "Create");
            yield return new WaitForSeconds(0.5f);

            if (!FillCreateRoomForm(rooms))
            {
                yield break;
            }

            // First Continue connects (the game shows a preloader); a second one creates the room
            // once the connection is up. This mirrors exactly what OnClickAction does.
            Click(menu, "Continue");
            yield return Step("Photon connected and in lobby",
                () => PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InLobby);
            LogConnection();

            Click(menu, "Continue");

            yield return Step("host is in the room",
                () => PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.Name == roomName);
            Log("INFO", $"room='{PhotonNetwork.CurrentRoom.Name}' visible={PhotonNetwork.CurrentRoom.IsVisible} "
                + $"open={PhotonNetwork.CurrentRoom.IsOpen} max={PhotonNetwork.CurrentRoom.MaxPlayers}");

            yield return Step("client joined (2 players in room)",
                () => PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount >= 2);

            ReportRoomMembers();

            // Only the master client can start, and customeStartGame closes the room to exactly the
            // players present before loading Play on everyone via PhotonNetwork.LoadLevel.
            yield return new WaitForSeconds(1f);
            Click(menu, "Start");
            yield return PlayPhase();
        }

        private IEnumerator ClientFlow(CoustomeRoomManager rooms, CreateJoinManager menu)
        {
            Click(menu, "Join");
            yield return new WaitForSeconds(0.5f);

            Click(menu, "JoinCustome"); // connects when offline
            yield return Step("Photon connected and in lobby",
                () => PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InLobby);
            LogConnection();

            // The assertion the original room-list bug would have caught: the client has to actually
            // SEE the host's room in the list it builds from OnRoomListUpdate.
            yield return Step($"room '{roomName}' appears in the client's room list",
                () => rooms.aliveRooms != null && rooms.aliveRooms.ContainsKey(roomName));

            var visible = new List<string>();
            if (rooms.aliveRooms != null)
            {
                foreach (KeyValuePair<string, RoomInfo> kv in rooms.aliveRooms)
                {
                    visible.Add($"{kv.Key}({kv.Value.PlayerCount}/{kv.Value.MaxPlayers})");
                }
            }

            Log("INFO", $"client sees {visible.Count} room(s): {string.Join(", ", visible)}");

            // Join the way a player does: click the row in the room table. That row is what sets
            // CoustomeRoomManager.joinRoomName, so going straight to the join call would skip the
            // table entirely -- and the table is exactly where the "room does not show up" class of
            // bug lives.
            RoomRowPrefab row = FindRoomRow(roomName);
            if (row == null)
            {
                Fail($"no room row in the table for '{roomName}' even though the list contains it");
                yield break;
            }

            Log("STEP", $"selecting room row '{roomName}'");
            row.SelectRoom();
            yield return new WaitForSeconds(0.25f);

            if (rooms.joinRoomName == null || rooms.joinRoomName.text != roomName)
            {
                Fail($"selecting the row did not set joinRoomName "
                    + $"(got '{(rooms.joinRoomName == null ? "null" : rooms.joinRoomName.text)}')");
            }

            Click(menu, "JoinCustome");

            yield return Step("client is in the room",
                () => PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.Name == roomName);
            yield return Step("both players present",
                () => PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount >= 2);

            ReportRoomMembers();
            yield return PlayPhase();
        }

        /// <summary>
        /// Both sides run this once the match starts. The cosmetic report is the payload: a hat that
        /// renders for one player and not the other shows up as a difference between the two sides'
        /// COSMETIC lines for the same owner.
        /// </summary>
        private IEnumerator PlayPhase()
        {
            yield return Step("Play scene loaded", () => SceneManager.GetActiveScene().name == "Play");

            // Players, fish and cosmetics are spawned across several frames after the load, and
            // remote cosmetics additionally wait on a network round trip.
            yield return new WaitForSeconds(6f);

            ReportPlayers();
            ReportCosmetics();
            yield return Shot("play");

            yield return new WaitForSeconds(4f);
            Log("STEP", "second cosmetic sample (catches cosmetics applied late)");
            ReportCosmetics();
            yield return Shot("play-late");
        }

        /// <summary>Every networked player object this side can see, and who owns it.</summary>
        private void ReportPlayers()
        {
            int count = 0;
            foreach (PhotonView view in FindObjectsByType<PhotonView>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (view == null)
                {
                    continue;
                }

                count++;
                Log("PLAYER", $"view={view.ViewID} owner={view.OwnerActorNr} "
                    + $"{(view.IsMine ? "MINE" : "remote")} object='{view.gameObject.name}' "
                    + $"active={view.gameObject.activeInHierarchy}");
            }

            Log("INFO", $"networked objects visible from this side: {count}");
        }

        /// <summary>The room table row whose label matches, or null when the table has no such row.</summary>
        private static RoomRowPrefab FindRoomRow(string wanted)
        {
            foreach (RoomRowPrefab row in FindObjectsByType<RoomRowPrefab>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (row != null && row.roomName != null && row.roomName.text == wanted)
                {
                    return row;
                }
            }

            return null;
        }

        private void Click(CreateJoinManager menu, string action)
        {
            Log("STEP", $"OnClickAction(\"{action}\")");
            try
            {
                menu.OnClickAction(action);
            }
            catch (Exception e)
            {
                Fail($"OnClickAction(\"{action}\") threw {e.GetType().Name}: {e.Message}");
            }
        }

        /// <summary>
        /// The Create/Join handlers read the LAN toggle and route to Mirror when it is on. This
        /// harness tests the Photon path, so the toggle is forced off before any of them run.
        /// </summary>
        private void ForcePhotonMode(CreateJoinManager menu)
        {
            Toggle lan = GetPrivateField<Toggle>(menu, "LAN") ?? GetPublicField<Toggle>(menu, "LAN");
            if (lan != null)
            {
                lan.isOn = false;
            }

            if (GS.Instance != null)
            {
                GS.Instance.isLan = false;
            }

            Log("STEP", $"forced Photon mode (LAN toggle {(lan == null ? "not found" : "set false")})");
        }

        private void LogConnection()
        {
            Log("INFO", $"region='{PhotonNetwork.CloudRegion}' appVersion='{PhotonNetwork.AppVersion}' "
                + $"server={PhotonNetwork.Server} nick='{PhotonNetwork.NickName}'");
        }

        /// <summary>
        /// Has this player render itself to a PNG.
        /// </summary>
        /// <remarks>
        /// Capturing a built player's window from the desktop does not work: Windows refuses
        /// SetForegroundWindow to a background process, so the grab returns whatever was actually on
        /// screen (usually the wallpaper), and DPI scaling resizes it. Unity's own capture renders
        /// the real backbuffer regardless of focus, occlusion or scaling -- so each side produces its
        /// own screenshot and both can be compared honestly.
        /// </remarks>
        private IEnumerator Shot(string label)
        {
            string dir = System.IO.Path.Combine(
                System.IO.Directory.GetCurrentDirectory(), "Build", "TestLogs", "shots");
            try
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            catch (Exception e)
            {
                Log("INFO", $"could not create shot directory: {e.Message}");
                yield break;
            }

            string file = System.IO.Path.Combine(dir, $"{role}-{label}.png");
            if (System.IO.File.Exists(file))
            {
                try { System.IO.File.Delete(file); } catch { }
            }

            ScreenCapture.CaptureScreenshot(file);

            // CaptureScreenshot completes at end of frame and writes asynchronously.
            for (int i = 0; i < 120 && !System.IO.File.Exists(file); i++)
            {
                yield return new WaitForEndOfFrame();
            }

            Log("SHOT", System.IO.File.Exists(file)
                ? $"{label} -> {file}"
                : $"{label} FAILED to write {file}");
        }

        // ------------------------------------------------------------------ observations

        private void ReportRoomMembers()
        {
            if (!PhotonNetwork.InRoom)
            {
                Fail("ReportRoomMembers called while not in a room");
                return;
            }

            var sb = new StringBuilder();
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                sb.Append($"[{p.ActorNumber}] '{p.NickName}'{(p.IsLocal ? " (me)" : string.Empty)}"
                    + $"{(p.IsMasterClient ? " (master)" : string.Empty)} ");
            }

            Log("INFO", $"room members ({PhotonNetwork.CurrentRoom.PlayerCount}): {sb}");
        }

        /// <summary>
        /// Records every cosmetic this side can see, tagged by whether it belongs to the local
        /// player. "The hat shows for everyone except its owner" is only visible by comparing these
        /// two logs, so this deliberately records the owner rather than just the sprite.
        /// </summary>
        private void ReportCosmetics()
        {
            int found = 0;
            foreach (SpriteRenderer sr in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (sr == null || !sr.gameObject.name.Contains("Cosmetic"))
                {
                    continue;
                }

                found++;
                Transform owner = sr.transform.root;
                PhotonView view = owner.GetComponentInChildren<PhotonView>();
                string ownership = view == null
                    ? "no PhotonView"
                    : (view.IsMine ? "MINE" : $"remote(actor {view.OwnerActorNr})");

                Log("COSMETIC", $"{ownership} owner='{owner.name}' node='{sr.gameObject.name}' "
                    + $"sprite='{(sr.sprite == null ? "NULL" : sr.sprite.name)}' "
                    + $"enabled={sr.enabled} activeInHierarchy={sr.gameObject.activeInHierarchy} "
                    + $"order={sr.sortingOrder} scale={sr.transform.lossyScale.x:0.###}");
            }

            Log("INFO", $"cosmetic nodes visible from this side: {found}");
        }

        // ------------------------------------------------------------------ plumbing

        private void ApplySelection(string prefKey, string spriteName, string label)
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                return;
            }

            PlayerPrefs.SetString(prefKey, spriteName);
            Log("STEP", $"equipped {label} '{spriteName}' (PlayerPrefs['{prefKey}'])");
        }

        /// <summary>
        /// Reports what the game kept of the requested selection. The loader deletes a key whose
        /// sprite it cannot resolve, so a name that looks fine on the command line can silently
        /// become "nothing equipped" -- that has to be visible, not inferred from an empty report.
        /// </summary>
        private void VerifySelectionSurvived()
        {
            if (!string.IsNullOrEmpty(fishHat))
            {
                string kept = Shop.CosmeticRuntimeApplier.GetSelectedFishHatName();
                if (string.IsNullOrEmpty(kept))
                {
                    Fail($"fish hat '{fishHat}' did not resolve to a sprite - the game dropped the selection");
                }
                else
                {
                    Log("INFO", $"fish hat selection kept: '{kept}'");
                }
            }

            if (!string.IsNullOrEmpty(fishermanHat))
            {
                string kept = Shop.CosmeticRuntimeApplier.GetSelectedFishermanHatName();
                if (string.IsNullOrEmpty(kept))
                {
                    Fail($"fisherman hat '{fishermanHat}' did not resolve to a sprite - the game dropped the selection");
                }
                else
                {
                    Log("INFO", $"fisherman hat selection kept: '{kept}'");
                }
            }
        }

        private void SetNickName()
        {
            if (GS.Instance != null)
            {
                GS.Instance.nickName = nickName;
            }

            PhotonNetwork.NickName = nickName;
            Log("STEP", $"nickname set to '{nickName}'");
        }

        /// <summary>
        /// Fills the create-room InputFields the same way a player typing into them would.
        /// </summary>
        /// <remarks>
        /// The fields are <c>[SerializeField] private</c>, so this reaches them by reflection. That
        /// is deliberate: the alternative is widening the game's API purely for a test, and a test
        /// should not change the shape of the thing it measures.
        /// </remarks>
        private bool FillCreateRoomForm(CoustomeRoomManager rooms)
        {
            InputField name = GetPrivateField<InputField>(rooms, "createRoomName");
            InputField password = GetPrivateField<InputField>(rooms, "roomPasswordInput");
            Text nameError = GetPrivateField<Text>(rooms, "createRoomNameError");
            Text passwordError = GetPrivateField<Text>(rooms, "roomPasswordInputError");

            if (name == null)
            {
                Fail("create-room name InputField not found - cannot drive room creation");
                return false;
            }

            name.text = roomName;
            // InputFields carry a characterLimit. Silently keeping a truncated name would have the
            // host create "AUTOTEST_4" while the client hunts for "AUTOTEST_4679" forever, so this
            // reports the truncation and then works with the name that actually exists.
            if (name.text != roomName)
            {
                Fail($"room name truncated by the InputField: asked for '{roomName}', "
                    + $"field kept '{name.text}' (characterLimit={name.characterLimit}). "
                    + "Use a room name within that limit.");
                roomName = name.text;
            }
            // No password, so the client's join path never raises the password popup. The field is
            // optional: some scenes leave it unassigned and the game copes.
            if (password != null) password.text = string.Empty;
            else Log("INFO", "roomPasswordInput unassigned on this manager; leaving password empty");
            if (nameError != null) nameError.text = string.Empty;
            if (passwordError != null) passwordError.text = string.Empty;
            if (rooms.maxPlayers <= 0) rooms.maxPlayers = 4;

            Log("STEP", $"create-room form filled: name='{name.text}' maxPlayers={rooms.maxPlayers}");
            return true;
        }

        private static T GetPrivateField<T>(object target, string field) where T : class
        {
            FieldInfo info = target.GetType().GetField(field,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return info?.GetValue(target) as T;
        }

        private static T GetPublicField<T>(object target, string field) where T : class
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public);
            return info?.GetValue(target) as T;
        }

        private IEnumerator Step(string description, Func<bool> condition)
        {
            checksRun++;
            float deadline = Time.realtimeSinceStartup + stepTimeout;
            Log("STEP", description);

            while (Time.realtimeSinceStartup < deadline)
            {
                bool ok = false;
                try
                {
                    ok = condition();
                }
                catch (Exception e)
                {
                    Log("INFO", $"condition threw (still waiting): {e.GetType().Name}: {e.Message}");
                }

                if (ok)
                {
                    Log("OK", description);
                    yield break;
                }

                yield return new WaitForSeconds(0.25f);
            }

            Fail($"TIMEOUT after {stepTimeout}s waiting for: {description}");
        }

        private IEnumerator Finish()
        {
            Log(failures == 0 ? "RESULT-PASS" : "RESULT-FAIL",
                $"role={role} checks={checksRun} failures={failures}");
            yield return new WaitForSeconds(1f);

            if (hold)
            {
                // Stay in the match. The point of holding is to be looked at -- screenshotted from
                // the Editor, or left on screen while the other side is inspected. Keep refreshing a
                // screenshot so that whatever gets triggered from the other side is visible here
                // within a few seconds, without needing to talk to this process.
                Log("INFO", "holding in match (not quitting); refreshing hold screenshot every 8s");
                while (true)
                {
                    yield return new WaitForSeconds(8f);
                    ReportCosmetics();
                    yield return Shot("hold");
                }
            }

            Log("INFO", "quitting");
            Application.Quit(failures == 0 ? 0 : 1);
        }

        private void Fail(string message)
        {
            failures++;
            Log("FAIL", message);
        }

        private void Log(string kind, string message)
        {
            Debug.Log($"{Prefix} {role} t={Time.realtimeSinceStartup - startTime:0.00} {kind} {message}");
        }

        private static bool ReadFlag(string key)
        {
            foreach (string a in Environment.GetCommandLineArgs())
            {
                if (string.Equals(a, key, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ReadArg(string key)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
