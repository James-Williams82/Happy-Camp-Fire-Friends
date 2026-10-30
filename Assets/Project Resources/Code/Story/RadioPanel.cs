using System.Collections.Generic;
using CampfireFriends.Menu;
using TMPro;
using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>
    /// The walkie on the player's wrist: the owner's lines appear as text with a squelch, one at a
    /// time, each up long enough to read. LOG pages back through everything said tonight, so a
    /// line missed while busy is never lost. Reports appear as planks when there's something to
    /// report (the knocking). Nothing shows until the crew picks up the walkies in the office.
    /// </summary>
    public class RadioPanel : StoryListener
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private PlankButton _log;
        [SerializeField] private PlankButton _report;
        [SerializeField] private AudioSource _speaker;
        [SerializeField] private GameObject _screen;

        [Tooltip("Bixby's recordings. Lines without one are text only.")]
        [SerializeField] private RadioVoice _voice;

        private readonly Queue<int> _queue = new Queue<int>();
        private float _showUntil;
        private int _logIndex = -1;
        private AudioClip _squelch;

        private void Awake()
        {
            _squelch = StorySounds.Squelch(7);
            if (_speaker != null) RadioBand(_speaker.gameObject);
            if (_log != null) _log.Clicked.AddListener(PageLog);
            if (_report != null) _report.Clicked.AddListener(() => Story?.Raise(Night01Flag.KnocksReported));
        }

        private void OnDestroy()
        {
            if (_squelch != null) Destroy(_squelch);
        }

        private IStoryAuthority _listening;

        protected override void OnBound()
        {
            if (_listening != null) _listening.LineSpoken -= OnLine;
            _listening = Story;
            _listening.LineSpoken += OnLine;
            RefreshReport();
        }

        protected override void OnDisable()
        {
            if (_listening != null) _listening.LineSpoken -= OnLine;
            _listening = null;
            base.OnDisable();
        }

        protected override void OnFlag(Night01Flag flag, bool live) => RefreshReport();

        private void OnLine(int line, bool live)
        {
            if (live) _queue.Enqueue(line);
        }

        private void Update()
        {
            bool carried = Stories.Running && Story != null && Story.State.Has(Night01Flag.RadioCheckIn);
            if (_screen != null) _screen.SetActive(carried);
            if (_log != null) _log.gameObject.SetActive(carried);
            if (!carried || _text == null) return;

            if (Time.time >= _showUntil && _queue.Count > 0)
            {
                int line = _queue.Dequeue();
                _logIndex = -1;
                _text.text = Format(line);
                _showUntil = Time.time + (_voice != null ? _voice.Seconds(line) : Night01Lines.ReadingSeconds(line));
                if (_speaker != null)
                {
                    _speaker.PlayOneShot(_squelch);
                    var clip = _voice != null ? _voice.Clip(line) : null;
                    if (clip != null) { _speaker.clip = clip; _speaker.PlayDelayed(0.15f); }
                }
            }
            else if (Time.time >= _showUntil && _logIndex < 0)
            {
                _text.text = "";
            }
        }

        /// <summary>A walkie's sound: no lows, no highs, a little crunch.</summary>
        private static void RadioBand(GameObject speaker)
        {
            Ensure<AudioHighPassFilter>(speaker).cutoffFrequency = 350f;
            Ensure<AudioLowPassFilter>(speaker).cutoffFrequency = 3400f;
            Ensure<AudioDistortionFilter>(speaker).distortionLevel = 0.18f;
        }

        private static T Ensure<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

        /// <summary>Steps back one line through tonight's log; past the oldest, back to live.</summary>
        private void PageLog()
        {
            var log = Story?.Log;
            if (log == null || log.Count == 0 || _text == null) return;

            _logIndex = _logIndex < 0 ? log.Count - 1 : _logIndex - 1;
            if (_logIndex < 0) { _text.text = ""; return; }
            _text.text = $"<size=70%>LOG {_logIndex + 1}/{log.Count}</size>\n{Format(log[_logIndex])}";
            _showUntil = Time.time + 8f;
        }

        private void RefreshReport()
        {
            if (_report == null || Story == null) return;
            bool show = Story.State.Has(Night01Flag.CabinKnocksPlayed) && !Story.State.Has(Night01Flag.KnocksReported);
            _report.gameObject.SetActive(show);
            _report.Text = "REPORT KNOCKING";
        }

        private static string Format(int line) =>
            Night01Lines.Text.TryGetValue(line, out var text) ? $"<b>{Night01Lines.Speaker}:</b> {text}" : "";
    }
}
