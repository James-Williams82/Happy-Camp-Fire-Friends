using System;

namespace CampfireFriends.Ambience
{
    /// <summary>
    /// When the next sound of a scattered layer (a distant bird, a cricket, a creak) should play.
    /// Waits a random time between the layer's bounds, stretched while the layer is barely present,
    /// so sounds thin out as their time of day passes instead of stopping dead.
    /// </summary>
    public sealed class ScatterTimer
    {
        private readonly Random _random;
        private readonly float _minInterval;
        private readonly float _maxInterval;
        private float _wait;

        public ScatterTimer(float minInterval, float maxInterval, int seed)
        {
            _minInterval = Math.Max(0.05f, Math.Min(minInterval, maxInterval));
            _maxInterval = Math.Max(_minInterval, maxInterval);
            _random = new Random(seed);
            _wait = NextWait();
        }

        /// <summary>Advances time; true when a sound should play now.</summary>
        /// <param name="presence">0–1: how much the layer belongs at this time of day. At 0 nothing plays.</param>
        public bool Tick(float deltaTime, float presence)
        {
            if (presence <= 0.001f) return false;

            // Low presence slows the clock rather than shortening the wait, so a layer fading out
            // plays its last sounds further and further apart.
            _wait -= deltaTime * Math.Min(1f, presence);
            if (_wait > 0f) return false;

            _wait = NextWait();
            return true;
        }

        /// <summary>A random 0–1 value for picking clips, positions and pitch.</summary>
        public float Next() => (float)_random.NextDouble();

        private float NextWait() => _minInterval + (_maxInterval - _minInterval) * (float)_random.NextDouble();
    }
}
