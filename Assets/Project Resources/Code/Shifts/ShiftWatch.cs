using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CampfireFriends.Shifts
{
    /// <summary>
    /// The guard's wrist watch: night number, camp time and the shift phase, from whichever clock is
    /// in charge. Its one button asks for the next phase, for testing a whole shift quickly.
    /// </summary>
    public class ShiftWatch : MonoBehaviour
    {
        [SerializeField] private TMP_Text _night;
        [SerializeField] private TMP_Text _time;
        [SerializeField] private TMP_Text _phase;

        [SerializeField] private Button _skip;
        [SerializeField] private TMP_Text _skipLabel;

        private int _shownMinute = -1;
        private ShiftPhase _shownPhase;
        private int _shownNight;

        private void OnEnable()
        {
            if (_skip != null) _skip.onClick.AddListener(Skip);
            _shownMinute = -1;
        }

        private void OnDisable()
        {
            if (_skip != null) _skip.onClick.RemoveListener(Skip);
        }

        private void Skip() => ShiftClocks.Current?.RequestSkip();

        private void Update()
        {
            var clock = ShiftClocks.Current?.Clock;
            if (clock == null) return;

            int minute = Mathf.FloorToInt(clock.Hours * 60f) % (24 * 60);
            if (minute == _shownMinute && clock.Phase == _shownPhase && clock.Night == _shownNight) return;

            _shownMinute = minute;
            _shownPhase = clock.Phase;
            _shownNight = clock.Night;

            if (_night != null) _night.text = $"Night {clock.Night}";
            if (_time != null) _time.text = ClockText(minute);
            if (_phase != null) _phase.text = PhaseName(clock.Phase);
            if (_skipLabel != null) _skipLabel.text = $"Skip to {PhaseName(clock.NextPhase)}";
        }

        /// <summary>"9:40 PM".</summary>
        public static string ClockText(int minuteOfDay)
        {
            int hour = minuteOfDay / 60, minute = minuteOfDay % 60;
            int twelve = hour % 12 == 0 ? 12 : hour % 12;
            return $"{twelve}:{minute:00} {(hour < 12 ? "AM" : "PM")}";
        }

        public static string PhaseName(ShiftPhase phase)
        {
            switch (phase)
            {
                case ShiftPhase.Prep: return "Prep";
                case ShiftPhase.Dusk: return "Dusk";
                case ShiftPhase.NightWatch: return "Night watch";
                case ShiftPhase.Morning: return "Morning";
                default: return phase.ToString();
            }
        }
    }
}
