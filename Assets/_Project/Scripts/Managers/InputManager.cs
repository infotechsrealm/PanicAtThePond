using System;
using UnityEngine;
using UnityEngine.InputSystem;

using PanicAtThePond.Data;

namespace PanicAtThePond.Managers
{
    /// <summary>
    /// The only script that is allowed to read raw input. Everything else subscribes to the typed
    /// events below.
    ///
    /// Backed by the generated <see cref="PlayerControls"/> wrapper over
    /// <c>Assets/_Project/Scripts/Data/PlayerControls.inputactions</c>. The actions mirror the
    /// controls the game actually uses: WASD/arrow movement (Y also drives rod selection), hold
    /// X+V to charge a cast and release to fire, right mouse to reel in, Space to mash, Q to drop
    /// carried junk, Escape to go back, plus the F9/F11 debug toggles.
    ///
    /// <para><b>Migration state.</b> This manager is authoritative but not yet the sole reader:
    /// the legacy <c>Input.*</c> call sites are still live so behaviour is unchanged until they
    /// are switched over. Flip <see cref="_isAuthoritative"/> on once the new path has been
    /// playtested with two live clients, then delete the legacy reads.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InputManager : MonoBehaviour
    {
        [Tooltip("Leave OFF until the new input path has been playtested with two live clients. " +
                 "While OFF the legacy Input.* call sites remain the source of truth and this " +
                 "manager only publishes events.")]
        [SerializeField] private bool _isAuthoritative;

        [SerializeField] private bool _keepAliveAcrossScenes = true;

        private PlayerControls _controls;
        private Vector2 _moveInput;

        /// <summary>Singleton access point. Null until the manager's <c>Awake</c> has run.</summary>
        public static InputManager Instance { get; private set; }

        /// <summary>Current movement input. X drives fish/fisherman horizontal, Y drives rod selection.</summary>
        public Vector2 MoveInput => _moveInput;

        /// <summary>True while both cast keys (X and V) are held.</summary>
        public bool IsCastHeld { get; private set; }

        /// <summary>True once the legacy <c>Input.*</c> readers have been retired.</summary>
        public bool IsAuthoritative => _isAuthoritative;

        /// <summary>The control scheme currently driving input, for swapping UI prompt glyphs.</summary>
        public string ActiveControlScheme { get; private set; } = "Keyboard&Mouse";

        /// <summary>Raised whenever the movement vector changes.</summary>
        public event Action<Vector2> OnMoveChanged;

        /// <summary>Raised on the frame the cast charge begins (X+V held).</summary>
        public event Action OnCastStarted;

        /// <summary>Raised on the frame the cast is released (either key let go).</summary>
        public event Action OnCastReleased;

        /// <summary>Raised on the frame the reel-in input (right mouse) is pressed.</summary>
        public event Action OnReelPressed;

        /// <summary>Raised on each mash press during the mash phase (Space).</summary>
        public event Action OnMashPressed;

        /// <summary>Raised on the frame the drop-carried-junk input (Q) is pressed.</summary>
        public event Action OnDropJunkPressed;

        /// <summary>Raised on the frame the back/cancel input (Escape) is pressed.</summary>
        public event Action OnBackPressed;

        /// <summary>Raised on the frame the fullscreen toggle (F11) is pressed.</summary>
        public event Action OnToggleFullscreenPressed;

        /// <summary>Raised on the frame the room-filter debug toggle (F9) is pressed.</summary>
        public event Action OnToggleRoomFilterPressed;

        /// <summary>Raised when the active control scheme changes (keyboard ↔ gamepad).</summary>
        public event Action<string> OnControlSchemeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_keepAliveAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            _controls = new PlayerControls();
        }

        private void OnEnable()
        {
            if (_controls == null)
            {
                return;
            }

            _controls.Gameplay.Enable();
            _controls.Global.Enable();

            _controls.Gameplay.Move.performed += HandleMove;
            _controls.Gameplay.Move.canceled += HandleMove;
            _controls.Gameplay.Cast.performed += HandleCastStarted;
            _controls.Gameplay.Cast.canceled += HandleCastReleased;
            _controls.Gameplay.Reel.performed += HandleReel;
            _controls.Gameplay.Mash.performed += HandleMash;
            _controls.Gameplay.DropJunk.performed += HandleDropJunk;

            _controls.Global.Back.performed += HandleBack;
            _controls.Global.ToggleFullscreen.performed += HandleToggleFullscreen;
            _controls.Global.ToggleRoomFilter.performed += HandleToggleRoomFilter;

            InputSystem.onDeviceChange += HandleDeviceChange;
        }

        private void OnDisable()
        {
            if (_controls == null)
            {
                return;
            }

            _controls.Gameplay.Move.performed -= HandleMove;
            _controls.Gameplay.Move.canceled -= HandleMove;
            _controls.Gameplay.Cast.performed -= HandleCastStarted;
            _controls.Gameplay.Cast.canceled -= HandleCastReleased;
            _controls.Gameplay.Reel.performed -= HandleReel;
            _controls.Gameplay.Mash.performed -= HandleMash;
            _controls.Gameplay.DropJunk.performed -= HandleDropJunk;

            _controls.Global.Back.performed -= HandleBack;
            _controls.Global.ToggleFullscreen.performed -= HandleToggleFullscreen;
            _controls.Global.ToggleRoomFilter.performed -= HandleToggleRoomFilter;

            InputSystem.onDeviceChange -= HandleDeviceChange;

            _controls.Gameplay.Disable();
            _controls.Global.Disable();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            Cleanup();
        }

        /// <summary>Switches the active action map so input is gated by context, not by booleans.</summary>
        /// <param name="isGameplayActive">True to enable the Gameplay map, false to leave only Global live.</param>
        public void SetGameplayInputEnabled(bool isGameplayActive)
        {
            if (_controls == null)
            {
                return;
            }

            if (isGameplayActive)
            {
                _controls.Gameplay.Enable();
            }
            else
            {
                _controls.Gameplay.Disable();
            }
        }

        private void HandleMove(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
            OnMoveChanged?.Invoke(_moveInput);
        }

        private void HandleCastStarted(InputAction.CallbackContext context)
        {
            IsCastHeld = true;
            OnCastStarted?.Invoke();
        }

        private void HandleCastReleased(InputAction.CallbackContext context)
        {
            IsCastHeld = false;
            OnCastReleased?.Invoke();
        }

        private void HandleReel(InputAction.CallbackContext context) => OnReelPressed?.Invoke();

        private void HandleMash(InputAction.CallbackContext context) => OnMashPressed?.Invoke();

        private void HandleDropJunk(InputAction.CallbackContext context) => OnDropJunkPressed?.Invoke();

        private void HandleBack(InputAction.CallbackContext context) => OnBackPressed?.Invoke();

        private void HandleToggleFullscreen(InputAction.CallbackContext context) => OnToggleFullscreenPressed?.Invoke();

        private void HandleToggleRoomFilter(InputAction.CallbackContext context) => OnToggleRoomFilterPressed?.Invoke();

        private void HandleDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change != InputDeviceChange.Added && change != InputDeviceChange.Reconnected)
            {
                return;
            }

            string scheme = device is Gamepad ? "Gamepad" : "Keyboard&Mouse";
            if (scheme == ActiveControlScheme)
            {
                return;
            }

            ActiveControlScheme = scheme;
            OnControlSchemeChanged?.Invoke(scheme);
        }

        /// <summary>
        /// Clears the static instance before a new play session. Required because the project may run
        /// with Domain Reload disabled, which would otherwise carry a destroyed instance across runs.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Cleanup()
        {
            OnMoveChanged = null;
            OnCastStarted = null;
            OnCastReleased = null;
            OnReelPressed = null;
            OnMashPressed = null;
            OnDropJunkPressed = null;
            OnBackPressed = null;
            OnToggleFullscreenPressed = null;
            OnToggleRoomFilterPressed = null;
            OnControlSchemeChanged = null;

            _controls?.Dispose();
            _controls = null;
        }
    }
}
