namespace CampfireFriends.Game
{
    /// <summary>How a session is played. The host picks it; everyone in the session plays by the host's.</summary>
    public enum GameMode
    {
        /// <summary>Build and play freely: every piece is free, nothing is at stake.</summary>
        Creative,

        /// <summary>Survive the nights with what you gather (not built yet).</summary>
        Survival,

        /// <summary>The security-guard story, night by night, starting with Night 1. Materials cost.</summary>
        Story,
    }

    /// <summary>What a game mode allows. Systems ask the rules rather than checking the mode, so a new mode is one entry here.</summary>
    public readonly struct GameRules
    {
        /// <summary>The mode can be chosen yet.</summary>
        public readonly bool Available;

        /// <summary>Building costs no materials, and removing a piece refunds nothing.</summary>
        public readonly bool FreeBuilding;

        private GameRules(bool available, bool freeBuilding)
        {
            Available = available;
            FreeBuilding = freeBuilding;
        }

        public static GameRules For(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Creative: return new GameRules(available: true, freeBuilding: true);
                case GameMode.Survival: return new GameRules(available: false, freeBuilding: false);
                case GameMode.Story: return new GameRules(available: true, freeBuilding: false);
                default: return new GameRules(available: false, freeBuilding: false);
            }
        }
    }
}
