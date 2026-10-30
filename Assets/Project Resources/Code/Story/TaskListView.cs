using System.Text;
using TMPro;
using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>The checklist beside the pulled-up map: what's left to do in Night 1's current sequence.</summary>
    public class TaskListView : StoryListener
    {
        [SerializeField] private TMP_Text _text;

        private Night01Sequence _drawn = (Night01Sequence)(-1);
        private long _drawnFlags = -1;

        protected override void OnFlag(Night01Flag flag, bool live) { }

        private void Update()
        {
            if (_text == null) return;
            if (Story == null || !Stories.Running)
            {
                _text.text = "<b>TONIGHT</b>\n\nNo shift running.";
                _drawnFlags = -1;
                return;
            }
            if (Story.Sequence == _drawn && Story.State.Flags == _drawnFlags) return;
            _drawn = Story.Sequence;
            _drawnFlags = Story.State.Flags;

            var sb = new StringBuilder($"<b>TONIGHT</b>\n<size=75%>{Night01Tasks.Title(_drawn)}</size>\n\n");
            foreach (var task in Night01Tasks.For(Story.State, _drawn))
                sb.Append(task.Done ? $"<color=#8a8a7a><s>[x] {task.Text}</s></color>\n" : $"[  ] {task.Text}\n");
            _text.text = sb.ToString();
        }
    }
}
