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
    private int playerCount;
    private float lastRestartTime = -10f;
    private const float RestartCooldown = 2f;

    public int HostPlayerId => hostPlayerId;
    public int ClientPlayerId => clientPlayerId;
    public int PlayerCount => playerCount;
    public bool IsRemotePlayerConnected => playerCount >= 2;

    public event System.Action OnRemotePlayerConnected;
    public event System.Action OnRemotePlayerDisconnected;

    public override void Awake()
    {
        // 중복 인스턴스 방지: 이미 singleton이 있으면 파괴
        if (singleton != null && singleton != this)
        {
            Debug.Log("[DinoNetworkManager] Duplicate detected. Destroying self.");
            Destroy(gameObject);
            return;
        }

        base.Awake();
        if (featureFlags == null)
            featureFlags = Resources.Load<FeatureFlags>("FeatureFlags");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        nextPlayerId = 1;
        playerCount = 0;
        gameStarted = false;
        NetworkServer.RegisterHandler<JoinGameMessage>(OnServerJoinGame);
        NetworkServer.RegisterHandler<LaunchInputMessage>(OnServerLaunchInput);
        NetworkServer.RegisterHandler<RestartRequestMessage>(OnServerRestartRequest);
        Debug.Log("[DinoNetworkManager] Server started.");
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        playerCount = NetworkServer.connections.Count;
        Debug.Log($"[DinoNetworkManager] Player connected. Total: {playerCount}");

        // Host(Player 1)는 항상 첫 연결. Player 2(원격)가 연결되면 알림.
        if (playerCount >= 2)
        {
            OnRemotePlayerConnected?.Invoke();
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        playerCount = NetworkServer.connections.Count;
        Debug.Log($"[DinoNetworkManager] Player disconnected. Total: {playerCount}");

        if (playerCount < 2)
        {
            OnRemotePlayerDisconnected?.Invoke();
        }
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
        NetworkClient.RegisterHandler<MapSelectMessage>(OnClientMapSelect);
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

        // 재시작 쿨다운: 너무 빠른 재시작 요청 방지
        if (Time.time - lastRestartTime < RestartCooldown)
        {
            Debug.LogWarning("[DinoNetworkManager] Restart request ignored (cooldown).");
            return;
        }
        lastRestartTime = Time.time;

        gameStarted = false;
        GameSessionController session = FindFirstObjectByType<GameSessionController>();
        if (session != null)
        {
            session.RestartGame();
            // RestartGame → BeginGame → NotifyGameStarted()에서 gameStarted = true로 설정됨
        }
        RestartConfirmedMessage confirmMsg = new RestartConfirmedMessage { restartApproved = true };
        NetworkServer.SendToAll(confirmMsg);
    }

    private void OnClientJoinAccepted(JoinAcceptedMessage msg)
    {
        GameLaunchContext.SetNetworkClientInfo(msg.assignedPlayerId);
        Debug.Log($"[DinoNetworkManager] Client received PlayerId: {msg.assignedPlayerId}");

        // 클라이언트(원격)만 MapSelect로 이동. 호스트는 이미 MapSelect에 있음
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "02_MapSelect")
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("02_MapSelect");
        }
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
        if (featureFlags != null && !featureFlags.enableLanMultiplayer)
        {
            Debug.LogWarning("[DinoNetworkManager] LAN multiplayer disabled by FeatureFlags.");
            return;
        }
        GameLaunchContext.SetMode(GameMode.NetworkHost);
        GameLaunchContext.ServerIP = "0.0.0.0";
        networkAddress = "localhost";
        StartHost();

        // 서버 시작 직후 상태 로깅
        if (NetworkServer.active)
        {
            Debug.Log($"[DinoNetworkManager] Host started on port 7777 (UDP). LAN IP 확인 후 클라이언트에서 입력하세요.");
        }
        else
        {
            Debug.LogError("[DinoNetworkManager] Host FAILED to start! Port 7777 may be in use by another process.");
        }
    }

    public void StartNetworkClient(string ip)
    {
        if (featureFlags != null && !featureFlags.enableLanMultiplayer)
        {
            Debug.LogWarning("[DinoNetworkManager] LAN multiplayer disabled by FeatureFlags.");
            return;
        }
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

    private void OnClientMapSelect(MapSelectMessage msg)
    {
        GameLaunchContext.SelectMap((MapId)msg.mapId);
        Debug.Log($"[DinoNetworkManager] Client received MapSelect: {msg.mapId}");
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.LogError($"[DinoNetworkManager] Client disconnected. Server at '{networkAddress}:7777' may be unreachable or connection rejected.");
        Debug.LogError($"[DinoNetworkManager] Check: (1) Host PC 방화벽에서 포트 7777(UDP) 허용 (2) 올바른 IP 주소 입력 (3) Host가 먼저 실행 중인지 확인");

        var mmc = FindFirstObjectByType<DinoAlkkagi.Presentation.MainMenuController>();
        if (mmc != null)
        {
            mmc.ShowConnectionStatus($"연결 실패: {networkAddress}:7777\n방화벽 및 IP를 확인하세요.");
        }
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
