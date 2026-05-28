namespace DinoAlkkagi.Core
{
    public enum GameMode
    {
        LocalHotseat,
        VsComputer
    }

    public static class GameLaunchContext
    {
        public static GameMode CurrentMode { get; private set; } = GameMode.LocalHotseat;
        public static bool IsVsComputer => CurrentMode == GameMode.VsComputer;
        public static MapId SelectedMap { get; private set; } = MapId.Terrian;
        public static bool HasSelectedMap { get; private set; }
        public static string ServerAddress { get; private set; } = string.Empty;

        public static void SetMode(GameMode mode)
        {
            CurrentMode = mode;
        }

        public static void SelectMap(MapId mapId)
        {
            SelectedMap = mapId;
            HasSelectedMap = true;
        }

        public static void SetServerAddress(string serverAddress)
        {
            ServerAddress = serverAddress ?? string.Empty;
        }

        public static void ResetToDefault()
        {
            CurrentMode = GameMode.LocalHotseat;
            SelectedMap = MapId.Terrian;
            HasSelectedMap = false;
            ServerAddress = string.Empty;
        }
    }
}
