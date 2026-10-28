using System;
using CampfireFriends.Building;
using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// The local build tool's controller. Each frame it reads one input frame into the tool state,
    /// targets the pointer, asks the model where the piece would go and whether it may, updates the
    /// views, and on a confirm press sends one request to the authority. It never changes the world
    /// itself: the authority does, and the change comes back like anyone else's.
    ///
    /// Composed entirely from swappable parts: an input source, an aim source, and passive views.
    /// </summary>
    public class BuildController : MonoBehaviour
    {
        [Header("Composition")]
        [SerializeField] private BuildInputSource _input;
        [SerializeField] private AimSource _aim;
        [SerializeField] private BuildPreviewView _preview;
        [SerializeField] private PointerView _pointer;

        [Tooltip("Drawn over the piece the pointer is on while deleting.")]
        [SerializeField] private Material _deleteHighlight;

        [Header("Tuning")]
        [SerializeField] private TargetingSettings _targeting = new TargetingSettings();
        [SerializeField] private SolverSettings _solver = new SolverSettings();

        private IConstructionBackend _backend;
        private BuildTargeter _targeter;
        private PlacementSolver _placementSolver;
        private PieceView _highlighted;

        public BuildToolState State { get; private set; } = new BuildToolState(0);

        /// <summary>The pose being previewed this frame, if any.</summary>
        public PlacementProposal Proposal { get; private set; }

        /// <summary>Why the current preview or delete target is refused, or None.</summary>
        public PlacementRejection Reason { get; private set; }

        /// <summary>A request was sent to the authority.</summary>
        public event Action Requested;

        /// <summary>A confirm press was refused, locally or by the authority. Hook haptics and audio here.</summary>
        public event Action<PlacementRejection> Refused;

        private void Awake() => _targeter = new BuildTargeter(_targeting);

        private void OnDisable()
        {
            Bind(null);
            HideViews();
        }

        private void Update()
        {
            var backend = ConstructionBackends.Current;
            if (backend != _backend) Bind(backend);
            if (_backend == null)
            {
                HideViews();
                return;
            }

            var input = _input != null ? _input.Poll() : BuildInputFrame.Empty;
            ApplyCommands(input);

            if (_aim == null || !_aim.TryGetRay(out var ray))
            {
                HideViews();
                return;
            }

            // The pointer shows whenever there is aim, so the player can always see where they point.
            var target = _targeter.Target(ray);
            if (_pointer != null) _pointer.Show(ray, target.PointerEnd);

            if (State.Mode == BuildMode.None) HideToolViews();
            else if (State.Mode == BuildMode.Place) UpdatePlacing(target, input.Confirm);
            else UpdateDeleting(target, input.Confirm);
        }

        private void ApplyCommands(BuildInputFrame input)
        {
            if (input.Cancel) State.Cancel();
            if (input.Slot >= 0) State.Select(input.Slot);
            if (input.Cycle != 0) State.Cycle(input.Cycle);
            if (input.ToggleDelete) State.ToggleDelete();
            if (input.CycleMode) State.CycleMode();
            if (input.Rotate) State.Rotate();
        }

        private void UpdatePlacing(BuildTarget target, bool confirm)
        {
            SetHighlight(null);

            var definition = _backend.Catalog.Get(State.SelectedIndex);
            Proposal = definition != null
                ? _placementSolver.Solve(definition.Footprint, State.Rotation, target.Aim)
                : PlacementProposal.None;

            if (!Proposal.HasPose)
            {
                Reason = definition == null ? PlacementRejection.UnknownPiece : PlacementRejection.NoTarget;
                if (_preview != null) _preview.Hide();
            }
            else
            {
                Reason = _backend.Validator.ValidatePlacement(definition.Id, Proposal.Min, Proposal.Rotation, _backend.Stock);
                if (_preview != null) _preview.Show(definition, _backend.Grid, Proposal.Bounds, Proposal.Rotation, Reason == PlacementRejection.None);
            }

            if (!confirm) return;
            if (Reason != PlacementRejection.None)
            {
                Refuse(Reason, definition != null ? definition.Id : "nothing");
                return;
            }

            _backend.RequestPlace(definition.Id, Proposal.Min, Proposal.Rotation);
            Requested?.Invoke();
        }

        private void UpdateDeleting(BuildTarget target, bool confirm)
        {
            if (_preview != null) _preview.Hide();
            Proposal = PlacementProposal.None;

            var piece = target.Piece;
            SetHighlight(piece);
            Reason = piece != null ? _backend.Validator.ValidateRemoval(piece.PlacedId) : PlacementRejection.NoTarget;

            if (!confirm || piece == null) return;
            if (Reason != PlacementRejection.None)
            {
                Refuse(Reason, piece.name);
                return;
            }

            _backend.RequestRemove(piece.PlacedId);
            Requested?.Invoke();
        }

        private void Bind(IConstructionBackend backend)
        {
            if (_backend != null) _backend.RequestRejected -= OnAuthorityRejected;
            _backend = backend;
            if (_backend == null) return;

            _backend.RequestRejected += OnAuthorityRejected;
            State = new BuildToolState(_backend.Catalog.Count);
            _placementSolver = new PlacementSolver(_backend.Grid, _backend.World, _solver);
        }

        private void OnAuthorityRejected(PlacementRejection reason) => Refuse(reason, "request");

        private void Refuse(PlacementRejection reason, string subject)
        {
            Debug.Log($"[Build] {State.Mode} {subject} refused: {reason}", this);
            Refused?.Invoke(reason);
        }

        private void SetHighlight(PieceView piece)
        {
            if (piece == _highlighted) return;
            if (_highlighted != null) _highlighted.ClearMaterialOverride();
            _highlighted = piece;
            if (_highlighted != null && _deleteHighlight != null) _highlighted.OverrideMaterial(_deleteHighlight);
        }

        /// <summary>Picks a palette piece and starts placing it. The entry point for menus.</summary>
        public void SelectPiece(int index) => State.Select(index);

        private void HideViews()
        {
            HideToolViews();
            if (_pointer != null) _pointer.Hide();
        }

        private void HideToolViews()
        {
            SetHighlight(null);
            Proposal = PlacementProposal.None;
            if (_preview != null) _preview.Hide();
        }
    }
}
