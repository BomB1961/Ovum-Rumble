namespace DinoAlkkagi.Core
{
    public static class GameLaunchContext
    {
        public static MapId SelectedMap { get; private set; } = MapId.Terrian;
        public static bool HasSelectedMap { get; private set; }

        public static void SelectMap(MapId mapId)
        {
            SelectedMap = mapId;
            HasSelectedMap = true;
        }
    }
}
