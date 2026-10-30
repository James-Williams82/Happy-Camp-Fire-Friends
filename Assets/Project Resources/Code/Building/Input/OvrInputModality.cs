namespace CampfireFriends.Construction
{
    /// <summary>Reads the active input modality from the Meta runtime: hands once hand tracking takes over.</summary>
    public class OvrInputModality : InputModalitySource
    {
        public override bool UsingHands => (OVRInput.GetActiveController() & OVRInput.Controller.Hands) != 0;
    }
}
