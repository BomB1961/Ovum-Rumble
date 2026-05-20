using UnityEngine;

namespace DinoAlkkagi.Presentation
{
public class GameSessionUiBridge : MonoBehaviour
{
    private const float DefaultGameDurationSeconds = 300f;
    private const float DefaultTurnDurationSeconds = 25f;
    private const int DefaultStartingEggCount = 4;

    [SerializeField] private HudPresenter hudPresenter;
    [SerializeField] private ResultScreen resultScreen;
    [SerializeField] private float gameDurationSeconds = DefaultGameDurationSeconds;
    [SerializeField] private float turnDurationSeconds = DefaultTurnDurationSeconds;
    [SerializeField] private int startingEggCount = DefaultStartingEggCount;

    private int currentPlayerIndex;
    private int p1EggCount;
    private int p2EggCount;
    private float gameStartedAt;
    private float turnStartedAt;
    private bool isPlaying;

    private void Awake()
    {
        hudPresenter ??= FindFirstObjectByType<HudPresenter>();
        resultScreen ??= FindFirstObjectByType<ResultScreen>();
    }

    private void Start()
    {
        StartGame();
    }

    private void Update()
    {
        if (!isPlaying)
        {
            return;
        }

        float gameElapsedSeconds = Time.time - gameStartedAt;
        float turnRemainingSeconds = Mathf.Max(0f, turnDurationSeconds - (Time.time - turnStartedAt));

        hudPresenter?.ShowHud(GetCurrentPlayerName(), p1EggCount, p2EggCount, turnRemainingSeconds, gameElapsedSeconds);

        if (gameElapsedSeconds >= gameDurationSeconds)
        {
            HandleGameTimeExpired();
        }
    }

    public void StartGame()
    {
        currentPlayerIndex = 0;
        p1EggCount = startingEggCount;
        p2EggCount = startingEggCount;
        gameStartedAt = Time.time;
        turnStartedAt = Time.time;
        isPlaying = true;

        resultScreen?.Hide();
        hudPresenter?.ShowGuide("\uc54c\uc744 \uc870\uc900\ud558\uc138\uc694.");
        hudPresenter?.ShowHud(GetCurrentPlayerName(), p1EggCount, p2EggCount, turnDurationSeconds, 0f);
    }

    public void RestartGame()
    {
        StartGame();
    }

    public void RequestTurnAdvance()
    {
        currentPlayerIndex = currentPlayerIndex == 0 ? 1 : 0;
        turnStartedAt = Time.time;
        hudPresenter?.ShowHud(GetCurrentPlayerName(), p1EggCount, p2EggCount, turnDurationSeconds, Time.time - gameStartedAt);
    }

    public void SetEggCount(int playerIndex, int eggCount)
    {
        if (playerIndex == 0)
        {
            p1EggCount = Mathf.Max(0, eggCount);
        }
        else if (playerIndex == 1)
        {
            p2EggCount = Mathf.Max(0, eggCount);
        }

        hudPresenter?.ShowHud(GetCurrentPlayerName(), p1EggCount, p2EggCount, GetTurnRemainingSeconds(), Time.time - gameStartedAt);
    }

    public void ShowWinResult(string winnerName)
    {
        isPlaying = false;
        resultScreen?.ShowWin(winnerName, p1EggCount, p2EggCount, 0, 0);
    }

    public void ShowDrawResult()
    {
        isPlaying = false;
        resultScreen?.ShowDraw(p1EggCount, p2EggCount, 0, 0);
    }

    private void HandleGameTimeExpired()
    {
        isPlaying = false;
        hudPresenter?.ShowGuide("\uc804\uccb4 \uac8c\uc784 \uc2dc\uac04\uc774 \uc885\ub8cc\ub418\uc5c8\uc2b5\ub2c8\ub2e4.");
    }

    private float GetTurnRemainingSeconds()
    {
        return Mathf.Max(0f, turnDurationSeconds - (Time.time - turnStartedAt));
    }

    private string GetCurrentPlayerName()
    {
        return currentPlayerIndex == 0 ? "P1" : "P2";
    }
}
}
