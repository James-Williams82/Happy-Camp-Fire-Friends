using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>
    /// Base for anything in the world that reacts to Night 1's flags. It follows whichever story
    /// is in charge; on binding it applies flags already raised without their sounds, so joining
    /// late, or the session taking over, shows the world as it is rather than replaying it.
    /// </summary>
    public abstract class StoryListener : MonoBehaviour
    {
        protected IStoryAuthority Story { get; private set; }

        protected virtual void OnEnable()
        {
            Stories.Changed += Rebind;
            Rebind();
        }

        protected virtual void OnDisable()
        {
            Stories.Changed -= Rebind;
            Bind(null);
        }

        private void Rebind() => Bind(Stories.Current);

        private void Bind(IStoryAuthority story)
        {
            if (ReferenceEquals(story, Story)) return;
            if (Story != null) Story.State.Raised -= OnFlag;

            Story = story;
            if (Story == null) return;

            Story.State.Raised += OnFlag;
            for (int bit = 0; bit < 64; bit++)
            {
                var flag = (Night01Flag)(1L << bit);
                if (Story.State.Has(flag) && flag != Night01Flag.None) OnFlag(flag, false);
            }
            OnBound();
        }

        /// <summary>A flag is up. <paramref name="live"/> is false when it happened before we were here.</summary>
        protected abstract void OnFlag(Night01Flag flag, bool live);

        protected virtual void OnBound() { }
    }
}
