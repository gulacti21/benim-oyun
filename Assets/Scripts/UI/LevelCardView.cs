using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelCardView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private UnityEngine.UI.Image background;
    [SerializeField] private TextMeshProUGUI numberLabel;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private UnityEngine.UI.Image[] stars;

    [SerializeField] private Color unlockedBackground = new Color(0.21f, 0.16f, 0.12f, 1f);
    [SerializeField] private Color lockedBackground = new Color(0.13f, 0.11f, 0.09f, 1f);
    [SerializeField] private Color unlockedText = new Color(0.96f, 0.92f, 0.84f, 1f);
    [SerializeField] private Color lockedText = new Color(0.96f, 0.92f, 0.84f, 0.28f);
    [SerializeField] private Color starOnColor = new Color(0.96f, 0.74f, 0.26f, 1f);
    [SerializeField] private Color starOffColor = new Color(1f, 1f, 1f, 0.12f);

    private int levelIndex;
    private Action<int> clickHandler;

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }
    }

    public void Bind(int index, LevelData level, Action<int> onClick)
    {
        levelIndex = index;
        clickHandler = onClick;

        bool unlocked = ProgressService.IsUnlocked(index);
        int earnedStars = ProgressService.GetStars(index);

        if (numberLabel != null)
        {
            numberLabel.SetText((index + 1).ToString());
            numberLabel.color = unlocked ? unlockedText : lockedText;
        }

        if (nameLabel != null)
        {
            nameLabel.SetText(unlocked ? (level != null ? level.levelName : "Seviye") : "Kilitli");
            nameLabel.color = unlocked ? unlockedText : lockedText;
        }

        if (background != null)
        {
            background.color = unlocked ? unlockedBackground : lockedBackground;
        }

        if (button != null)
        {
            button.interactable = unlocked;
        }

        if (stars == null)
        {
            return;
        }

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null)
            {
                continue;
            }

            stars[i].enabled = unlocked;
            stars[i].color = i < earnedStars ? starOnColor : starOffColor;
        }
    }

    private void HandleClick()
    {
        clickHandler?.Invoke(levelIndex);
    }
}
