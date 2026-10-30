using System;
using System.Collections.Generic;
using System.Linq;
using CampfireFriends.Construction;
using CampfireFriends.Game;
using Fusion;
using GameMode = Fusion.GameMode;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CampfireFriends.Network
{
    public enum NetworkSessionState
    {
        Offline,
        Starting,
        Hosting,
        Joined,
    }

    /// <summary>
    /// Hosts or joins a Photon Fusion session (Host mode) for the scene it's in, which keeps running
    /// rather than reloading. Once connected, the scene's offline construction is retired so the
    /// host's networked construction takes over, and every player gets an avatar. Leaving, or the
    /// host leaving, reloads the scene offline. Menus call <see cref="Host"/>, <see cref="Join"/>
    /// and <see cref="Leave"/> and show <see cref="State"/>.
    /// </summary>
    public class NetworkSession : MonoBehaviour, INetworkRunnerCallbacks
    {
        private const string ModeProperty = "mode";

        [Tooltip("Everyone testing together uses the same name.")]
        [SerializeField] private string _sessionName = "CampfireTest";

        [SerializeField, Range(1, 8)] private int _maxPlayers = 4;

        [SerializeField] private NetworkObject _avatarPrefab;
        [SerializeField] private AvatarPoseSource _poses;

        [Tooltip("The scene's offline construction; removed once a session starts.")]
        [SerializeField] private LocalConstructionBackend _offlineBackend;

        private readonly Dictionary<PlayerRef, NetworkObject> _avatars = new Dictionary<PlayerRef, NetworkObject>();
        private NetworkRunner _runner;
        private bool _leaving;

        public NetworkSessionState State { get; private set; } = NetworkSessionState.Offline;
        public string SessionName => _sessionName;
        public int PlayerCount => _runner != null && _runner.IsRunning ? _runner.ActivePlayers.Count() : 0;

        /// <summary>Why the last attempt failed or the session ended, or empty.</summary>
        public string LastError { get; private set; } = "";

        /// <summary>State, player count or error changed.</summary>
        public event Action Changed;

        public void Host() => Begin(GameMode.Host);
        public void Join() => Begin(GameMode.Client);

        public void Leave()
        {
            if (_runner == null) return;
            _leaving = true;
            _runner.Shutdown();
        }

        private async void Begin(GameMode mode)
        {
            if (State != NetworkSessionState.Offline) return;

            int buildIndex = SceneManager.GetActiveScene().buildIndex;
            if (buildIndex < 0)
            {
                Fail("This scene isn't in the build settings, so it can't be shared.");
                return;
            }

            LastError = "";
            SetState(NetworkSessionState.Starting);

            var runnerObject = new GameObject("Network Runner");
            _runner = runnerObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);

            var scene = new NetworkSceneInfo();
            scene.AddSceneRef(SceneRef.FromIndex(buildIndex), LoadSceneMode.Additive);

            var result = await _runner.StartGame(new StartGameArgs
            {
                GameMode = mode,
                SessionName = _sessionName,
                PlayerCount = _maxPlayers,
                Scene = scene,
                SceneManager = runnerObject.AddComponent<NetworkSceneManagerDefault>(),
                ObjectProvider = runnerObject.AddComponent<NetworkObjectProviderDefault>(),

                // The host's game mode travels with the session, so joiners play by it.
                SessionProperties = mode == GameMode.Host
                    ? new Dictionary<string, SessionProperty> { [ModeProperty] = (int)GameModes.Selected }
                    : null,
            });

            if (this == null) return;
            if (!result.Ok)
            {
                if (runnerObject != null) Destroy(runnerObject);
                _runner = null;
                Fail(Explain(mode, result.ShutdownReason));
                return;
            }

            if (mode == GameMode.Client) FollowHostMode();

            // Networked construction replaces the offline one; its pieces go with it.
            if (_offlineBackend != null) Destroy(_offlineBackend.gameObject);
            SetState(mode == GameMode.Host ? NetworkSessionState.Hosting : NetworkSessionState.Joined);
        }

        private void FollowHostMode()
        {
            var properties = _runner.SessionInfo != null ? _runner.SessionInfo.Properties : null;
            if (properties != null && properties.TryGetValue(ModeProperty, out var value) && value.IsInt)
                GameModes.Follow((Game.GameMode)(int)value);
        }

        private string Explain(GameMode mode, ShutdownReason reason)
        {
            switch (reason)
            {
                case ShutdownReason.GameNotFound: return $"No session called \"{_sessionName}\" yet. Ask someone to Host first.";
                case ShutdownReason.GameIdAlreadyExists: return $"\"{_sessionName}\" is already being hosted. Use Join.";
                case ShutdownReason.GameIsFull: return $"\"{_sessionName}\" is full ({_maxPlayers} players).";
                default: return $"Couldn't {(mode == GameMode.Host ? "host" : "join")}: {reason}.";
            }
        }

        private void Fail(string reason)
        {
            LastError = reason;
            Debug.LogWarning($"[Session] {reason}", this);
            SetState(NetworkSessionState.Offline);
        }

        private void SetState(NetworkSessionState state)
        {
            State = state;
            Changed?.Invoke();
        }

        // ---------------------------------------------------------------- runner callbacks

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer && _avatarPrefab != null)
                _avatars[player] = runner.Spawn(_avatarPrefab, Vector3.zero, Quaternion.identity, player);
            Changed?.Invoke();
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (_avatars.TryGetValue(player, out var avatar))
            {
                _avatars.Remove(player);
                if (avatar != null) runner.Despawn(avatar);
            }
            Changed?.Invoke();
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            if (_poses != null) input.Set(_poses.Sample());
        }

        /// <summary>The session ended: we left, the host left, or the connection dropped. Start over offline.</summary>
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            if (State == NetworkSessionState.Starting) return; // a failed start is reported by Begin

            Debug.Log($"[Session] Ended: {(_leaving ? "left" : shutdownReason.ToString())}. Reloading offline.", this);
            GameModes.Restore();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    }
}
