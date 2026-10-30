using System;
using UnityEngine;

namespace CampfireFriends.Game
{
    /// <summary>
    /// The game mode this player chose, and the one in effect: their own offline or when hosting,
    /// the host's while in someone else's session. Kept across scene reloads, so leaving a session
    /// comes back to the player's own choice.
    /// </summary>
    public static class GameModes
    {
        public static GameMode Selected { get; private set; } = GameMode.Creative;
        public static GameMode Active { get; private set; } = GameMode.Creative;

        public static GameRules Rules => GameRules.For(Active);

        /// <summary>The active mode changed.</summary>
        public static event Action Changed;

        /// <summary>The player picks a mode (offline only). Unavailable modes are ignored.</summary>
        public static void Select(GameMode mode)
        {
            if (!GameRules.For(mode).Available) return;
            Selected = mode;
            SetActive(mode);
        }

        /// <summary>Joined someone's session: play by their mode.</summary>
        public static void Follow(GameMode hostMode) => SetActive(hostMode);

        /// <summary>Out of a session again: back to the player's own choice.</summary>
        public static void Restore() => SetActive(Selected);

        private static void SetActive(GameMode mode)
        {
            if (mode == Active) return;
            Active = mode;
            Changed?.Invoke();
        }

        public static string DisplayName(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Creative: return "Creative";
                case GameMode.Survival: return "Survival";
                case GameMode.Story: return "Story";
                default: return mode.ToString();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForDomainReloadFreePlayMode()
        {
            Selected = GameMode.Creative;
            Active = GameMode.Creative;
            Changed = null;
        }
    }
}
