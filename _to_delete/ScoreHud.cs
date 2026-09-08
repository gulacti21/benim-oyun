using UnityEngine;

public class ScoreHud : MonoBehaviour
{
    [SerializeField] private LevelController controller;
    [SerializeField] private int fontSize = 28;

    private GUIStyle labelStyle;
    private GUIStyle titleStyle;

    private void OnGUI()
    {
        if (controller == null)
        {
            return;
        }

        EnsureStyles();

        string levelName = controller.Level != null ? controller.Level.levelName : "Seviye";

        GUI.Label(new Rect(24f, 16f, 600f, 40f), (controller.LevelIndex + 1) + ". " + levelName, labelStyle);
        GUI.Label(new Rect(24f, 52f, 600f, 40f), "Puan: " + controller.Score + " / " + controller.TotalMarbles, labelStyle);
        GUI.Label(new Rect(24f, 88f, 600f, 40f), "Atis hakki: " + controller.ShotsLeft, labelStyle);

        if (GUI.Button(new Rect(Screen.width - 180f, 16f, 160f, 44f), "Harita"))
        {
            controller.OpenLevelSelect();
        }

        if (controller.State == LevelController.LevelState.Playing)
        {
            return;
        }

        float width = 460f;
        float height = 280f;
        Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

        GUI.Box(panel, GUIContent.none);

        bool won = controller.State == LevelController.LevelState.Won;
        GUI.Label(new Rect(panel.x, panel.y + 20f, panel.width, 50f), won ? "KAZANDIN" : "KAYBETTIN", titleStyle);

        string stars = new string('*', controller.Stars) + new string('-', 3 - controller.Stars);
        GUI.Label(new Rect(panel.x, panel.y + 78f, panel.width, 40f), "Yildiz: " + stars, titleStyle);

        float buttonY = panel.y + 140f;

        if (GUI.Button(new Rect(panel.x + 30f, buttonY, 180f, 50f), "Tekrar Oyna"))
        {
            controller.RestartLevel();
        }

        if (won && controller.HasNextLevel)
        {
            if (GUI.Button(new Rect(panel.x + 240f, buttonY, 190f, 50f), "Sonraki Seviye"))
            {
                controller.LoadNextLevel();
            }
        }

        if (GUI.Button(new Rect(panel.x + 130f, buttonY + 65f, 200f, 50f), "Seviye Haritasi"))
        {
            controller.OpenLevelSelect();
        }
    }

    private void EnsureStyles()
    {
        if (labelStyle != null)
        {
            return;
        }

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = FontStyle.Bold
        };

        labelStyle.normal.textColor = Color.white;

        titleStyle = new GUIStyle(labelStyle)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = fontSize + 8
        };
    }
}
