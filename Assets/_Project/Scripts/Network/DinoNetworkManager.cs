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

    // VPS 릴레??(?�독?? ?�스?�터 미노�?
    private const int VpsRelayPort = 7777;
    private const int ClientRelayPort = 17777; // 같�? PC ?�스????충돌 방�?
    private const byte _xorKey = 0xAB;
    private static byte[] _vpsAddr = { 0x9f, 0x9e, 0x85, 0x9e, 0x92, 0x85, 0x9a, 0x9b, 0x9a, 0x85, 0x9a, 0x9e, 0x9e };
    private static byte[] _vpsToken = { 0xd2, 0xda, 0x9a, 0xc1, 0xc4, 0x99, 0xdb, 0xcf, 0xc7, 0x99, 0xd2, 0xc2 };
    private static string VpsAddress => Deobfuscate(_vpsAddr);
    private static string VpsAuthToken => Deobfuscate(_vpsToken);

    private static string Deobfuscate(byte[] data)
    {
        char[] chars = new char[data.Length];
        for (int i = 0; i < data.Length; i++)
            chars[i] = (char)(data[i] ^ _xorKey);
        return new string(chars);
    }

    private string roomCode;
    private Thread relayThread;
    private volatile bool relayRunning;
    private TcpClient vpsRelayClient;
    private readonly ManualResetEventSlim relayReadySignal = new ManualResetEventSlim(false);

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

        // KcpTransport(UDP) ??TelepathyTransport(TCP) ?�동 변??
        // VPS TCP 릴레?��????�환?�을 ?�해 (kcp2k??UDP 기반?�라 릴레?��? 불일�?
        EnsureTelepathyTransport();
    }

    private void EnsureTelepathyTransport()
    {
        var cur = transport;
        if (cur == null) return;
        if (cur.GetType().Name != "KcpTransport") return;

        ushort port = 7777;
        if (cur is PortTransport pt)
            port = pt.Port;

        var telepathy = gameObject.AddComponent<TelepathyTransport>();
        telepathy.port = port;
        telepathy.NoDelay = true;
        transport = telepathy;
        DestroyImmediate(cur);

        Debug.Log($"[DinoNetworkManager] Transport swapped: KcpTransport ??TelepathyTransport (port {port})");
    }

    void OnDestroy() { StopRelay(); }

    // ?�?�?�?VPS TCP ?�어 ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?

    string TcpCommand(string command)
    {
        try
        {
            using (var tcp = new TcpClient())
            {
                tcp.Connect(VpsAddress, VpsRelayPort + 1);
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

    // ?�?�?�??�스???�작 (VPS 릴레?? ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?

    public void StartNetworkHost()
    {
        try
        {
            if (featureFlags != null && !featureFlags.enableLanMultiplayer)
            {
                Debug.LogWarning("[DinoNetworkManager] LAN multiplayer disabled by FeatureFlags.");
                return;
            }

            // 1. VPS??�??�성
            string resp = TcpCommand("CREATE_ROOM");
            if (resp == null || !resp.StartsWith("CODE:"))
            {
                Debug.LogError("[DinoNetworkManager] Failed to create room on VPS.");
                return;
            }
            roomCode = resp.Substring(5).Trim();
            Debug.Log($"[DinoNetworkManager] Room created: {roomCode}");

            // 2. 로컬 Mirror ?�버 ?�작 (TelepathyTransport, 127.0.0.1:7777)
            // ?�트??EnsureTelepathyTransport()?�서 ?��? 7777�??�정??
            GameLaunchContext.SetMode(GameMode.NetworkHost);
            GameLaunchContext.ServerIP = VpsAddress;
            networkAddress = "127.0.0.1";
            StartServer();

            if (!NetworkServer.active)
            {
                Debug.LogError("[DinoNetworkManager] Server failed to start.");
                return;
            }

            // 3. VPS TCP 릴레???�작 (VPS?�로컬서�?브릿�?
            StartHostRelay();

            // 4. 로컬 ?�라?�언???�속 (?�스???�레?�어)
            networkAddress = "127.0.0.1";
            StartClient();

            // 5. �?코드 ?�림
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
                vpsRelayClient.Connect(VpsAddress, VpsRelayPort);
                var vpsStream = vpsRelayClient.GetStream();

                // 방 코드로 자신 식별
                byte[] ident = Encoding.UTF8.GetBytes($"HOST:{roomCode}:{VpsAuthToken}\n");
                vpsStream.Write(ident, 0, ident.Length);
                vpsStream.Flush();

                // ⭐ Client가 접속해서 데이터를 보낼 때까지 무기한 대기
                // VPS 브릿지는 Client 접속 시에만 시작되므로, Read()는 Client의
                // 첫 번째 Mirror 메시지(JoinGameMessage 등)가 도착할 때까지 블로킹됨
                // 타임아웃 없음 — 접속은 언제든 될 수 있으므로 계속 기다림
                Debug.Log("[DinoNetworkManager] Waiting for remote client...");
                byte[] initialBuf = new byte[65536];
                vpsRelayClient.ReceiveTimeout = 0; // 무기한 대기
                int initialBytes = vpsStream.Read(initialBuf, 0, initialBuf.Length);
                if (initialBytes <= 0)
                {
                    Debug.LogWarning("[DinoNetworkManager] Remote client disconnected before relay established.");
                    return;
                }

                // ⭐ Client가 접속한 후에만 Mirror 서버 연결
                var localClient = new TcpClient();
                localClient.Connect("127.0.0.1", 7777);
                var localStream = localClient.GetStream();

                // 이미 읽은 첫 데이터를 Mirror 서버로 전달
                localStream.Write(initialBuf, 0, initialBytes);
                localStream.Flush();

                Debug.Log("[DinoNetworkManager] VPS relay bridged.");

                // 양방향 포워딩 (나머지 데이터)
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

    // ?�?�?�??�라?�언???�작 (VPS 릴레?? ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?

    public void StartNetworkClient(string code)
    {
        try
        {
            if (featureFlags != null && !featureFlags.enableLanMultiplayer)
            {
                Debug.LogWarning("[DinoNetworkManager] LAN multiplayer disabled by FeatureFlags.");
                return;
            }

            // 1. VPS??�?참여 ?�청
            string resp = TcpCommand($"JOIN:{code}");
            if (resp == null || !resp.StartsWith("OK"))
            {
                Debug.LogError($"[DinoNetworkManager] Failed to join room {code}.");
                var mmc = FindFirstObjectByType<DinoAlkkagi.Presentation.MainMenuController>();
                mmc?.ShowConnectionStatus($"�?{code}�?찾을 ???�습?�다.");
                return;
            }

            roomCode = code;
            GameLaunchContext.SetMode(GameMode.NetworkClient);
            GameLaunchContext.ServerIP = VpsAddress;

            // 2. ?�라?�언??릴레???�작 (로컬?�서 Mirror ?�속 ?��?
            StartClientRelay();

            // 3. 로컬 릴레이로 Mirror 접속 (ClientRelayPort = 17777)
            networkAddress = "127.0.0.1";
            if (transport is PortTransport pt)
                pt.Port = (ushort)ClientRelayPort;
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
                // VPS??먼�? ?�결 + �?코드 ?�송
                vpsRelayClient = new TcpClient();
                vpsRelayClient.Connect(VpsAddress, VpsRelayPort);
                var vpsStream = vpsRelayClient.GetStream();

                byte[] ident = Encoding.UTF8.GetBytes($"CLNT:{roomCode}:{VpsAuthToken}\n");
                vpsStream.Write(ident, 0, ident.Length);
                vpsStream.Flush();

                // 로컬?�서 Mirror ?�속 ?��?
                var listener = new TcpListener(System.Net.IPAddress.Loopback, ClientRelayPort);
                listener.Start();
                relayReadySignal.Set();
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

        // 릴레??리스??준�??�료�?기다�?(최�? 5�?
        if (!relayReadySignal.Wait(5000))
        {
            Debug.LogWarning("[DinoNetworkManager] Client relay listener not ready within 5s, proceeding anyway.");
        }
        relayReadySignal.Reset();
    }

    // ?�?�?�?TCP ?�워???�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?

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

    // ?�?�?�?Mirror ?�트?�크 ?�벤???�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?

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
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        playerCount = NetworkServer.connections.Count;
        Debug.Log($"[DinoNetworkManager] Player disconnected. Total: {playerCount}");

        // 2 미만이면 원격 플레이어 연결 해제 (릴레이/직접 LAN 모두 동일)
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

        // PlayerId 2가 할당되면 원격 플레이어 접속 완료 (릴레이/직접 LAN 모두)
        if (playerId >= 2)
            OnRemotePlayerConnected?.Invoke();
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
        mmc?.ShowConnectionStatus($"P{msg.assignedPlayerId}�??�속?? ?�스?��? 맵을 ?�택 중입?�다...");
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
            mmc?.ShowConnectionStatus("VPS 릴레???�결 ?�패");
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

