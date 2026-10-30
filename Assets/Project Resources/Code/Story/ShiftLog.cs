using CampfireFriends.Menu;
using TMPro;
using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>
    /// The shift log clipboard: once everyone's back, the UNEXPLAINED ACTIVITY line is filled in by
    /// choosing structural noise, possible trespass or unknown. One report for the whole team.
    /// </summary>
    public class ShiftLog : StoryListener
    {
        [SerializeField] private PlankButton _structural;
        [SerializeField] private PlankButton _trespass;
        [SerializeField] private PlankButton _unknown;
        [SerializeField] private TMP_Text _entry;

        private void Awake()
        {
            Wire(_structural, "STRUCTURAL NOISE", Night01Flag.ReportStructural);
            Wire(_trespass, "POSSIBLE TRESPASS", Night01Flag.ReportTrespass);
            Wire(_unknown, "UNKNOWN", Night01Flag.ReportUnknown);
        }

        private void Wire(PlankButton plank, string text, Night01Flag flag)
        {
            if (plank == null) return;
            plank.Text = text;
            plank.Clicked.AddListener(() => { if (Story != null && Story.Sequence == Night01Sequence.ShiftLog) Story.Raise(flag); });
        }

        private void Update()
        {
            bool open = Story != null && Stories.Running && Story.State.Has(Night01Flag.BackAtOffice) && Story.State.Has(Night01Flag.CabinImpactPlayed);
            bool filed = Story != null && (Story.State.Flags & (long)(Night01Flag.ReportStructural | Night01Flag.ReportTrespass | Night01Flag.ReportUnknown)) != 0;
            foreach (var plank in new[] { _structural, _trespass, _unknown })
                if (plank != null) plank.gameObject.SetActive(open && !filed);
        }

        protected override void OnFlag(Night01Flag flag, bool live)
        {
            if (_entry == null) return;
            if (flag == Night01Flag.ReportStructural) _entry.text = "UNEXPLAINED ACTIVITY: Structural noise";
            else if (flag == Night01Flag.ReportTrespass) _entry.text = "UNEXPLAINED ACTIVITY: Possible trespass";
            else if (flag == Night01Flag.ReportUnknown) _entry.text = "UNEXPLAINED ACTIVITY: Unknown";
        }
    }
}
