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

        public static void SetMode(GameMode mode)
        {
            CurrentMode = mode;
        }

        public static void ResetToDefault()
        {
            CurrentMode = GameMode.LocalHotseat;
        }
    }
}
