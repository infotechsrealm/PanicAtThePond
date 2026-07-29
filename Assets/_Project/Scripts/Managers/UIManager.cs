using System.Collections.Generic;
using UnityEngine;

namespace PanicAtThePond.Managers
{
    /// <summary>
    /// Owns show/hide for named UI panels so gameplay and manager code never calls
    /// <c>SetActive</c> on UI objects directly.
    ///
    /// This project uses uGUI (Canvas), not UI Toolkit — the ruleset's UITK guidance explicitly does
    /// not apply here because the existing scenes are already Canvas-based. Panels register
    /// themselves at startup rather than being found with <c>GameObject.Find</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private bool _keepAliveAcrossScenes = false;

        private readonly Dictionary<string, GameObject> _panels = new Dictionary<string, GameObject>();
        private readonly List<string> _openPanels = new List<string>();

        /// <summary>Singleton access point. Null until the manager's <c>Awake</c> has run.</summary>
        public static UIManager Instance { get; private set; }

        /// <summary>Ids of every panel currently shown, in the order they were opened.</summary>
        public IReadOnlyList<string> OpenPanels => _openPanels;

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

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            Cleanup();
        }

        /// <summary>
        /// Registers <paramref name="panel"/> under <paramref name="panelId"/>. Safe to call again with
        /// the same id — the newest registration wins, which keeps scene reloads working.
        /// </summary>
        public void RegisterPanel(string panelId, GameObject panel)
        {
            if (string.IsNullOrEmpty(panelId) || panel == null)
            {
                Debug.LogWarning("[UIManager] RegisterPanel called with an empty id or null panel.");
                return;
            }

            _panels[panelId] = panel;
        }

        /// <summary>Removes a panel registration. Does not touch the panel's active state.</summary>
        public void UnregisterPanel(string panelId)
        {
            if (string.IsNullOrEmpty(panelId))
            {
                return;
            }

            _panels.Remove(panelId);
            _openPanels.Remove(panelId);
        }

        /// <summary>Activates the panel registered under <paramref name="panelId"/>.</summary>
        public void ShowPanel(string panelId)
        {
            if (!TryGetPanel(panelId, out GameObject panel))
            {
                return;
            }

            panel.SetActive(true);
            if (!_openPanels.Contains(panelId))
            {
                _openPanels.Add(panelId);
            }
        }

        /// <summary>Deactivates the panel registered under <paramref name="panelId"/>.</summary>
        public void HidePanel(string panelId)
        {
            if (!TryGetPanel(panelId, out GameObject panel))
            {
                return;
            }

            panel.SetActive(false);
            _openPanels.Remove(panelId);
        }

        /// <summary>Deactivates every registered panel.</summary>
        public void HideAllPanels()
        {
            foreach (KeyValuePair<string, GameObject> pair in _panels)
            {
                if (pair.Value != null)
                {
                    pair.Value.SetActive(false);
                }
            }

            _openPanels.Clear();
        }

        /// <summary>True when the panel is registered and currently active.</summary>
        public bool IsPanelOpen(string panelId)
        {
            return TryGetPanel(panelId, out GameObject panel) && panel.activeSelf;
        }

        private bool TryGetPanel(string panelId, out GameObject panel)
        {
            panel = null;

            if (string.IsNullOrEmpty(panelId))
            {
                return false;
            }

            if (!_panels.TryGetValue(panelId, out panel) || panel == null)
            {
                Debug.LogWarning($"[UIManager] No panel registered under id '{panelId}'.");
                return false;
            }

            return true;
        }

        private void Cleanup()
        {
            _panels.Clear();
            _openPanels.Clear();
        }
    }
}
