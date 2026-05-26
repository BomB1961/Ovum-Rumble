using UnityEngine;
using Mirror;
using DinoAlkkagi.Core;
using DinoAlkkagi.Data;
using DinoAlkkagi.Presentation;
using UnityEngine.SceneManagement;

public class DinoNetworkManager : NetworkManager
{
    [Header("Dino Settings")]
    [SerializeField] private FeatureFlags featureFlags;
    [SerializeField] private int hostPlayerId = 1;
    [SerializeField] private int clientPlayerId = 2;

    private int nextPlayerId = 1;
    private bool gameStarted;

    public int HostPlayerId => hostPlayerId;
    public int ClientPlayerId => clientPlayerId;

    public override void Awake()
    {
        base.Awake();
        featureFlags ??= FindFirstObjectByType<FeatureFlags>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        nextPlayerId = 1;
        gameStarted = false;
        NetworkServer.RegisterHandler<JoinGameMessage>(OnServerJoinGame);
        NetworkServer.RegisterHandler<LaunchInputMessage>(OnServerLaunchInput);
        NetworkServer.RegisterHandler<RestartRequestMessage>(OnServerRestartRequest);
        Debug.Log("[DinoNetworkManager] Server started.");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        NetworkClient.RegisterHandler<JoinAcceptedMessage>(OnClientJoinAccepted);
        NetworkClient.RegisterHandler<StateSnapshotMessage>(OnClientStateSnapshot);
        NetworkClient.RegisterHandler<TurnChangeMessage>(OnClientTurnChange);
        NetworkClient.RegisterHandler<GameResultMessage>(OnClientGameResult);
        NetworkClient.RegisterHandler<RestartConfirmedMessage>(OnClientRestartConfirmed);
        NetworkClient.RegisterHandler<LoadSceneMessage>(OnClientLoadScene);
        Debug.Log("[DinoNetworkManager] Client started.");
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        JoinGameMessage msg = new JoinGameMessage { playerName = "Player" };
        NetworkClient.Send(msg);
        Debug.Log("[DinoNetworkManager] Client connected. Sent JoinGameMessage.");
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (nextPlayerId > 2)
        {
            Debug.LogWarning("[DinoNetworkManager] Maximum players reached. Rejecting connection.");
            conn.Disconnect();
            return;
        }
        base.OnServerAddPlayer(conn);
    }

    private void OnServerJoinGame(NetworkConnectionToClient conn, JoinGameMessage msg)
    {
        int playerId = nextPlayerId;
        nextPlayerId++;

        JoinAcceptedMessage acceptMsg = new JoinAcceptedMessage { assignedPlayerId = playerId };
        conn.Send(acceptMsg);
        Debug.Log($"[DinoNetworkManager] Assigned PlayerId {playerId} to connection {conn.connectionId}");
    }

    private void OnServerLaunchInput(NetworkConnectionToClient conn, LaunchInputMessage msg)
    {
        if (!gameStarted) return;
        GameSessionController session = FindFirstObjectByType<GameSessionController>();
        if (session != null)
        {
            session.OnRemoteLaunch(msg.eggNetId, msg.direction, msg.force);
            Debug.Log($"[DinoNetworkManager] Remote launch: eggNetId={msg.eggNetId}, force={msg.force}");
        }
    }

    private void OnServerRestartRequest(NetworkConnectionToClient conn, RestartRequestMessage msg)
    {
        if (!gameStarted) return;
        gameStarted = false;
        GameSessionController session = FindFirstObjectByType<GameSessionController>();
        if (session != null)
        {
            session.RestartGame();
        }
        gameStarted = true;
        RestartConfirmedMessage confirmMsg = new RestartConfirmedMessage { restartApproved = true };
        NetworkServer.SendToAll(confirmMsg);
    }

    private void OnClientJoinAccepted(JoinAcceptedMessage msg)
    {
        GameLaunchContext.SetNetworkClientInfo(msg.assignedPlayerId);
        Debug.Log($"[DinoNetworkManager] Client received PlayerId: {msg.assignedPlayerId}");
    }

    private void OnClientStateSnapshot(StateSnapshotMessage msg)
    {
        NetworkGameStateSync receiver = FindFirstObjectByType<NetworkGameStateSync>();
        if (receiver != null)
        {
            receiver.ApplySnapshot(msg);
        }
    }

    private void OnClientTurnChange(TurnChangeMessage msg)
    {
        GameEvents.TriggerTurnStarted(msg.playerId);
    }

    private void OnClientGameResult(GameResultMessage msg)
    {
        GameResult result = (GameResult)msg.result;
        GameEvents.TriggerGameEnded(result);
    }

    private void OnClientRestartConfirmed(RestartConfirmedMessage msg)
    {
        if (msg.restartApproved)
        {
            GameSessionController session = FindFirstObjectByType<GameSessionController>();
            if (session != null)
            {
                session.RestartGame();
            }
        }
    }

    public void StartNetworkHost()
    {
        GameLaunchContext.SetMode(GameMode.NetworkHost);
        GameLaunchContext.ServerIP = "0.0.0.0";
        networkAddress = "localhost";
        StartHost();
    }

    public void StartNetworkClient(string ip)
    {
        GameLaunchContext.SetMode(GameMode.NetworkClient);
        GameLaunchContext.ServerIP = ip;
        networkAddress = ip;
        StartClient();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (gameStarted)
        {
            GameEvents.TriggerGameEnded(GameResult.None);
        }
        gameStarted = false;
        GameLaunchContext.ResetToDefault();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        GameLaunchContext.ResetToDefault();
    }

    private void OnClientLoadScene(LoadSceneMessage msg)
    {
        Debug.Log($"[DinoNetworkManager] Client received LoadScene: {msg.sceneName}");
        UnityEngine.SceneManagement.SceneManager.LoadScene(msg.sceneName);
    }

    public void NotifyGameStarted()
    {
        gameStarted = true;
    }
}
