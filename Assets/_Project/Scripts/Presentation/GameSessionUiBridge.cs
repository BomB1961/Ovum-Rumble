using DinoAlkkagi.Core;
using DinoAlkkagi.Rules;
using UnityEngine;

namespace DinoAlkkagi.Presentation
{
public class GameSessionUiBridge : MonoBehaviour
{
    private const float DefaultTurnDurationSeconds = 25f;

    [SerializeField] private HudPresenter hudPresenter;
    [SerializeField] private ResultScreen resultScreen;
    [SerializeField] private GameSessionController gameSessionController;
    [SerializeField] private WinConditionChecker winConditionChecker;
    [SerializeField] private float turnDurationSeconds = DefaultTurnDurationSeconds;

    private int currentPlayerId = 1;
    private int p1WinCount;
    private int p2WinCount;
    private float gameStartedAt;
    private float turnStartedAt;
    private bool isPlaying;
    private bool hasGameEnded;

    // 네트워크 타이머 동기화 (스냅샷 수신 시 갱신)
    private float serverGameElapsed;
    private float serverTurnElapsed;
    private bool useServerTime;
    private float serverTimeReceivedAt; // 로컬 Time.time when snapshot arrived

    private void Awake()
    {
        hudPresenter ??= FindFirstObjectByType<HudPresenter>();
        resultScreen ??= FindFirstObjectByType<ResultScreen>();
        gameSessionController ??= FindFirstObjectByType<GameSessionController>();
        winConditionChecker ??= FindFirstObjectByType<WinConditionChecker>();
    }

    private void OnEnable()
    {
        GameEvents.OnGameStarted += HandleGameStarted;
        GameEvents.OnTurnStarted += HandleTurnStarted;
        GameEvents.OnEggLaunched += HandleEggLaunched;
        GameEvents.OnEggFell += HandleEggFell;
        GameEvents.OnGameEnded += HandleGameEnded;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStarted -= HandleGameStarted;
        GameEvents.OnTurnStarted -= HandleTurnStarted;
        GameEvents.OnEggLaunched -= HandleEggLaunched;
        GameEvents.OnEggFell -= HandleEggFell;
        GameEvents.OnGameEnded -= HandleGameEnded;
    }

    private void HandleGameStarted()
    {
        gameStartedAt = Time.time;
        turnStartedAt = Time.time;
        isPlaying = true;
        hasGameEnded = false;
        resultScreen?.Hide();
        hudPresenter?.ShowGuide("알을 조준하세요.");
        RefreshHud();
    }

    private void HandleTurnStarted(int playerId)
    {
        currentPlayerId = playerId;
        turnStartedAt = Time.time;
        RefreshHud();
    }

    private void HandleEggLaunched(EggController egg)
    {
        hudPresenter?.ShowGuide("알이 움직이는 중입니다.\n입력이 잠시 잠깁니다.");
        RefreshHud();
    }

    private void HandleEggFell(EggController egg)
    {
        RefreshHud();
    }

    private void HandleGameEnded(GameResult result)
    {
        if (hasGameEnded)
        {
            return;
        }

        hasGameEnded = true;
        isPlaying = false;
        if (result == GameResult.Player1Win)
        {
            p1WinCount++;
        }
        else if (result == GameResult.Player2Win)
        {
            p2WinCount++;
        }

        int p1EggCount = GetAliveCount(1);
        int p2EggCount = GetAliveCount(2);
        hudPresenter?.ShowGuide("게임 종료\n한 판 더 하거나 메인 메뉴로 돌아가세요.");

        switch (result)
        {
            case GameResult.Player1Win:
                resultScreen?.ShowWin("P1", p1EggCount, p2EggCount, p1WinCount, p2WinCount);
                break;
            case GameResult.Player2Win:
                resultScreen?.ShowWin("P2", p1EggCount, p2EggCount, p1WinCount, p2WinCount);
                break;
            case GameResult.Draw:
                resultScreen?.ShowDraw(p1EggCount, p2EggCount, p1WinCount, p2WinCount);
                break;
        }
    }

    public void RestartGame()
    {
        gameSessionController ??= FindFirstObjectByType<GameSessionController>();
        gameSessionController?.RestartGame();
    }

    private void RefreshHud()
    {
        float gameElapsedSeconds;
        float turnRemainingSeconds;

        if (useServerTime)
        {
            // 스냅샷 수신 시각 이후 경과 시간을 더해 부드럽게 카운트
            float localElapsed = Time.time - serverTimeReceivedAt;
            gameElapsedSeconds = serverGameElapsed + localElapsed;
            turnRemainingSeconds = Mathf.Max(0f, turnDurationSeconds - (serverTurnElapsed + localElapsed));
        }
        else
        {
            gameElapsedSeconds = isPlaying ? Time.time - gameStartedAt : 0f;
            turnRemainingSeconds = isPlaying
                ? Mathf.Max(0f, turnDurationSeconds - (Time.time - turnStartedAt))
                : turnDurationSeconds;
        }

        hudPresenter?.ShowHud(GetCurrentPlayerName(), GetAliveCount(1), GetAliveCount(2), turnRemainingSeconds, gameElapsedSeconds);
    }

    /// <summary>
    /// 서버에서 받은 타이머 값을 클라이언트 HUD에 적용한다.
    /// 스냅샷 도착 시각을 기록하여 다음 스냅샷까지 부드럽게 보간한다.
    /// </summary>
    public void ApplyServerTime(float gameElapsed, float turnElapsed)
    {
        if (!GameLaunchContext.IsNetworkClient) return;

        serverGameElapsed = gameElapsed;
        serverTurnElapsed = turnElapsed;
        serverTimeReceivedAt = Time.time;
        useServerTime = true;
    }

    /// <summary>
    /// 서버가 스냅샷에 보낼 현재 게임/턴 경과 시간 반환.
    /// </summary>
    public float GetGameElapsedTime()
    {
        return isPlaying ? Time.time - gameStartedAt : 0f;
    }

    public float GetTurnElapsedTime()
    {
        return isPlaying ? Time.time - turnStartedAt : 0f;
    }

    private void Update()
    {
        if (isPlaying)
        {
            RefreshHud();
        }
    }

    private int GetAliveCount(int playerId)
    {
        if (winConditionChecker != null)
        {
            return winConditionChecker.GetAliveCount(playerId);
        }

        if (gameSessionController == null)
        {
            return 0;
        }

        int count = 0;
        foreach (EggController egg in gameSessionController.AllEggs)
        {
            if (egg != null && egg.OwnerPlayerId == playerId && egg.IsAlive)
            {
                count++;
            }
        }

        return count;
    }

    private string GetCurrentPlayerName()
    {
        return $"P{currentPlayerId}";
    }
}
}
