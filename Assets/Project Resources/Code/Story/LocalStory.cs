using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>
    /// Night 1 offline: this machine runs the director. A hosted session's story takes over from
    /// it (starting from its flags), after which this one waits.
    /// </summary>
    public class LocalStory : MonoBehaviour, IStoryAuthority
    {
        [Tooltip("Bixby's recordings; their lengths pace the radio. Optional.")]
        [SerializeField] private RadioVoice _voice;

        private readonly List<int> _log = new List<int>();
        private Night01Director _director;

        public StoryState State { get; } = new StoryState();
        public Night01Sequence Sequence => _director != null ? _director.Sequence : Night01Sequence.Arrival;
        public IReadOnlyList<int> Log => _log;

        public event Action<int, bool> LineSpoken;

        private void Awake()
        {
            _director = new Night01Director(State, _voice != null ? _voice.Seconds : (System.Func<int, float>)null);
            _director.Said += line =>
            {
                _log.Add(line);
                LineSpoken?.Invoke(line, true);
            };
            _director.Requested += Stories.Apply;
        }

        private void OnEnable() => Stories.Register(this);
        private void OnDisable() => Stories.Unregister(this);

        private void Update()
        {
            if (!ReferenceEquals(Stories.Current, this)) return;
            _director.Tick(Time.deltaTime);
            Stories.HoldDaylight(_director.Sequence);
        }

        public void Raise(Night01Flag flag)
        {
            if (Stories.Running && ReferenceEquals(Stories.Current, this)) State.Raise(flag);
        }
    }
}
