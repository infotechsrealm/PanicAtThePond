using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PanicAtThePond.Managers
{
    /// <summary>
    /// The only script that is allowed to read raw input. Everything else subscribes to the typed
    /// events below.
    ///
    /// The actions modelled here mirror the controls the game actually uses today:
    /// horizontal/vertical movement, rod selection, hold X+V to charge a cast, release to cast,
    /// right mouse to reel in, space to mash, escape to go back.
    ///
    /// The project's <c>PlayerControls.inputactions</c> asset still contains only Unity's default
    /// template actions (Move/Look/Fire) and has <c>generateWrapperCode: 0</c>. Generating a C# wrapper
    /// from it would produce a class that does not describe this game, so the asset needs authoring to
    /// match these actions before the ruleset's "generate C# class" step becomes meaningful.
    /// </summary>
    [DisallowMultipleComponent]
    public class InputManager : MonoBehaviour
    {
        private const string HORIZONTAL_AXIS = "Horizontal";
        private const string VERTICAL_AXIS = "Vertical";

        [SerializeField] private bool _keepAliveAcrossScenes = true;

        private Vector2 _moveInput;
        private bool _castHeldLastFrame;

        /// <summary>Singleton access point. Null until the manager's <c>Awake</c> has run.</summary>
        public static InputManager Instance { get; private set; }

        /// <summary>Current movement input. X drives boat/fish horizontal, Y drives rod selection.</summary>
        public Vector2 MoveInput => _moveInput;

        /// <summary>True while both cast keys (X and V) are held.</summary>
        public bool IsCastHeld { get; private set; }

        /// <summary>Raised every frame the movement vector changes.</summary>
        public event Action<Vector2> OnMoveChanged;

        /// <summary>Raised on the frame the cast charge begins (X+V pressed).</summary>
        public event Action OnCastStarted;

        /// <summary>Raised on the frame the cast is released.</summary>
        public event Action OnCastReleased;

        /// <summary>Raised on the frame the reel-in input (right mouse) is pressed.</summary>
        public event Action OnReelPressed;

        /// <summary>Raised on each mash press during the mash phase (space).</summary>
        public event Action OnMashPressed;

        /// <summary>Raised on the frame the drop-carried-junk input (Q) is pressed.</summary>
        public event Action OnDropJunkPressed;

        /// <summary>Raised on the frame the back/cancel input (escape) is pressed.</summary>
        public event Action OnBackPressed;

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
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (keyboard == null)
            {
                return;
            }

            ReadMovement(keyboard);
            ReadCast(keyboard);
            ReadDiscreteActions(keyboard, mouse);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            Cleanup();
        }

        private void ReadMovement(Keyboard keyboard)
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) { horizontal -= 1f; }
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) { horizontal += 1f; }
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) { vertical -= 1f; }
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) { vertical += 1f; }

            Vector2 next = new Vector2(horizontal, vertical);
            if (next != _moveInput)
            {
                _moveInput = next;
                OnMoveChanged?.Invoke(_moveInput);
            }
        }

        private void ReadCast(Keyboard keyboard)
        {
            bool castHeld = keyboard.xKey.isPressed && keyboard.vKey.isPressed;
            IsCastHeld = castHeld;

            if (castHeld && !_castHeldLastFrame)
            {
                OnCastStarted?.Invoke();
            }
            else if (!castHeld && _castHeldLastFrame)
            {
                OnCastReleased?.Invoke();
            }

            _castHeldLastFrame = castHeld;
        }

        private void ReadDiscreteActions(Keyboard keyboard, Mouse mouse)
        {
            if (mouse != null && mouse.rightButton.wasPressedThisFrame)
            {
                OnReelPressed?.Invoke();
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                OnMashPressed?.Invoke();
            }

            if (keyboard.qKey.wasPressedThisFrame)
            {
                OnDropJunkPressed?.Invoke();
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                OnBackPressed?.Invoke();
            }
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
        }
    }
}
