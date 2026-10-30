using UnityEngine;

namespace CampfireFriends.Locomotion
{
    /// <summary>
    /// How long a guard can sprint: drains while sprinting, refills after a short rest. Run it
    /// dry and it has to refill a way before sprinting is allowed again.
    /// </summary>
    public sealed class Stamina
    {
        public float Seconds { get; }
        public float RefillSeconds { get; }
        public float RestBeforeRefill { get; }

        /// <summary>Sprint left, 0..1.</summary>
        public float Fraction { get; private set; } = 1f;

        /// <summary>Ran dry, and not yet refilled far enough to sprint again.</summary>
        public bool Winded { get; private set; }

        private const float RecoverTo = 0.3f;
        private float _rested;

        public Stamina(float seconds = 6f, float refillSeconds = 8f, float restBeforeRefill = 1f)
        {
            Seconds = Mathf.Max(0.1f, seconds);
            RefillSeconds = Mathf.Max(0.1f, refillSeconds);
            RestBeforeRefill = Mathf.Max(0f, restBeforeRefill);
        }

        /// <summary>Runs the meter forward; true while actually sprinting.</summary>
        public bool Tick(bool wantsToSprint, float deltaTime)
        {
            if (wantsToSprint && !Winded && Fraction > 0f)
            {
                _rested = 0f;
                Fraction = Mathf.Max(0f, Fraction - deltaTime / Seconds);
                if (Fraction <= 0f) Winded = true;
                return true;
            }

            _rested += deltaTime;
            if (_rested >= RestBeforeRefill) Fraction = Mathf.Min(1f, Fraction + deltaTime / RefillSeconds);
            if (Winded && Fraction >= RecoverTo) Winded = false;
            return false;
        }
    }
}
