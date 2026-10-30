using UnityEngine;

namespace CampfireFriends.Shifts
{
    /// <summary>
    /// The shift clock offline. It runs only while it's the clock in charge; once a session's clock
    /// takes over (which starts from this one's time) it waits.
    /// </summary>
    public class LocalShiftClock : MonoBehaviour, IShiftClockSource
    {
        [SerializeField] private ShiftSettings _settings;

        [Header("Testing")]
        [Tooltip("The phase play starts in.")]
        [SerializeField] private ShiftPhase _startIn = ShiftPhase.Prep;

        [Tooltip("Shift speed. 1 is the schedule's own pace.")]
        [SerializeField, Min(0f)] private float _speed = 1f;

        [Tooltip("Hold the clock (e.g. at night, behind the main menu) until StartShift is called.")]
        [SerializeField] private bool _holdUntilStarted;

        [Tooltip("Where a new shift begins when StartShift is called: the crew arrives after dark.")]
        [SerializeField] private ShiftPhase _shiftStartsIn = ShiftPhase.Dusk;

        public ShiftClock Clock { get; private set; }
        public ShiftSettings Settings => _settings;
        public bool Held { get; set; }

        private void Awake()
        {
            var schedule = _settings != null ? _settings.CreateSchedule() : ShiftSchedule.Condensed();
            Clock = new ShiftClock(schedule);
            for (int i = 0; i < schedule.Count && Clock.Phase != _startIn; i++) Clock.SkipPhase();
        }

        private void OnEnable() => ShiftClocks.Register(this);
        private void OnDisable() => ShiftClocks.Unregister(this);

        private void Update()
        {
            if (!_holdUntilStarted && !Held && ReferenceEquals(ShiftClocks.Current, this)) Clock.Advance(Time.deltaTime * _speed);
        }

        /// <summary>Night 1 begins: the clock goes to the start of the shift and runs.</summary>
        public void StartShift()
        {
            _holdUntilStarted = false;
            var schedule = Clock.Schedule;
            int start = 0;
            while (start < schedule.Count && schedule[start].Phase != _shiftStartsIn) start++;
            Clock.Sync(1, start < schedule.Count ? schedule.StartOf(start) : 0f);
        }

        public void RequestSkip() => Clock.SkipPhase();
    }
}
