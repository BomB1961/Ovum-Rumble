using UnityEngine;

namespace DinoAlkkagi.Presentation
{
public class ResultScreen : MonoBehaviour
{
    [SerializeField] private GameUIController gameUIController;

    private void Awake()
    {
        gameUIController ??= FindFirstObjectByType<GameUIController>();
    }

    public void ShowWin(string winnerName, int p1EggCount, int p2EggCount, int p1WinCount, int p2WinCount)
    {
        if (gameUIController != null)
        {
            gameUIController.ShowResult(winnerName, p1EggCount, p2EggCount, p1WinCount, p2WinCount);
        }
    }

    public void ShowDraw(int p1EggCount, int p2EggCount, int p1WinCount, int p2WinCount)
    {
        if (gameUIController != null)
        {
            gameUIController.ShowDrawResult(p1EggCount, p2EggCount, p1WinCount, p2WinCount);
        }
    }

    public void Hide()
    {
        if (gameUIController != null)
        {
            gameUIController.HideResult();
        }
    }
}
}
