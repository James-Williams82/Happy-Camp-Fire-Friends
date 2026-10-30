using System.Text;
using TMPro;
using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>TONIGHT'S ASSIGNMENTS on the office board, ticked off as the work gets done.</summary>
    public class WorkOrderBoard : StoryListener
    {
        [SerializeField] private TMP_Text _text;

        private static readonly (string task, Night01Flag done)[] Orders =
        {
            ("Restore power to the security circuit (maintenance shed)", Night01Flag.SecurityCircuitOn),
            ("Secure Cabin 6's damaged door opening", Night01Flag.Cabin06Secured),
            ("Inspect the dining hall exterior", Night01Flag.DiningHallInspected),
            ("Check the perimeter and report entry signs", Night01Flag.PerimeterInspected),
            ("Return and complete the shift log", Night01Flag.Completed),
        };

        protected override void OnBound() => Draw();
        protected override void OnFlag(Night01Flag flag, bool live) => Draw();

        private void Draw()
        {
            if (_text == null || Story == null) return;
            var sb = new StringBuilder("<b>TONIGHT'S ASSIGNMENTS</b>\n<size=60%>TEMPORARY SECURITY</size>\n\n");
            foreach (var (task, done) in Orders)
                sb.Append(Story.State.Has(done) ? $"<s>[x] {task}</s>\n" : $"[  ] {task}\n");
            _text.text = sb.ToString();
        }
    }
}
