using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class UiBuilder
{
    public static readonly Color Cream = new Color(0.96f, 0.92f, 0.84f, 1f);
    public static readonly Color CreamDim = new Color(0.96f, 0.92f, 0.84f, 0.62f);
    public static readonly Color PanelBrown = new Color(0.17f, 0.13f, 0.10f, 0.98f);
    public static readonly Color ChipBrown = new Color(0.12f, 0.09f, 0.07f, 0.62f);
    public static readonly Color Overlay = new Color(0.06f, 0.045f, 0.035f, 0.82f);
    public static readonly Color Brass = new Color(0.85f, 0.60f, 0.24f, 1f);
    public static readonly Color BrassDeep = new Color(0.55f, 0.36f, 0.16f, 1f);
    public static readonly Color Gold = new Color(0.96f, 0.74f, 0.26f, 1f);
    public static readonly Color StarOff = new Color(1f, 1f, 1f, 0.14f);

    public static GameHudReferences LastReferences;

    public struct GameHudReferences
    {
        public TextMeshProUGUI LevelLabel;
        public TextMeshProUGUI ScoreLabel;
        public TextMeshProUGUI ShotsLabel;
        public Button PauseButton;
        public GameObject PausePanel;
        public Button ResumeButton;
        public Button PauseRestartButton;
        public Button PauseMapButton;
        public GameObject ResultPanel;
        public TextMeshProUGUI ResultTitle;
        public TextMeshProUGUI ResultScore;
        public UnityEngine.UI.Image[] StarIcons;
        public Button RetryButton;
        public Button NextButton;
        public Button ResultMapButton;
    }

    private static TMP_FontAsset cachedFont;

    private const string CharacterSet =
        "0123456789" +
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
        "abcdefghijklmnopqrstuvwxyz" +
        "ÇĞİÖŞÜçğıöşü" +
        " .,:;!?()[]-_+=*/%&#@|~^" +
        "\u2022\u2026";

    public static TMP_FontAsset GetFontAsset()
    {
        if (cachedFont != null)
        {
            return cachedFont;
        }

        string assetPath = "Assets/Fonts/Nunito SDF.asset";
        cachedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);

        if (cachedFont != null)
        {
            return cachedFont;
        }

        Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Nunito-Bold.ttf");

        if (source == null)
        {
            Debug.LogWarning("[MISKETR] Nunito-Bold.ttf not found, falling back to the default TMP font.");
            return null;
        }

        TMP_FontAsset created = TMP_FontAsset.CreateFontAsset(source);

        if (created == null)
        {
            return null;
        }

        created.name = "Nunito SDF";

        string missing;

        if (!created.TryAddCharacters(CharacterSet, out missing))
        {
            Debug.LogWarning("[MISKETR] Some glyphs could not be baked: " + missing);
        }

        created.atlasPopulationMode = AtlasPopulationMode.Static;

        AssetDatabase.CreateAsset(created, assetPath);

        if (created.material != null)
        {
            created.material.name = "Nunito SDF Material";
            AssetDatabase.AddObjectToAsset(created.material, created);
        }

        if (created.atlasTextures != null)
        {
            for (int i = 0; i < created.atlasTextures.Length; i++)
            {
                if (created.atlasTextures[i] != null)
                {
                    created.atlasTextures[i].name = "Nunito SDF Atlas " + i;
                    AssetDatabase.AddObjectToAsset(created.atlasTextures[i], created);
                }
            }
        }

        EditorUtility.SetDirty(created);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        cachedFont = created;

        return cachedFont;
    }

    public static Sprite RoundedSprite()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    public static Sprite CircleSprite()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
    }

    public static GameObject CreateCanvas(string canvasName)
    {
        GameObject canvasObject = new GameObject(canvasName, typeof(RectTransform));

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        return canvasObject;
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    public static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    public static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = anchoredPosition;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static UnityEngine.UI.Image CreatePanel(string name, Transform parent, Color color, float cornerScale, bool raycastTarget)
    {
        RectTransform rect = CreateRect(name, parent);
        UnityEngine.UI.Image image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = RoundedSprite();
        image.type = UnityEngine.UI.Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = cornerScale;
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    public static UnityEngine.UI.Image CreateCircle(string name, Transform parent, Color color, bool raycastTarget)
    {
        RectTransform rect = CreateRect(name, parent);
        UnityEngine.UI.Image image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = CircleSprite();
        image.type = UnityEngine.UI.Image.Type.Simple;
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    public static TextMeshProUGUI CreateText(string name, Transform parent, string content, float fontSize, TextAlignmentOptions alignment, Color color, float characterSpacing)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.characterSpacing = characterSpacing;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        TMP_FontAsset fontAsset = GetFontAsset();

        if (fontAsset != null)
        {
            text.font = fontAsset;
        }

        return text;
    }

    public static Button CreatePillButton(string name, Transform parent, string label, Color background, Color labelColor, Vector2 size, float fontSize)
    {
        UnityEngine.UI.Image image = CreatePanel(name, parent, background, 0.16f, true);
        image.rectTransform.sizeDelta = size;

        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TextMeshProUGUI text = CreateText("Label", image.transform, label, fontSize, TextAlignmentOptions.Center, labelColor, 2f);
        Stretch(text.rectTransform);

        return button;
    }

    private static UnityEngine.UI.Image CreateChip(Transform parent, string caption, string value, Vector2 anchoredPosition, out TextMeshProUGUI valueLabel)
    {
        UnityEngine.UI.Image chip = CreatePanel("Chip_" + caption, parent, ChipBrown, 0.22f, false);
        Anchor(chip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, new Vector2(260f, 124f));

        TextMeshProUGUI captionLabel = CreateText("Caption", chip.transform, caption, 22f, TextAlignmentOptions.TopLeft, CreamDim, 6f);
        Anchor(captionLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -14f), new Vector2(212f, 26f));

        valueLabel = CreateText("Value", chip.transform, value, 40f, TextAlignmentOptions.BottomLeft, Cream, 0f);
        Anchor(valueLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 16f), new Vector2(212f, 48f));

        return chip;
    }

    public static void BuildGameHud(GameObject canvasObject, out GameHudView view)
    {
        Transform root = canvasObject.transform;

        RectTransform safeArea = CreateRect("SafeArea", root);
        Stretch(safeArea);
        safeArea.offsetMin = new Vector2(0f, 0f);
        safeArea.offsetMax = new Vector2(0f, -60f);

        TextMeshProUGUI levelLabel = CreateText("LevelLabel", safeArea, "SEVİYE 1", 26f, TextAlignmentOptions.TopLeft, CreamDim, 8f);
        Anchor(levelLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -20f), new Vector2(700f, 32f));

        TextMeshProUGUI scoreLabel;
        CreateChip(safeArea, "MİSKET", "0 / 0", new Vector2(36f, -104f), out scoreLabel);

        TextMeshProUGUI shotsLabel;
        CreateChip(safeArea, "ATIŞ", "0", new Vector2(316f, -104f), out shotsLabel);

        Button pauseButton = CreatePillButton("PauseButton", safeArea, "II", ChipBrown, Cream, new Vector2(104f, 104f), 40f);
        UnityEngine.UI.Image pauseImage = pauseButton.GetComponent<UnityEngine.UI.Image>();
        pauseImage.sprite = CircleSprite();
        pauseImage.type = UnityEngine.UI.Image.Type.Simple;
        Anchor(pauseButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -14f), new Vector2(104f, 104f));

        // Pause panel
        UnityEngine.UI.Image pauseOverlay = CreatePanel("PausePanel", root, Overlay, 1f, true);
        pauseOverlay.sprite = null;
        Stretch(pauseOverlay.rectTransform);

        UnityEngine.UI.Image pauseBox = CreatePanel("Box", pauseOverlay.transform, PanelBrown, 0.10f, false);
        Anchor(pauseBox.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 620f));

        TextMeshProUGUI pauseTitle = CreateText("Title", pauseBox.transform, "MOLA", 58f, TextAlignmentOptions.Center, Cream, 12f);
        Anchor(pauseTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(700f, 70f));

        Button resumeButton = CreatePillButton("ResumeButton", pauseBox.transform, "DEVAM ET", Brass, new Color(0.14f, 0.10f, 0.06f), new Vector2(560f, 104f), 38f);
        Anchor(resumeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(560f, 104f));

        Button pauseRestartButton = CreatePillButton("RestartButton", pauseBox.transform, "TEKRAR OYNA", BrassDeep, Cream, new Vector2(560f, 104f), 38f);
        Anchor(pauseRestartButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -310f), new Vector2(560f, 104f));

        Button pauseMapButton = CreatePillButton("MapButton", pauseBox.transform, "SEVİYE HARİTASI", ChipBrown, Cream, new Vector2(560f, 104f), 38f);
        Anchor(pauseMapButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -440f), new Vector2(560f, 104f));

        // Result panel
        UnityEngine.UI.Image resultOverlay = CreatePanel("ResultPanel", root, Overlay, 1f, true);
        resultOverlay.sprite = null;
        Stretch(resultOverlay.rectTransform);

        UnityEngine.UI.Image resultBox = CreatePanel("Box", resultOverlay.transform, PanelBrown, 0.10f, false);
        Anchor(resultBox.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 820f));

        TextMeshProUGUI resultTitle = CreateText("Title", resultBox.transform, "KAZANDIN", 62f, TextAlignmentOptions.Center, Cream, 12f);
        Anchor(resultTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(800f, 80f));

        UnityEngine.UI.Image[] starIcons = new UnityEngine.UI.Image[3];
        float[] starOffsets = new float[] { -160f, 0f, 160f };
        float[] starSizes = new float[] { 96f, 122f, 96f };

        for (int i = 0; i < 3; i++)
        {
            UnityEngine.UI.Image star = CreateCircle("Star" + i, resultBox.transform, StarOff, false);
            Anchor(star.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(starOffsets[i], i == 1 ? -170f : -186f), new Vector2(starSizes[i], starSizes[i]));
            starIcons[i] = star;
        }

        TextMeshProUGUI resultScore = CreateText("ScoreText", resultBox.transform, "0 / 0", 38f, TextAlignmentOptions.Center, CreamDim, 2f);
        Anchor(resultScore.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(800f, 56f));

        Button nextButton = CreatePillButton("NextButton", resultBox.transform, "SONRAKİ SEVİYE", Brass, new Color(0.14f, 0.10f, 0.06f), new Vector2(600f, 104f), 38f);
        Anchor(nextButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -420f), new Vector2(600f, 104f));

        Button retryButton = CreatePillButton("RetryButton", resultBox.transform, "TEKRAR OYNA", BrassDeep, Cream, new Vector2(600f, 104f), 38f);
        Anchor(retryButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -550f), new Vector2(600f, 104f));

        Button resultMapButton = CreatePillButton("MapButton", resultBox.transform, "SEVİYE HARİTASI", ChipBrown, Cream, new Vector2(600f, 104f), 38f);
        Anchor(resultMapButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -680f), new Vector2(600f, 104f));

        view = canvasObject.AddComponent<GameHudView>();

        pauseOverlay.gameObject.SetActive(false);
        resultOverlay.gameObject.SetActive(false);

        LastReferences = new GameHudReferences
        {
            LevelLabel = levelLabel,
            ScoreLabel = scoreLabel,
            ShotsLabel = shotsLabel,
            PauseButton = pauseButton,
            PausePanel = pauseOverlay.gameObject,
            ResumeButton = resumeButton,
            PauseRestartButton = pauseRestartButton,
            PauseMapButton = pauseMapButton,
            ResultPanel = resultOverlay.gameObject,
            ResultTitle = resultTitle,
            ResultScore = resultScore,
            StarIcons = starIcons,
            RetryButton = retryButton,
            NextButton = nextButton,
            ResultMapButton = resultMapButton
        };
    }

    public static LevelSelectView BuildLevelSelect(GameObject canvasObject, LevelDatabase database, out LevelCardView[] cards, out TextMeshProUGUI totalStarsLabel, out Button resetButton)
    {
        Transform root = canvasObject.transform;

        UnityEngine.UI.Image background = CreatePanel("Background", root, new Color(0.13f, 0.10f, 0.08f, 1f), 1f, false);
        background.sprite = null;
        Stretch(background.rectTransform);

        TextMeshProUGUI title = CreateText("Title", root, "MİSKETR", 86f, TextAlignmentOptions.Center, Cream, 16f);
        Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(900f, 110f));

        totalStarsLabel = CreateText("TotalStars", root, "0 / 18 YILDIZ", 30f, TextAlignmentOptions.Center, Gold, 8f);
        Anchor(totalStarsLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -320f), new Vector2(900f, 44f));

        int count = database != null ? database.Count : 6;
        cards = new LevelCardView[count];

        const float cardWidth = 460f;
        const float cardHeight = 280f;
        const float gap = 40f;

        for (int i = 0; i < count; i++)
        {
            int column = i % 2;
            int row = i / 2;

            float x = -((cardWidth + gap) * 0.5f) + (column * (cardWidth + gap));
            float y = -420f - (row * (cardHeight + gap));

            UnityEngine.UI.Image card = CreatePanel("LevelCard" + (i + 1), root, new Color(0.21f, 0.16f, 0.12f, 1f), 0.14f, true);
            Anchor(card.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(cardWidth, cardHeight));

            Button cardButton = card.gameObject.AddComponent<Button>();
            cardButton.targetGraphic = card;

            ColorBlock colors = cardButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = Color.white;
            colors.fadeDuration = 0.08f;
            cardButton.colors = colors;

            TextMeshProUGUI numberLabel = CreateText("Number", card.transform, (i + 1).ToString(), 76f, TextAlignmentOptions.TopLeft, Cream, 0f);
            Anchor(numberLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(200f, 92f));

            TextMeshProUGUI nameLabel = CreateText("Name", card.transform, "Mors", 32f, TextAlignmentOptions.BottomLeft, Cream, 2f);
            Anchor(nameLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -118f), new Vector2(400f, 44f));

            UnityEngine.UI.Image[] cardStars = new UnityEngine.UI.Image[3];

            for (int starIndex = 0; starIndex < 3; starIndex++)
            {
                UnityEngine.UI.Image star = CreateCircle("Star" + starIndex, card.transform, StarOff, false);
                Anchor(star.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(34f + (starIndex * 62f), 34f), new Vector2(48f, 48f));
                cardStars[starIndex] = star;
            }

            LevelCardView cardView = card.gameObject.AddComponent<LevelCardView>();

            SerializedObject cardSerialized = new SerializedObject(cardView);
            cardSerialized.FindProperty("button").objectReferenceValue = cardButton;
            cardSerialized.FindProperty("background").objectReferenceValue = card;
            cardSerialized.FindProperty("numberLabel").objectReferenceValue = numberLabel;
            cardSerialized.FindProperty("nameLabel").objectReferenceValue = nameLabel;

            SerializedProperty starsProperty = cardSerialized.FindProperty("stars");
            starsProperty.arraySize = 3;

            for (int starIndex = 0; starIndex < 3; starIndex++)
            {
                starsProperty.GetArrayElementAtIndex(starIndex).objectReferenceValue = cardStars[starIndex];
            }

            cardSerialized.ApplyModifiedPropertiesWithoutUndo();

            cards[i] = cardView;
        }

        resetButton = CreatePillButton("ResetButton", root, "İLERLEMEYİ SIFIRLA", ChipBrown, CreamDim, new Vector2(520f, 88f), 28f);
        Anchor(resetButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(520f, 88f));

        LevelSelectView view = canvasObject.AddComponent<LevelSelectView>();

        return view;
    }
}
