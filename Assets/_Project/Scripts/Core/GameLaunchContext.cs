namespace DinoAlkkagi.Core
{
    public enum GameMode
    {
        LocalHotseat,
        VsComputer,
        NetworkHost,
        NetworkClient
    }

    public static class GameLaunchContext
    {
        public static GameMode CurrentMode { get; private set; } = GameMode.LocalHotseat;
        public static bool IsVsComputer => CurrentMode == GameMode.VsComputer;
        public static bool IsNetwork => CurrentMode == GameMode.NetworkHost || CurrentMode == GameMode.NetworkClient;
        public static bool IsNetworkHost => CurrentMode == GameMode.NetworkHost;
        public static bool IsNetworkClient => CurrentMode == GameMode.NetworkClient;
        public static MapId SelectedMap { get; private set; } = MapId.Terrian;
        public static bool HasSelectedMap { get; private set; }

        public static string ServerIP { get; set; } = "127.0.0.1";
        public static int ServerPort { get; set; } = 7777;
        public static int LocalPlayerId { get; private set; } = 1;

        public static void SetMode(GameMode mode)
        {
            CurrentMode = mode;
        }

        public static void SelectMap(MapId mapId)
        {
            SelectedMap = mapId;
            HasSelectedMap = true;
        }

        public static void SetNetworkClientInfo(int assignedPlayerId)
        {
            LocalPlayerId = assignedPlayerId;
        }

        public static void ResetToDefault()
        {
            CurrentMode = GameMode.LocalHotseat;
            SelectedMap = MapId.Terrian;
            HasSelectedMap = false;
            ServerIP = "127.0.0.1";
            LocalPlayerId = 1;
        }
    }
}
