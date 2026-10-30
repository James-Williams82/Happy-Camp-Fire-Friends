using System.Collections;
using CampfireFriends.Construction;
using CampfireFriends.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CampfireFriends.Network
{
    /// <summary>
    /// The world-space session menu: Host, Join, Leave and Close, with a status line. It forwards
    /// clicks to the <see cref="NetworkSession"/> and shows its state; it keeps none of its own.
    /// It opens at start and with <see cref="Toggle"/> (the left menu button), in front of the player.
    /// </summary>
    public class SessionMenu : MonoBehaviour
    {
        private const float TrackingTimeout = 5f;

        [SerializeField] private NetworkSession _session;

        [Header("Game mode (chosen offline; joiners play the host's)")]
        [SerializeField] private Button _creative;
        [SerializeField] private Button _survival;

        [Header("Buttons")]
        [SerializeField] private Button _host;
        [SerializeField] private Button _join;
        [SerializeField] private Button _leave;
        [SerializeField] private Button _close;

        [SerializeField] private TMP_Text _status;

        [Header("Presentation")]
        [Tooltip("The visible panel. A child, so this component keeps listening while it's hidden.")]
        [SerializeField] private GameObject _panel;

        [SerializeField] private bool _openAtStart = true;
        [Tooltip("Never closer than half a metre, even with hands.")]
        [SerializeField] private ViewerPlacement _placement = new ViewerPlacement { HandDistance = 0.5f };

        private void Awake()
        {
            if (_host != null) _host.onClick.AddListener(OnHost);
            if (_join != null) _join.onClick.AddListener(OnJoin);
            if (_leave != null) _leave.onClick.AddListener(OnLeave);
            if (_close != null) _close.onClick.AddListener(Close);
            if (_creative != null) _creative.onClick.AddListener(() => Choose(GameMode.Creative));
            if (_survival != null) _survival.onClick.AddListener(() => Choose(GameMode.Survival));
        }

        private IEnumerator Start()
        {
            if (_session != null) _session.Changed += Refresh;
            Refresh();
            if (_panel != null) _panel.SetActive(false);
            if (!_openAtStart) yield break;

            // Open once the headset is tracking, or the menu is placed in front of a head on the floor.
            float giveUp = Time.realtimeSinceStartup + TrackingTimeout;
            while (!_placement.ViewerTracked && Time.realtimeSinceStartup < giveUp) yield return null;
            Open();
        }

        private void OnDestroy()
        {
            if (_session != null) _session.Changed -= Refresh;
        }

        public void Toggle()
        {
            if (_panel != null && _panel.activeSelf) Close();
            else Open();
        }

        public void Open()
        {
            if (_panel == null) return;
            _placement.PlaceInFront(transform);
            _panel.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnHost() => _session.Host();
        private void OnJoin() => _session.Join();
        private void OnLeave() => _session.Leave();

        private void Choose(GameMode mode)
        {
            if (_session != null && _session.State != NetworkSessionState.Offline) return;
            GameModes.Select(mode);
            Refresh();
        }

        private void Refresh()
        {
            if (_session == null) return;

            var state = _session.State;

            // The mode is picked before hosting or playing alone; a session keeps the host's.
            bool offline = state == NetworkSessionState.Offline;
            ShowMode(_creative, GameMode.Creative, offline);
            ShowMode(_survival, GameMode.Survival, offline);

            SetVisible(_host, state == NetworkSessionState.Offline);
            SetVisible(_join, state == NetworkSessionState.Offline);
            SetVisible(_leave, state == NetworkSessionState.Hosting || state == NetworkSessionState.Joined);

            if (_status != null) _status.text = StatusText();
        }

        private string StatusText()
        {
            string players = _session.PlayerCount == 1 ? "1 player" : $"{_session.PlayerCount} players";
            string mode = $"{GameModes.DisplayName(GameModes.Active)} mode";
            switch (_session.State)
            {
                case NetworkSessionState.Starting: return "Connecting…";
                case NetworkSessionState.Hosting: return $"{mode} · Hosting \"{_session.SessionName}\" · {players}";
                case NetworkSessionState.Joined: return $"{mode} · In \"{_session.SessionName}\" · {players}";
                default:
                    return string.IsNullOrEmpty(_session.LastError)
                        ? $"{mode} · Playing offline. Host \"{_session.SessionName}\", or Join it."
                        : _session.LastError;
            }
        }

        /// <summary>A mode button: shows which is chosen, and greys out modes that aren't built yet.</summary>
        private static void ShowMode(Button button, GameMode mode, bool visible)
        {
            if (button == null) return;
            SetVisible(button, visible);

            bool available = GameRules.For(mode).Available;
            button.interactable = available;

            string name = GameModes.DisplayName(mode);
            string text = !available ? $"{name} (coming soon)" : GameModes.Selected == mode ? $"{name} (selected)" : name;
            foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                if (label.name == "Label") label.text = text;
        }

        private static void SetVisible(Button button, bool visible)
        {
            if (button != null) button.gameObject.SetActive(visible);
        }
    }
}
