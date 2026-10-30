using UnityEngine;

namespace CampfireFriends.Shifts
{
    /// <summary>The shift's timing and the sun's path across it. Shared by the offline and networked clocks, so every player computes the same phases.</summary>
    [CreateAssetMenu(menuName = "Campfire Friends/Shift Settings", fileName = "ShiftSettings")]
    public class ShiftSettings : ScriptableObject
    {
        [Tooltip("The shift's phases in order, each with its length in real seconds and the camp hours it covers.")]
        public PhaseSpan[] Phases = ShiftSchedule.DefaultPhases();

        public SunPath Sun = SunPath.Default;

        public ShiftSchedule CreateSchedule() => new ShiftSchedule(Phases);
    }
}
