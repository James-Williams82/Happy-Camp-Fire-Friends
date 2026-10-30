using CampfireFriends.Building;
using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// The local player's build cues: entering and leaving build mode, picking a piece from the
    /// menu, selecting a placed piece, and refusals. Only the person building hears these.
    /// </summary>
    public class BuildCueSounds : MonoBehaviour
    {
        [SerializeField] private BuildController _controller;
        [SerializeField] private BuildSoundSet _sounds;

        [Tooltip("Plays the cues. Usually 2D (spatial blend 0), since they're feedback rather than world sound.")]
        [SerializeField] private AudioSource _source;

        private BuildMode _lastMode;

        private void OnEnable()
        {
            if (_controller == null) return;
            _lastMode = _controller.Mode;
            _controller.ModeChanged += OnModeChanged;
            _controller.PiecePicked += OnPiecePicked;
            _controller.PieceSelected += OnPieceSelected;
            _controller.Refused += OnRefused;
        }

        private void OnDisable()
        {
            if (_controller == null) return;
            _controller.ModeChanged -= OnModeChanged;
            _controller.PiecePicked -= OnPiecePicked;
            _controller.PieceSelected -= OnPieceSelected;
            _controller.Refused -= OnRefused;
        }

        private void OnModeChanged(BuildMode mode)
        {
            if (mode == BuildMode.None) Play(_sounds.ModeOff);
            else if (_lastMode == BuildMode.None) Play(_sounds.ModeOn);
            _lastMode = mode;
        }

        private void OnPiecePicked(int index) => Play(_sounds.MenuPick);

        private void OnPieceSelected(int placedId) => Play(_sounds.Select);

        private void OnRefused(PlacementRejection reason) => Play(_sounds.Refused);

        private void Play(AudioClip[] clips)
        {
            if (_sounds == null || _source == null) return;

            var clip = BuildSoundSet.PickFrom(clips);
            if (clip == null) return;

            _source.pitch = _sounds.RandomPitch();
            _source.PlayOneShot(clip);
        }
    }
}
