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

    // VPS Î¶¥Î†à??(?úÎèÖ?? ?∏Ïä§?ôÌÑ∞ ÎØ∏ÎÖ∏Ï∂?
    private const int VpsRelayPort = 7777;
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

    // ?Ä?Ä?Ä VPS TCP ?úÏñ¥ ?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä

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

    // ?Ä?Ä?Ä ?∏Ïä§???úÏûë (VPS Î¶¥Î†à?? ?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä

    public void StartNetworkHost()
    {
        try
        {
            if (featureFlags != null && !featureFlags.enableLanMultiplayer)
            {
                Debug.LogWarning("[DinoNetworkManager] LAN multiplayer disabled by FeatureFlags.");
                return;
            }

            // 1. VPS??Î∞??ùÏÑ±
            string resp = TcpCommand("CREATE_ROOM");
            if (resp == null || !resp.StartsWith("CODE:"))
            {
                Debug.LogError("[DinoNetworkManager] Failed to create room on VPS.");
                return;
            }
            roomCode = resp.Substring(5).Trim();
            Debug.Log($"[DinoNetworkManager] Room created: {roomCode}");

            // 2. Î°úÏª¨ Mirror ?úÎ≤Ñ ?úÏûë (TelepathyTransport, 127.0.0.1:7777)
            GameLaunchContext.SetMode(GameMode.NetworkHost);
            GameLaunchContext.ServerIP = VpsAddress;
            networkAddress = "127.0.0.1";
            StartServer();

            if (!NetworkServer.active)
            {
                Debug.LogError("[DinoNetworkManager] Server failed to start.");
                return;
            }

            // 3. VPS TCP Î¶¥Î†à???úÏûë (VPS?îÎ°úÏª¨ÏÑúÎ≤?Î∏åÎ¶øÏß?
            StartHostRelay();

            // 4. Î°úÏª¨ ?¥Îùº?¥Ïñ∏???ëÏÜç (?∏Ïä§???åÎ†à?¥Ïñ¥)
            networkAddress = "127.0.0.1";
            StartClient();

            // 5. Î∞?ÏΩîÎìú ?åÎ¶º
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

                // Î∞?ÏΩîÎìúÎ°??∏Ïä§???ùÎ≥Ñ
                byte[] ident = Encoding.UTF8.GetBytes($"HOST:{roomCode}:{VpsAuthToken}\n");
                vpsStream.Write(ident, 0, ident.Length);
                vpsStream.Flush();

                // Î°úÏª¨ Mirror ?úÎ≤Ñ???∞Í≤∞ (Virtual Client ??ï†)
                var localClient = new TcpClient();
                localClient.Connect("127.0.0.1", 7777);
                var localStream = localClient.GetStream();

                Debug.Log("[DinoNetworkManager] VPS relay bridged.");

                // ?ëÎ∞©???¨Ïõå??
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

    // ?Ä?Ä?Ä ?¥Îùº?¥Ïñ∏???úÏûë (VPS Î¶¥Î†à?? ?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä

    public void StartNetworkClient(string code)
    {
        try
        {
            if (featureFlags != null && !featureFlags.enableLanMultiplayer)
            {
                Debug.LogWarning("[DinoNetworkManager] LAN multiplayer disabled by FeatureFlags.");
                return;
            }

            // 1. VPS??Î∞?Ï∞∏Ïó¨ ?îÏ≤≠
            string resp = TcpCommand($"JOIN:{code}");
            if (resp == null || !resp.StartsWith("OK"))
            {
                Debug.LogError($"[DinoNetworkManager] Failed to join room {code}.");
                var mmc = FindFirstObjectByType<DinoAlkkagi.Presentation.MainMenuController>();
                mmc?.ShowConnectionStatus($"Î∞?{code}Î•?Ï∞æÏùÑ ???ÜÏäµ?àÎã§.");
                return;
            }

            roomCode = code;
            GameLaunchContext.SetMode(GameMode.NetworkClient);
            GameLaunchContext.ServerIP = VpsAddress;

            // 2. ?¥Îùº?¥Ïñ∏??Î¶¥Î†à???úÏûë (Î°úÏª¨?êÏÑú Mirror ?ëÏÜç ?ÄÍ∏?
            StartClientRelay();

            // 3. Î°úÏª¨ Î¶¥Î†à?¥Î°ú Mirror ?ëÏÜç
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
                // VPS??Î®ºÏ? ?∞Í≤∞ + Î∞?ÏΩîÎìú ?ÑÏÜ°
                vpsRelayClient = new TcpClient();
                vpsRelayClient.Connect(VpsAddress, VpsRelayPort);
                var vpsStream = vpsRelayClient.GetStream();

                byte[] ident = Encoding.UTF8.GetBytes($"CLNT:{roomCode}:{VpsAuthToken}\n");
                vpsStream.Write(ident, 0, ident.Length);
                vpsStream.Flush();

                // Î°úÏª¨?êÏÑú Mirror ?ëÏÜç ?ÄÍ∏?
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

        Thread.Sleep(100); // Î¶¥Î†à??Î¶¨Ïä§??Ï§ÄÎπ??ÄÍ∏?
    }

    // ?Ä?Ä?Ä TCP ?¨Ïõå???Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä

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

    // ?Ä?Ä?Ä Mirror ?§Ìä∏?åÌÅ¨ ?¥Î≤§???Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä?Ä

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

        // VPS Î¶¥Î†à??Î™®Îìú(roomCode != null): host client(1) + relay bridge(1) = 2Í∞?Í∏∞Î≥∏
        //   ???êÍ≤© ?åÎ†à?¥Ïñ¥ ?ëÏÜç ??3Í∞úÍ? ?òÎ?Î°?>= 3
        // ÏßÅÏ†ë LAN Î™®Îìú(roomCode == null): host client(1) = 1Í∞?Í∏∞Î≥∏
        //   ???êÍ≤© ?åÎ†à?¥Ïñ¥ ?ëÏÜç ??2Í∞úÍ? ?òÎ?Î°?>= 2
        bool isRelayMode = roomCode != null;
        int remoteThreshold = isRelayMode ? 3 : 2;
        if (playerCount >= remoteThreshold)
            OnRemotePlayerConnected?.Invoke();
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        playerCount = NetworkServer.connections.Count;
        Debug.Log($"[DinoNetworkManager] Player disconnected. Total: {playerCount}");

        bool isRelayMode = roomCode != null;
        int remoteThreshold = isRelayMode ? 3 : 2;
        if (playerCount < remoteThreshold)
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
        mmc?.ShowConnectionStatus($"P{msg.assignedPlayerId}Î°??ëÏÜç?? ?∏Ïä§?∏Í? ÎßµÏùÑ ?†ÌÉù Ï§ëÏûÖ?àÎã§...");
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
            mmc?.ShowConnectionStatus("VPS Î¶¥Î†à???∞Í≤∞ ?§Ìå®");
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
