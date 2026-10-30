using System;
using CampfireFriends.Building;
using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// The local build tool's controller. Each frame it reads one input frame into the tool state,
    /// targets the pointer, asks the model where a piece would go and whether it may, updates the
    /// views, and on a confirm press sends one request to the authority. It never changes the world
    /// itself: the authority does, and the change comes back like anyone else's.
    ///
    /// Modes (see <see cref="BuildMode"/>): in build mode with nothing in hand the pointer outlines
    /// placed pieces and confirm selects one; a selected piece offers rotate, reposition and delete
    /// (<see cref="RotateSelected"/>, <see cref="RepositionSelected"/>, <see cref="DeleteSelected"/>),
    /// which menus call. Placing needs a piece picked from the menu first. With build mode off,
    /// the pointer outlines usable pieces within reach and confirm uses them (switches a light).
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

        [Tooltip("Outlines the placed piece under the pointer, showing what confirm would select or use.")]
        [SerializeField] private BoundsOutlineView _hoverOutline;

        [Tooltip("Outlines the selected piece while its modify menu is open or it is being moved.")]
        [SerializeField] private BoundsOutlineView _selectionOutline;

        [Header("Tuning")]
        [SerializeField] private TargetingSettings _targeting = new TargetingSettings();
        [SerializeField] private SolverSettings _solver = new SolverSettings();

        [Tooltip("Metres the outlines stand off a piece's surface, so they never sink into it.")]
        [SerializeField, Min(0f)] private float _outlineMargin = 0.01f;

        private IConstructionBackend _backend;
        private BuildTargeter _targeter;
        private PlacementSolver _placementSolver;
        private BuildMode _reportedMode;
        private int _reportedTarget;
        private PlacementRejection _selectionRefusal;

        public BuildToolState State { get; private set; } = new BuildToolState(0);

        public BuildMode Mode => State.Mode;

        /// <summary>The ghost pose being previewed this frame, if any.</summary>
        public PlacementProposal Proposal { get; private set; }

        /// <summary>Why the current ghost or last selection action is refused, or None.</summary>
        public PlacementRejection Reason { get; private set; }

        /// <summary>A request was sent to the authority.</summary>
        public event Action Requested;

        /// <summary>A confirm or menu action was refused, locally or by the authority. Hook haptics and audio here.</summary>
        public event Action<PlacementRejection> Refused;

        /// <summary>Build mode was entered, left or changed. Menus follow this.</summary>
        public event Action<BuildMode> ModeChanged;

        /// <summary>A palette piece was picked to place (its catalog index).</summary>
        public event Action<int> PiecePicked;

        /// <summary>A placed piece was selected and its modify menu opened (its placed id).</summary>
        public event Action<int> PieceSelected;

        /// <summary>A placed piece was used with build mode off (its placed id).</summary>
        public event Action<int> PieceUsed;

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
            DropVanishedSelection();
            ReportMode();

            if (!State.IsBuilding)
            {
                UpdateUsing(input.Confirm);
                return;
            }

            ShowSelectionOutline();

            if (_aim == null || !_aim.TryGetRay(out var ray))
            {
                HideAimViews();
                return;
            }

            var target = _targeter.Target(ray, State.Mode == BuildMode.Move ? State.TargetId : BuildWorld.NoPiece);
            if (_pointer != null) _pointer.Show(ray, target.PointerEnd);

            switch (State.Mode)
            {
                case BuildMode.Browse:
                case BuildMode.Modify:
                    UpdateSelecting(target, input.Confirm);
                    break;
                case BuildMode.Place:
                    UpdatePlacing(target, input.Confirm);
                    break;
                case BuildMode.Move:
                    UpdateMoving(target, input.Confirm);
                    break;
            }
        }

        // ---------------------------------------------------------------- commands for menus

        /// <summary>Picks a palette piece and starts placing it.</summary>
        public void SelectPiece(int index)
        {
            Pick(index);
            ReportMode();
        }

        private void Pick(int index)
        {
            State.Select(index);
            if (State.Mode == BuildMode.Place && State.SelectedIndex == index) PiecePicked?.Invoke(index);
        }

        /// <summary>Turns the selected piece a quarter turn about Y, in place.</summary>
        public void RotateSelected()
        {
            if (State.Mode != BuildMode.Modify || !TryGetSelected(out var piece, out var definition)) return;

            var rotation = piece.Rotation.Rotated(1);
            var min = piece.Bounds.MinForSizeAboutCentre(rotation.RotateSize(definition.Footprint));
            if (!Allowed(_backend.Validator.ValidateMove(piece.Id, min, rotation), piece.PieceId)) return;

            _backend.RequestMove(piece.Id, min, rotation);
            Requested?.Invoke();
        }

        /// <summary>Starts repositioning the selected piece: its ghost follows the pointer until confirmed.</summary>
        public void RepositionSelected()
        {
            if (State.Mode != BuildMode.Modify || !TryGetSelected(out var piece, out _)) return;

            State.BeginMove(piece.Rotation);
            ReportMode();
        }

        /// <summary>Removes the selected piece and closes its menu.</summary>
        public void DeleteSelected()
        {
            if (State.Mode != BuildMode.Modify || !TryGetSelected(out var piece, out _)) return;
            if (!Allowed(_backend.Validator.ValidateRemoval(piece.Id), piece.PieceId)) return;

            _backend.RequestRemove(piece.Id);
            Requested?.Invoke();
            State.Deselect();
            ReportMode();
        }

        /// <summary>Closes the selected piece's menu without changing it.</summary>
        public void CloseSelection()
        {
            if (State.Mode != BuildMode.Modify && State.Mode != BuildMode.Move) return;

            State.Deselect();
            ReportMode();
        }

        /// <summary>The selected piece's box in world space, for placing its menu.</summary>
        public bool TryGetSelectedBounds(out Bounds bounds)
        {
            bounds = default;
            if (!TryGetSelected(out var piece, out _)) return false;

            bounds = _backend.Grid.ToWorldBounds(piece.Bounds);
            return true;
        }

        // ---------------------------------------------------------------- per mode

        private void ApplyCommands(BuildInputFrame input)
        {
            if (input.ToggleBuild) State.ToggleBuild();

            // Out of build mode the tool can't be manipulated: only the toggle gets back in.
            if (!State.IsBuilding) return;

            if (input.Back) State.Back();
            if (input.Slot >= 0) Pick(input.Slot);
            if (input.Cycle != 0) State.Cycle(input.Cycle);

            if (input.Rotate)
            {
                if (State.Mode == BuildMode.Modify) RotateSelected();
                else State.Rotate();
            }
        }

        /// <summary>Nothing in hand: outline what the pointer is on; confirm selects it, or dismisses the current selection.</summary>
        private void UpdateSelecting(BuildTarget target, bool confirm)
        {
            HideGhost();

            var hovered = target.Piece != null && target.Piece.PlacedId != State.TargetId ? target.Piece : null;
            ShowOutline(_hoverOutline, hovered != null ? hovered.PlacedId : BuildWorld.NoPiece, true);

            if (!confirm) return;
            if (hovered != null)
            {
                State.Inspect(hovered.PlacedId);
                PieceSelected?.Invoke(hovered.PlacedId);
            }
            else if (target.Piece == null)
            {
                State.Deselect();
            }
        }

        private void UpdatePlacing(BuildTarget target, bool confirm)
        {
            if (_hoverOutline != null) _hoverOutline.Hide();

            var definition = _backend.Catalog.Get(State.SelectedIndex);
            if (definition == null)
            {
                HideGhost();
                Reason = PlacementRejection.UnknownPiece;
                return;
            }

            Proposal = _placementSolver.Solve(definition.Footprint, State.Rotation, target.Aim);
            Reason = Proposal.HasPose
                ? _backend.Validator.ValidatePlacement(definition.Id, Proposal.Min, Proposal.Rotation, _backend.FreeBuilding ? null : _backend.Stock)
                : PlacementRejection.NoTarget;
            ShowGhost(definition);

            if (!confirm || !Allowed(Reason, definition.Id)) return;

            // The piece stays in hand so the next one can go straight down; Back returns to the menu.
            _backend.RequestPlace(definition.Id, Proposal.Min, Proposal.Rotation);
            Requested?.Invoke();
        }

        private void UpdateMoving(BuildTarget target, bool confirm)
        {
            if (_hoverOutline != null) _hoverOutline.Hide();
            if (!TryGetSelected(out var piece, out var definition))
            {
                HideGhost();
                return;
            }

            Proposal = _placementSolver.Solve(definition.Footprint, State.Rotation, target.Aim);
            Reason = Proposal.HasPose
                ? _backend.Validator.ValidateMove(piece.Id, Proposal.Min, Proposal.Rotation)
                : PlacementRejection.NoTarget;
            ShowGhost(definition);

            if (!confirm || !Allowed(Reason, piece.PieceId)) return;

            _backend.RequestMove(piece.Id, Proposal.Min, Proposal.Rotation);
            Requested?.Invoke();
            State.Deselect();
            ReportMode();
        }

        /// <summary>Build mode off: outline the usable piece under the pointer if it's within reach; confirm uses it.</summary>
        private void UpdateUsing(bool confirm)
        {
            HideGhost();
            if (_selectionOutline != null) _selectionOutline.Hide();

            int usable = BuildWorld.NoPiece;
            if (_aim != null && _aim.TryGetRay(out var ray))
            {
                // The pointer stays out of build mode too, for aiming at things to use.
                var target = _targeter.Target(ray);
                if (_pointer != null) _pointer.Show(ray, target.PointerEnd);
                if (target.Piece != null && CanUse(target.Piece.PlacedId, ray.origin)) usable = target.Piece.PlacedId;
            }
            else if (_pointer != null)
            {
                _pointer.Hide();
            }
            ShowOutline(_hoverOutline, usable, true);

            if (!confirm || usable == BuildWorld.NoPiece) return;

            _backend.RequestUse(usable);
            Requested?.Invoke();
            PieceUsed?.Invoke(usable);
        }

        /// <summary>Usable at all, and the hand is within the piece's use range of its nearest surface.</summary>
        private bool CanUse(int placedId, Vector3 hand)
        {
            if (_backend.Validator.ValidateUse(placedId) != PlacementRejection.None) return false;

            _backend.World.TryGet(placedId, out var piece);
            _backend.Validator.TryGetDefinition(piece.PieceId, out var definition);
            var bounds = _backend.Grid.ToWorldBounds(piece.Bounds);
            return (bounds.ClosestPoint(hand) - hand).sqrMagnitude <= definition.UseRange * definition.UseRange;
        }

        // ---------------------------------------------------------------- helpers

        private bool TryGetSelected(out PlacedPiece piece, out PieceDefinition definition)
        {
            piece = null;
            definition = null;
            return _backend != null
                   && State.TargetId != BuildWorld.NoPiece
                   && _backend.World.TryGet(State.TargetId, out piece)
                   && _backend.Validator.TryGetDefinition(piece.PieceId, out definition);
        }

        /// <summary>Someone else may delete or move-and-delete the selected piece; then there's nothing to modify.</summary>
        private void DropVanishedSelection()
        {
            if ((State.Mode == BuildMode.Modify || State.Mode == BuildMode.Move) && !_backend.World.Contains(State.TargetId))
                State.Deselect();
        }

        /// <summary>Clears stale refusals when the mode or selected piece changes, and tells menus about mode changes.</summary>
        private void ReportMode()
        {
            if (State.TargetId != _reportedTarget)
            {
                _reportedTarget = State.TargetId;
                _selectionRefusal = PlacementRejection.None;
            }

            if (State.Mode == _reportedMode) return;

            _reportedMode = State.Mode;
            _selectionRefusal = PlacementRejection.None;
            Reason = PlacementRejection.None;
            ModeChanged?.Invoke(_reportedMode);
        }

        private bool Allowed(PlacementRejection reason, string subject)
        {
            if (reason == PlacementRejection.None) return true;
            Refuse(reason, subject);
            return false;
        }

        private void Bind(IConstructionBackend backend)
        {
            if (_backend != null) _backend.RequestRejected -= OnAuthorityRejected;
            _backend = backend;
            if (_backend == null) return;

            _backend.RequestRejected += OnAuthorityRejected;
            State = new BuildToolState(_backend.Catalog.Count);
            _placementSolver = new PlacementSolver(_backend.Grid, _backend.World, _solver);
            ReportMode();
        }

        private void OnAuthorityRejected(PlacementRejection reason) => Refuse(reason, "request");

        private void Refuse(PlacementRejection reason, string subject)
        {
            Debug.Log($"[Build] {State.Mode} {subject} refused: {reason}", this);
            if (State.Mode == BuildMode.Modify) _selectionRefusal = reason;
            Refused?.Invoke(reason);
        }

        private void ShowGhost(PieceDefinition definition)
        {
            if (_preview == null) return;
            if (Proposal.HasPose) _preview.Show(definition, _backend.Grid, Proposal.Bounds, Proposal.Rotation, Reason == PlacementRejection.None);
            else _preview.Hide();
        }

        private void HideGhost()
        {
            Proposal = PlacementProposal.None;
            if (_preview != null) _preview.Hide();
        }

        private void ShowSelectionOutline()
        {
            bool selected = State.Mode == BuildMode.Modify || State.Mode == BuildMode.Move;
            ShowOutline(_selectionOutline, selected ? State.TargetId : BuildWorld.NoPiece, _selectionRefusal == PlacementRejection.None);
        }

        private void ShowOutline(BoundsOutlineView outline, int placedId, bool valid)
        {
            if (outline == null) return;
            if (placedId == BuildWorld.NoPiece || !_backend.World.TryGet(placedId, out var piece))
            {
                outline.Hide();
                return;
            }

            var bounds = _backend.Grid.ToWorldBounds(piece.Bounds);
            bounds.Expand(_outlineMargin * 2f);
            outline.Show(bounds, valid);
        }

        private void HideAimViews()
        {
            HideGhost();
            if (_hoverOutline != null) _hoverOutline.Hide();
            if (_pointer != null) _pointer.Hide();
        }

        private void HideViews()
        {
            HideAimViews();
            if (_selectionOutline != null) _selectionOutline.Hide();
        }
    }
}
