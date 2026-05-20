using UnityEngine;

namespace DinoAlkkagi.Presentation
{
public class HudPresenter : MonoBehaviour
{
    [SerializeField] private GameUIController gameUIController;

    private void Awake()
    {
        gameUIController ??= FindFirstObjectByType<GameUIController>();
    }

    public void ShowHud(string currentPlayerName, int p1EggCount, int p2EggCount, float turnRemainingSeconds, float gameElapsedSeconds)
    {
        if (gameUIController == null)
        {
            return;
        }

        gameUIController.UpdateHUD(currentPlayerName, p1EggCount, p2EggCount, turnRemainingSeconds, gameElapsedSeconds);
    }

    public void ShowGuide(string message)
    {
        if (gameUIController != null)
        {
            gameUIController.SetGuideText(message);
        }
    }
}
}
