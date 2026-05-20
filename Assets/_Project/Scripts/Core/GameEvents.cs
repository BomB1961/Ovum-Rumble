using System;

namespace DinoAlkkagi.Core
{
    /// <summary>
    /// 게임 전체에서 사용하는 중앙 이벤트 채널.
    /// 각 시스템은 이벤트를 발행하거나 구독하여 결합도를 낮춘다.
    /// </summary>
    public static class GameEvents
    {
        public static event Action OnGameStarted;
        public static event Action<int> OnTurnStarted;
        public static event Action<EggController> OnEggLaunched;
        public static event Action<float> OnEggCollision;
        public static event Action<EggController> OnEggFell;
        public static event Action OnAllEggsStopped;
        public static event Action<GameResult> OnGameEnded;

        public static void TriggerGameStarted() => OnGameStarted?.Invoke();
        public static void TriggerTurnStarted(int playerId) => OnTurnStarted?.Invoke(playerId);
        public static void TriggerEggLaunched(EggController egg) => OnEggLaunched?.Invoke(egg);
        public static void TriggerEggCollision(float impact) => OnEggCollision?.Invoke(impact);
        public static void TriggerEggFell(EggController egg) => OnEggFell?.Invoke(egg);
        public static void TriggerAllEggsStopped() => OnAllEggsStopped?.Invoke();
        public static void TriggerGameEnded(GameResult result) => OnGameEnded?.Invoke(result);
    }
}
