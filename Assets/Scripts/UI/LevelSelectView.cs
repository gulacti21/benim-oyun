using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectView : MonoBehaviour
{
    [SerializeField] private LevelDatabase database;
    [SerializeField] private LevelCardView[] cards;
    [SerializeField] private TextMeshProUGUI totalStarsLabel;
    [SerializeField] private Button resetButton;

    private void Awake()
    {
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(HandleReset);
        }
    }

    private void Start()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (database == null || cards == null)
        {
            return;
        }

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null)
            {
                continue;
            }

            cards[i].Bind(i, database.Get(i), HandleCardClicked);
        }

        if (totalStarsLabel != null)
        {
            totalStarsLabel.SetText(ProgressService.TotalStars(database.Count) + " / " + (database.Count * 3) + " YILDIZ");
        }
    }

    private void HandleCardClicked(int index)
    {
        GameSession.SelectedLevelIndex = index;
        SceneManager.LoadScene(GameSession.GameSceneName);
    }

    private void HandleReset()
    {
        if (database == null)
        {
            return;
        }

        ProgressService.ResetProgress(database.Count);
        Refresh();
    }
}
