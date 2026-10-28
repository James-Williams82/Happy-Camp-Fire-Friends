using Oculus.Interaction;
using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// Aims along an Interaction SDK ray interactor's ray, so the build pointer uses the rig's pointer
    /// pose. Yields while the interactor is disabled (for example by locomotion) or busy with a real
    /// interactable such as a UI panel, so building never steals those selects.
    /// </summary>
    public class RayInteractorAim : AimSource
    {
        [SerializeField] private RayInteractor _rayInteractor;

        public override bool TryGetRay(out Ray ray)
        {
            ray = default;
            if (_rayInteractor == null || !_rayInteractor.isActiveAndEnabled) return false;
            if (_rayInteractor.State == InteractorState.Disabled) return false;
            if (_rayInteractor.HasCandidate || _rayInteractor.HasInteractable) return false;

            ray = _rayInteractor.Ray;
            return ray.direction != Vector3.zero;
        }
    }
}
