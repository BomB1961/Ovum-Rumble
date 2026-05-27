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
        hudPresenter?.ShowGuide("\uc54c\uc744 \uc870\uc900\ud558\uc138\uc694.");
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
        hudPresenter?.ShowGuide("\uc54c\uc774 \uc6c0\uc9c1\uc774\ub294 \uc911\uc785\ub2c8\ub2e4.\n\uc785\ub825\uc774 \uc7a0\uc2dc \uc7a0\uae41\ub2c8\ub2e4.");
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
        hudPresenter?.ShowGuide("\uac8c\uc784 \uc885\ub8cc\n\ud55c \ud310 \ub354 \ud558\uac70\ub098 \uba54\uc778 \uba54\ub274\ub85c \ub3cc\uc544\uac00\uc138\uc694.");

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
        float gameElapsedSeconds = isPlaying ? Time.time - gameStartedAt : 0f;
        float turnRemainingSeconds = isPlaying
            ? Mathf.Max(0f, turnDurationSeconds - (Time.time - turnStartedAt))
            : turnDurationSeconds;

        hudPresenter?.ShowHud(GetCurrentPlayerName(), GetAliveCount(1), GetAliveCount(2), turnRemainingSeconds, gameElapsedSeconds);
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
        if (GameLaunchContext.IsVsComputer)
        {
            return currentPlayerId == 2 ? "AI" : "\ud50c\ub808\uc774\uc5b4";
        }

        return $"P{currentPlayerId}";
    }
}
}
