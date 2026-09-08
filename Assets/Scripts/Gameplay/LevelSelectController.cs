using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectController : MonoBehaviour
{
    [SerializeField] private LevelDatabase database;
    [SerializeField] private int columns = 3;
    [SerializeField] private Vector2 buttonSize = new Vector2(200f, 110f);
    [SerializeField] private float spacing = 20f;

    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle lockedStyle;

    private void OnGUI()
    {
        if (database == null || database.Count == 0)
        {
            return;
        }

        EnsureStyles();

        GUI.Label(new Rect(0f, 40f, Screen.width, 60f), "MISKETR", titleStyle);
        GUI.Label(new Rect(0f, 100f, Screen.width, 40f), "Toplam yildiz: " + ProgressService.TotalStars(database.Count), lockedStyle);

        int count = database.Count;
        int rows = Mathf.CeilToInt(count / (float)columns);
        float gridWidth = (columns * buttonSize.x) + ((columns - 1) * spacing);
        float gridHeight = (rows * buttonSize.y) + ((rows - 1) * spacing);
        float startX = (Screen.width - gridWidth) * 0.5f;
        float startY = Mathf.Max(170f, (Screen.height - gridHeight) * 0.5f);

        for (int i = 0; i < count; i++)
        {
            LevelData level = database.Get(i);

            if (level == null)
            {
                continue;
            }

            int column = i % columns;
            int row = i / columns;

            Rect rect = new Rect(
                startX + (column * (buttonSize.x + spacing)),
                startY + (row * (buttonSize.y + spacing)),
                buttonSize.x,
                buttonSize.y);

            bool unlocked = ProgressService.IsUnlocked(i);
            int stars = ProgressService.GetStars(i);

            string label = unlocked
                ? (i + 1) + ". " + level.levelName + "\n" + StarText(stars)
                : (i + 1) + ". Kilitli";

            GUI.enabled = unlocked;

            if (GUI.Button(rect, label, buttonStyle))
            {
                GameSession.SelectedLevelIndex = i;
                SceneManager.LoadScene(GameSession.GameSceneName);
            }

            GUI.enabled = true;
        }

        if (GUI.Button(new Rect(20f, Screen.height - 70f, 220f, 50f), "Ilerlemeyi sifirla"))
        {
            ProgressService.ResetProgress(database.Count);
        }
    }

    private static string StarText(int stars)
    {
        return new string('*', Mathf.Clamp(stars, 0, 3)) + new string('-', Mathf.Clamp(3 - stars, 0, 3));
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
        {
            return;
        }

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 48,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        titleStyle.normal.textColor = Color.white;

        lockedStyle = new GUIStyle(titleStyle)
        {
            fontSize = 22
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
    }
}
