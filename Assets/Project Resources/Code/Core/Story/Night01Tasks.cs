using System.Collections.Generic;

namespace CampfireFriends.Story
{
    /// <summary>One step of tonight's work, as the guard's checklist shows it.</summary>
    public readonly struct Night01Task
    {
        public readonly string Text;
        public readonly bool Done;

        public Night01Task(string text, bool done)
        {
            Text = text;
            Done = done;
        }
    }

    /// <summary>
    /// The steps of Night 1's current sequence, ticked off from the story's flags: what the map's
    /// checklist shows. Optional extras (the notice, the wrong breaker, the basketball) are left off.
    /// </summary>
    public static class Night01Tasks
    {
        private static readonly Dictionary<Night01Sequence, (string text, Night01Flag done)[]> Steps =
            new Dictionary<Night01Sequence, (string, Night01Flag)[]>
            {
                [Night01Sequence.Arrival] = new[]
                {
                    ("Step off the ferry onto the dock", Night01Flag.DockReached),
                    ("Lift the chain off the dock gate", Night01Flag.GateOpened),
                    ("Find the security office (behind Cabin 6)", Night01Flag.OfficeVisited),
                },
                [Night01Sequence.Office] = new[]
                {
                    ("Take a walkie from the radio spot", Night01Flag.RadioCheckIn),
                    ("Take a flashlight from the locker", Night01Flag.FlashlightCollected),
                    ("Read the work orders on the desk", Night01Flag.WorkOrderViewed),
                    ("Study the map on the north wall", Night01Flag.MapInspected),
                },
                [Night01Sequence.Generator] = new[]
                {
                    ("Take the fuse from the shed cabinet", Night01Flag.FuseCollected),
                    ("Take the fuel can", Night01Flag.FuelCollected),
                    ("Insert the fuse in the breaker panel", Night01Flag.FuseInserted),
                    ("Pour the fuel into the generator", Night01Flag.GeneratorFueled),
                    ("Pull the generator's starter", Night01Flag.GeneratorStarted),
                    ("Switch on SECURITY at the breaker panel", Night01Flag.SecurityCircuitOn),
                },
                [Night01Sequence.Cabin6] = new[]
                {
                    ("Go to Cabin 6", Night01Flag.Cabin06Arrived),
                    ("Board up Cabin 6's doorway", Night01Flag.Cabin06Secured),
                },
                [Night01Sequence.Perimeter] = new[]
                {
                    ("Check the dining-hall latch", Night01Flag.DiningHallInspected),
                    ("Check the service-gate padlock by the court", Night01Flag.PerimeterInspected),
                },
                [Night01Sequence.Finale] = new[]
                {
                    ("Head back to the security office", Night01Flag.BackAtOffice),
                },
                [Night01Sequence.ShiftLog] = new[]
                {
                    ("Head back to the security office", Night01Flag.BackAtOffice),
                    ("File the shift log at the desk", Night01Flag.Completed),
                },
            };

        public static IReadOnlyList<Night01Task> For(StoryState state, Night01Sequence sequence)
        {
            var tasks = new List<Night01Task>();
            if (state == null || !Steps.TryGetValue(sequence, out var steps)) return tasks;
            foreach (var (text, done) in steps) tasks.Add(new Night01Task(text, state.Has(done)));
            return tasks;
        }

        /// <summary>The sequence's heading on the checklist.</summary>
        public static string Title(Night01Sequence sequence)
        {
            switch (sequence)
            {
                case Night01Sequence.Arrival: return "Arrival";
                case Night01Sequence.Office: return "Security office";
                case Night01Sequence.Generator: return "Power (maintenance shed)";
                case Night01Sequence.Cabin6: return "Cabin 6";
                case Night01Sequence.Perimeter: return "Perimeter walk";
                case Night01Sequence.Finale: return "Back to the office";
                case Night01Sequence.ShiftLog: return "Shift log";
                default: return "Shift complete";
            }
        }
    }
}
