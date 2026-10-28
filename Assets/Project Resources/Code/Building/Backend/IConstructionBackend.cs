using System;
using System.Collections.Generic;
using CampfireFriends.Building;
using CampfireFriends.Supplies;
using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// The read side of a construction session: what a local preview needs to solve and validate
    /// exactly as the authority will. On a client this is a replicated mirror.
    /// </summary>
    public interface IConstructionModel
    {
        PieceCatalog Catalog { get; }
        BuildGrid Grid { get; }
        BuildWorld World { get; }
        PlacementValidator Validator { get; }
        IStockView Stock { get; }
    }

    /// <summary>The write side: requests the authority accepts or refuses. Nothing changes locally until it does.</summary>
    public interface IConstructionCommands
    {
        void RequestPlace(string pieceId, Vector3Int min, GridRotation rotation);
        void RequestRemove(int placedId);

        /// <summary>Raised on the requesting machine when the authority refuses a request.</summary>
        event Action<PlacementRejection> RequestRejected;
    }

    public interface IConstructionBackend : IConstructionModel, IConstructionCommands
    {
    }

    /// <summary>
    /// Where the build tool finds the active session. A networked session registers over an offline
    /// one, so the same tool works in the sandbox and in a hosted game.
    /// </summary>
    public static class ConstructionBackends
    {
        private static readonly List<IConstructionBackend> Active = new List<IConstructionBackend>();

        public static IConstructionBackend Current => Active.Count > 0 ? Active[Active.Count - 1] : null;

        public static void Register(IConstructionBackend backend)
        {
            if (backend != null && !Active.Contains(backend)) Active.Add(backend);
        }

        public static void Unregister(IConstructionBackend backend) => Active.Remove(backend);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForDomainReloadFreePlayMode() => Active.Clear();
    }
}
