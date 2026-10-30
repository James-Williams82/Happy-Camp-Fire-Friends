using System;
using System.Collections;
using CampfireFriends.Arrival;
using CampfireFriends.Game;
using CampfireFriends.Network;
using CampfireFriends.Shifts;
using TMPro;
using UnityEngine;

namespace CampfireFriends.Menu
{
    /// <summary>
    /// The signpost menu at the campfire across the lake. Play → pick a mode → play alone, host or
    /// join. Then the night lifts to the shift's afternoon, and everyone boards the ferry; the
    /// host (or a solo player) rings its bell to cross to camp. The planks are reused page to page.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Tooltip("The signpost's planks, top to bottom.")]
        [SerializeField] private PlankButton[] _planks = new PlankButton[0];
        [SerializeField] private TMP_Text _note;

        [SerializeField] private NetworkSession _session;
        [SerializeField] private LocalShiftClock _clock;
        [SerializeField] private Ferry _ferry;
        [SerializeField] private PlankButton _bell;

        [Tooltip("The signpost itself; hidden once the ferry leaves.")]
        [SerializeField] private GameObject _sign;

        [SerializeField, Min(0f)] private float _fadeSeconds = 1f;

        private enum Page { Title, Modes, Party, Boarding, Credits }
        private Page _page;
        private bool _started;

        private void Start()
        {
            for (int i = 0; i < _planks.Length; i++)
            {
                int index = i;
                _planks[i].Clicked.AddListener(() => OnPlank(index));
            }
            if (_bell != null) _bell.Clicked.AddListener(() => { if (_ferry != null && _ferry.MayRing) _ferry.RingBell(); });
            if (_session != null) _session.Changed += Refresh;
            if (_ferry != null) _ferry.StateChanged += OnFerry;
            Show(Page.Title);
        }

        private void OnDestroy()
        {
            if (_session != null) _session.Changed -= Refresh;
            if (_ferry != null) _ferry.StateChanged -= OnFerry;
        }

        // ---------------------------------------------------------------- pages

        private (string label, bool enabled, Action action)[] Planks(Page page)
        {
            switch (page)
            {
                case Page.Title:
                    return new (string, bool, Action)[]
                    {
                        ("PLAY", true, () => Show(Page.Modes)),
                        ("SETTINGS", false, null),
                        ("CREDITS", true, () => Show(Page.Credits)),
                        ("EXIT", true, Quit),
                    };
                case Page.Modes:
                    return new (string, bool, Action)[]
                    {
                        ("STORY", true, () => Choose(GameMode.Story)),
                        ("CREATIVE", true, () => Choose(GameMode.Creative)),
                        ("SURVIVAL", GameRules.For(GameMode.Survival).Available, () => Choose(GameMode.Survival)),
                        ("BACK", true, () => Show(Page.Title)),
                    };
                case Page.Party:
                    return new (string, bool, Action)[]
                    {
                        ("PLAY SOLO", true, () => Begin(null)),
                        ("HOST", _session != null, () => Begin(() => _session.Host())),
                        ("JOIN", _session != null, () => Begin(() => _session.Join())),
                        ("BACK", true, () => Show(Page.Modes)),
                    };
                case Page.Credits:
                    return new (string, bool, Action)[] { ("BACK", true, () => Show(Page.Title)) };
                default:
                    return new (string, bool, Action)[0];
            }
        }

        private void Show(Page page)
        {
            _page = page;
            var planks = Planks(page);
            for (int i = 0; i < _planks.Length; i++)
            {
                bool used = i < planks.Length;
                _planks[i].gameObject.SetActive(used);
                if (!used) continue;
                _planks[i].Text = planks[i].label;
                _planks[i].Interactable = planks[i].enabled;
            }
            Refresh();
        }

        private void OnPlank(int index)
        {
            var planks = Planks(_page);
            if (index < planks.Length && planks[index].enabled) planks[index].action?.Invoke();
        }

        private void Choose(GameMode mode)
        {
            GameModes.Select(mode);
            Show(Page.Party);
        }

        /// <summary>Lifts the night to the shift's afternoon behind a fade, then goes aboard (hosting or joining first if asked).</summary>
        private void Begin(Action connect)
        {
            if (_started) return;
            _started = true;
            StartCoroutine(BeginRoutine(connect));
        }

        private IEnumerator BeginRoutine(Action connect)
        {
            var fade = OVRScreenFade.instance;
            if (fade != null)
            {
                fade.fadeTime = _fadeSeconds;
                fade.FadeOut();
                yield return new WaitForSeconds(_fadeSeconds);
            }

            if (_clock != null) _clock.StartShift();
            connect?.Invoke();
            Show(Page.Boarding);

            if (fade != null) fade.FadeIn();
        }

        // ---------------------------------------------------------------- notes

        private void Refresh()
        {
            if (_note != null) _note.text = NoteText();
            if (_bell != null)
            {
                bool docked = _ferry != null && _ferry.Ride.State == FerryState.Docked;
                _bell.gameObject.SetActive(_started && docked);
                _bell.Interactable = _ferry != null && _ferry.MayRing;
                _bell.Text = _ferry != null && _ferry.MayRing ? "RING THE BELL" : "WAITING FOR HOST";
            }
        }

        private string NoteText()
        {
            switch (_page)
            {
                case Page.Title: return "";
                case Page.Modes: return "Story: Night 1, the first shift.\nCreative: build freely.";
                case Page.Party: return $"{GameModes.DisplayName(GameModes.Selected)}. Alone, or with up to three friends?";
                case Page.Credits: return "CAMP ASHWOOD\nA Campfire Friends VR game.";
            }

            // Boarding.
            if (_session == null || _session.State == NetworkSessionState.Offline)
                return !string.IsNullOrEmpty(_session != null ? _session.LastError : null)
                    ? $"{_session.LastError}\nBoard the ferry to go alone."
                    : "Board the ferry and ring the bell.";

            string players = _session.PlayerCount == 1 ? "1 player" : $"{_session.PlayerCount} players";
            switch (_session.State)
            {
                case NetworkSessionState.Starting: return "Connecting…";
                case NetworkSessionState.Hosting: return $"Hosting · {players}\nRing the bell when everyone's aboard.";
                default: return $"Joined · {players}\nBoard the ferry. The host rings the bell.";
            }
        }

        private void Update()
        {
            // Player count changes arrive through the session; the bell's permission can change on connect.
            if (_page == Page.Boarding && Time.frameCount % 30 == 0) Refresh();
        }

        private void OnFerry(FerryState state)
        {
            if (state != FerryState.Docked && _sign != null) _sign.SetActive(false);
            Refresh();
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
