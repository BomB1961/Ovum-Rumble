using UnityEngine;
using Mirror;
using DinoAlkkagi.Core;
using DinoAlkkagi.Data;
using DinoAlkkagi.Presentation;
using UnityEngine.SceneManagement;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.IO;

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
    private bool disconnectIntended;

    [Header("VPS Relay")]
    [SerializeField] private string vpsRelayAddress = "45.59.101.155";
    [SerializeField] private int vpsRelayPort = 7777;
    private string roomCode;
    private Thread relayThread;
    private volatile bool relayRunning;
    private TcpClient vpsRelayClient;

    public int HostPlayerId => hostPlayerId;
    public int ClientPlayerId => clientPlayerId;
    public int PlayerCount => playerCount;
    public bool IsRemotePlayerConnected => playerCount >= 2;
    public string RoomCode => roomCode;

    public event System.Action OnRemotePlayerConnected;
    public event System.Action OnRemotePlayerDisconnected;
    public event System.Action<string> OnRoomCreated;

    public override void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Debug.Log("[DinoNetworkManager] Duplicate detected. Destroying self.");
            Destroy(gameObject);
            return;
        }
        base.Awake();
        if (featureFlags == null)
            featureFlags = Resources.Load<FeatureFlags>("FeatureFlags");
        Application.quitting += () => disconnectIntended = true;
    }

    void OnDestroy() { StopRelay(); }

    // ─── VPS TCP 제어 ───────────────────────────────────────

    string TcpCommand(string command)
    {
        try
        {
            using (var tcp = new TcpClient())
            {
                tcp.Connect(vpsRelayAddress, vpsRelayPort + 1);
                tcp.ReceiveTimeout = 5000;
                var stream = tcp.GetStream();
                byte[] req = Encoding.UTF8.GetBytes(command + "\n");
                stream.Write(req, 0, req.Length);
                byte[] buf = new byte[256];
                int len = stream.Read(buf, 0, buf.Length);
                return Encoding.UTF8.GetString(buf, 0, len).Trim();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DinoNetworkManager] VPS command failed ({command}): {ex.Message}");
            return null;
        }
    }

    // ─── 호스트 시작 (VPS 릴레이) ───────────────────────────

    public void StartNetworkHost()
    {
        try
        {
            if (featureFlags != null && !featureFlags.enableLanMultiplayer)
            {
                Debug.LogWarning("[DinoNetworkManager] LAN multiplayer disabled by FeatureFlags.");
                return;
            }

            // 1. VPS에 방 생성
            string resp = TcpCommand("CREATE_ROOM");
            if (resp == null || !resp.StartsWith("CODE:"))
            {
                Debug.LogError("[DinoNetworkManager] Failed to create room on VPS.");
                return;
            }
            roomCode = resp.Substring(5).Trim();
            Debug.Log($"[DinoNetworkManager] Room created: {roomCode}");

            // 2. 로컬 Mirror 서버 시작 (TelepathyTransport, 127.0.0.1:7777)
            GameLaunchContext.SetMode(GameMode.NetworkHost);
            GameLaunchContext.ServerIP = vpsRelayAddress;
            networkAddress = "127.0.0.1";
            StartServer();

            if (!NetworkServer.active)
            {
                Debug.LogError("[DinoNetworkManager] Server failed to start.");
                return;
            }

            // 3. VPS TCP 릴레이 시작 (VPS↔로컬서버 브릿징)
            StartHostRelay();

            // 4. 로컬 클라이언트 접속 (호스트 플레이어)
            networkAddress = "127.0.0.1";
            StartClient();

            // 5. 방 코드 알림
            OnRoomCreated?.Invoke(roomCode);
            Debug.Log($"[DinoNetworkManager] Host ready. Room: {roomCode}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DinoNetworkManager] Host start failed: {ex.Message}");
        }
    }

    void StartHostRelay()
    {
        relayRunning = true;
        relayThread = new Thread(() =>
        {
            try
            {
                vpsRelayClient = new TcpClient();
                vpsRelayClient.Connect(vpsRelayAddress, vpsRelayPort);
                var vpsStream = vpsRelayClient.GetStream();

                // 방 코드로 호스트 식별
                byte[] ident = Encoding.UTF8.GetBytes($"HOST:{roomCode}\n");
                vpsStream.Write(ident, 0, ident.Length);
                vpsStream.Flush();

                // 로컬 Mirror 서버에 연결 (Virtual Client 역할)
                var localClient = new TcpClient();
                localClient.Connect("127.0.0.1", 7777);
                var localStream = localClient.GetStream();

                Debug.Log("[DinoNetworkManager] VPS relay bridged.");

                // 양방향 포워딩
                var t1 = new Thread(() => Forward(localStream, vpsStream)) { IsBackground = true };
                var t2 = new Thread(() => Forward(vpsStream, localStream)) { IsBackground = true };
                t1.Start(); t2.Start();
                t1.Join(); t2.Join();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DinoNetworkManager] Host relay error: {ex.Message}");
            }
            finally { relayRunning = false; }
        })
        { IsBackground = true };
        relayThread.Start();
    }

    // ─── 클라이언트 시작 (VPS 릴레이) ───────────────────────

    public void StartNetworkClient(string code)
    {
        try
        {
            if (featureFlags != null && !featureFlags.enableLanMultiplayer)
            {
                Debug.LogWarning("[DinoNetworkManager] LAN multiplayer disabled by FeatureFlags.");
                return;
            }

            // 1. VPS에 방 참여 요청
            string resp = TcpCommand($"JOIN:{code}");
            if (resp == null || !resp.StartsWith("OK"))
            {
                Debug.LogError($"[DinoNetworkManager] Failed to join room {code}.");
                var mmc = FindFirstObjectByType<DinoAlkkagi.Presentation.MainMenuController>();
                mmc?.ShowConnectionStatus($"방 {code}를 찾을 수 없습니다.");
                return;
            }

            roomCode = code;
            GameLaunchContext.SetMode(GameMode.NetworkClient);
            GameLaunchContext.ServerIP = vpsRelayAddress;

            // 2. 클라이언트 릴레이 시작 (로컬에서 Mirror 접속 대기)
            StartClientRelay();

            // 3. 로컬 릴레이로 Mirror 접속
            networkAddress = "127.0.0.1";
            StartClient();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DinoNetworkManager] Client start failed: {ex.Message}");
        }
    }

    void StartClientRelay()
    {
        relayRunning = true;
        relayThread = new Thread(() =>
        {
            try
            {
                // VPS에 먼저 연결 + 방 코드 전송
                vpsRelayClient = new TcpClient();
                vpsRelayClient.Connect(vpsRelayAddress, vpsRelayPort);
                var vpsStream = vpsRelayClient.GetStream();

                byte[] ident = Encoding.UTF8.GetBytes($"CLNT:{roomCode}\n");
                vpsStream.Write(ident, 0, ident.Length);
                vpsStream.Flush();

                // 로컬에서 Mirror 접속 대기
                var listener = new TcpListener(System.Net.IPAddress.Loopback, 7777);
                listener.Start();
                var mirrorConn = listener.AcceptTcpClient();
                listener.Stop();
                var mirrorStream = mirrorConn.GetStream();

                Debug.Log("[DinoNetworkManager] Client relay bridged.");

                var t1 = new Thread(() => Forward(mirrorStream, vpsStream)) { IsBackground = true };
                var t2 = new Thread(() => Forward(vpsStream, mirrorStream)) { IsBackground = true };
                t1.Start(); t2.Start();
                t1.Join(); t2.Join();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DinoNetworkManager] Client relay error: {ex.Message}");
            }
            finally { relayRunning = false; }
        })
        { IsBackground = true };
        relayThread.Start();

        Thread.Sleep(100); // 릴레이 리스너 준비 대기
    }

    // ─── TCP 포워딩 ─────────────────────────────────────────

    static void Forward(Stream src, Stream dst)
    {
        try
        {
            byte[] buffer = new byte[65536];
            int bytesRead;
            while ((bytesRead = src.Read(buffer, 0, buffer.Length)) > 0)
            {
                dst.Write(buffer, 0, bytesRead);
                dst.Flush();
            }
        }
        catch { }
    }

    void StopRelay()
    {
        relayRunning = false;
        try { vpsRelayClient?.Close(); } catch { }
        vpsRelayClient = null;
        relayThread = null;
    }

    // ─── Mirror 네트워크 이벤트 ────────────────────────────

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

        if (playerCount >= 2)
            OnRemotePlayerConnected?.Invoke();
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        playerCount = NetworkServer.connections.Count;
        Debug.Log($"[DinoNetworkManager] Player disconnected. Total: {playerCount}");

        if (playerCount < 2)
            OnRemotePlayerDisconnected?.Invoke();
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

    void OnServerJoinGame(NetworkConnectionToClient conn, JoinGameMessage msg)
    {
        int playerId = nextPlayerId;
        nextPlayerId++;
        JoinAcceptedMessage acceptMsg = new JoinAcceptedMessage { assignedPlayerId = playerId };
        conn.Send(acceptMsg);
        Debug.Log($"[DinoNetworkManager] Assigned PlayerId {playerId} to connection {conn.connectionId}");
    }

    void OnServerLaunchInput(NetworkConnectionToClient conn, LaunchInputMessage msg)
    {
        if (!gameStarted) return;
        GameSessionController session = FindFirstObjectByType<GameSessionController>();
        if (session != null)
        {
            session.OnRemoteLaunch(msg.eggNetId, msg.direction, msg.force);
            Debug.Log($"[DinoNetworkManager] Remote launch: eggNetId={msg.eggNetId}, force={msg.force}");
        }
    }

    void OnServerRestartRequest(NetworkConnectionToClient conn, RestartRequestMessage msg)
    {
        if (!gameStarted) return;
        if (Time.time - lastRestartTime < RestartCooldown)
        {
            Debug.LogWarning("[DinoNetworkManager] Restart request ignored (cooldown).");
            return;
        }
        lastRestartTime = Time.time;
        gameStarted = false;
        ServerChangeScene("01_Game");
    }

    void OnClientJoinAccepted(JoinAcceptedMessage msg)
    {
        GameLaunchContext.SetNetworkClientInfo(msg.assignedPlayerId);
        if (!NetworkServer.active)
            GameLaunchContext.SetMode(GameMode.NetworkClient);
        Debug.Log($"[DinoNetworkManager] Client received PlayerId: {msg.assignedPlayerId}");

        var mmc = FindFirstObjectByType<DinoAlkkagi.Presentation.MainMenuController>();
        mmc?.ShowConnectionStatus($"P{msg.assignedPlayerId}로 접속됨! 호스트가 맵을 선택 중입니다...");
    }

    void OnClientStateSnapshot(StateSnapshotMessage msg)
    {
        var receiver = FindFirstObjectByType<NetworkGameStateSync>();
        receiver?.ApplySnapshot(msg);
    }

    void OnClientTurnChange(TurnChangeMessage msg) => GameEvents.TriggerTurnStarted(msg.playerId);

    void OnClientGameResult(GameResultMessage msg)
    {
        if (NetworkServer.active) return;
        GameEvents.TriggerGameEnded((GameResult)msg.result);
    }

    void OnClientRestartConfirmed(RestartConfirmedMessage msg) { }

    void OnClientMapSelect(MapSelectMessage msg)
    {
        GameLaunchContext.SelectMap((MapId)msg.mapId);
        Debug.Log($"[DinoNetworkManager] Client received MapSelect: {msg.mapId}");
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        if (disconnectIntended)
        {
            Debug.Log($"[DinoNetworkManager] Client disconnected.");
        }
        else
        {
            Debug.LogError("[DinoNetworkManager] VPS relay connection lost.");
            var mmc = FindFirstObjectByType<DinoAlkkagi.Presentation.MainMenuController>();
            mmc?.ShowConnectionStatus("VPS 릴레이 연결 실패");
        }
        disconnectIntended = false;
    }

    void OnClientLoadScene(LoadSceneMessage msg)
    {
        Debug.Log($"[DinoNetworkManager] Client received LoadScene: {msg.sceneName}");
        SceneManager.LoadScene(msg.sceneName);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (gameStarted)
            GameEvents.TriggerGameEnded(GameResult.None);
        gameStarted = false;
        GameLaunchContext.ResetToDefault();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        StopRelay();
        GameLaunchContext.ResetToDefault();
    }

    public void NotifyGameStarted() { gameStarted = true; }

    public void StopClientSafe() { disconnectIntended = true; StopClient(); }
    public void StopHostSafe() { disconnectIntended = true; StopHost(); }
}
