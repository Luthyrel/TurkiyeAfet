using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class PrototypeUI : MonoBehaviour
{
    private PrototypeBootstrap owner;
    private Font font;
    private Canvas canvas;
    private GameObject mainPage;
    private GameObject provincePage;
    private GameObject introPage;
    private GameObject hudPage;
    private GameObject pausePage;
    private GameObject resultPage;
    private Text phaseText;
    private Text taskText;
    private Text scoreText;
    private Text fpsCounterText;
    private float fpsUpdateTimer;
    private int fpsFrameCount;
    private RectTransform hudPanelRect;
    private Text promptText;
    private Text noticeText;
    private Text resultTitle;
    private Text resultScore;
    private Text resultReview;
    private Text provinceHoverText;
    private Text provinceSelectedText;
    private Text provinceScenarioText;
    private Text provinceMapSubtitle;
    private Text introProvinceLabel;
    private Text introScenarioTitle;
    private Text introScenarioInfo;
    private Text quakeStartHint;
    private RectTransform landslideDirectionArrowRect;
    private Transform landslideWorldArrow;
    private Renderer landslideWorldArrowRenderer;
    private RectTransform targetGuideRect;
    private Text targetGuideText;
    private GameObject trappedOverlay;
    private GameObject floodDialogueOverlay;
    private Image trappedOverlayImage;
    private Text trappedTitle;
    private Text trappedQuestion;
    private Text trappedFeedback;
    private Text floodDialogueTitle;
    private Text floodDialogueBody;
    private Text floodDialogueFeedback;
    private Button[] trappedChoiceButtons;
    private Button[] floodDialogueButtons;
    private Button provinceContinueButton;
    private Button resetProgressButton;
    private ProvinceMapView provinceMapView;

    public void Initialize(PrototypeBootstrap bootstrap)
    {
        owner = bootstrap;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject("Türkçe Arayüz", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (EventSystem.current == null)
        {
            GameObject eventObject = new GameObject("Arayüz Olay Sistemi", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventObject.transform.SetParent(transform, false);
        }

        BuildMainPage();
        BuildProvincePage();
        BuildIntroPage();
        BuildHud();
        BuildPausePage();
        BuildResultPage();
        ShowMainPage();
    }

    private void Update()
    {
        if (fpsCounterText != null && hudPage != null && hudPage.activeSelf)
        {
            fpsFrameCount++;
            fpsUpdateTimer += Time.unscaledDeltaTime;
            if (fpsUpdateTimer >= 0.25f)
            {
                float fps = fpsFrameCount / fpsUpdateTimer;
                fpsCounterText.text = "FPS  " + Mathf.RoundToInt(fps);
                fpsFrameCount = 0;
                fpsUpdateTimer = 0f;
            }
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (floodDialogueOverlay != null && floodDialogueOverlay.activeSelf)
        {
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
                owner.Scenario?.SelectFloodDialogueChoice(0);
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
                owner.Scenario?.SelectFloodDialogueChoice(1);
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
                owner.Scenario?.SelectFloodDialogueChoice(2);
            return;
        }

        if (provincePage.activeSelf && keyboard.escapeKey.wasPressedThisFrame)
        {
            PrototypeAudio.PlayUiSelect();
            ShowMainPage();
        }
        else if (introPage.activeSelf)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                PrototypeAudio.PlayUiSelect();
                ShowProvincePage();
            }
            else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                PrototypeAudio.PlayUiSelect();
                owner.StartPlayableScenario();
            }
        }
        else if (pausePage.activeSelf)
        {
            if (keyboard.enterKey.wasPressedThisFrame ||
                keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                PrototypeAudio.PlayUiSelect();
                owner.ResumeGame();
            }
        }
        else if (resultPage.activeSelf && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
        {
            PrototypeAudio.PlayUiSelect();
            ShowMainPage();
        }
        else if (mainPage.activeSelf && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
        {
            PrototypeAudio.PlayUiSelect();
            ShowProvincePage();
        }
    }

    private void BuildMainPage()
    {
        mainPage = MakePage("Ana Menü", new Color32(10, 20, 32, 248));
        AddText(mainPage.transform, "Eyebrow", "T Ü R K İ Y E   A F E T   /   E Ğ İ T İ M   T A T B İ K A T I",
            new Vector2(0.07f, 0.84f), new Vector2(0.90f, 0.94f), 22, new Color32(94, 215, 196, 255), TextAnchor.MiddleLeft);
        AddText(mainPage.transform, "Başlık", "Hazırlık hayat kurtarır.",
            new Vector2(0.07f, 0.56f), new Vector2(0.72f, 0.82f), 58, Color.white, TextAnchor.MiddleLeft);
        AddText(mainPage.transform, "Açıklama",
            "Türkiye'deki afetlere hazırlık için etkileşimli 3D eğitim.\nEvini güvenli hâle getir, sarsıntıda doğru davran, güvenli alana ulaş.",
            new Vector2(0.07f, 0.38f), new Vector2(0.72f, 0.55f), 25, new Color32(198, 211, 224, 255), TextAnchor.UpperLeft);

        RectTransform card = MakeRect(mainPage.transform, "Menü Kartı", new Vector2(0.70f, 0.13f), new Vector2(0.95f, 0.87f));
        AddImage(card.gameObject, new Color32(20, 37, 53, 245));
        AddText(card, "Kart Başlığı", "TATBİKATA HAZIR MISIN?", new Vector2(0.08f, 0.79f), new Vector2(0.92f, 0.94f),
            19, new Color32(123, 220, 204, 255), TextAnchor.MiddleLeft);
        AddText(card, "Kart Açıklaması", "İl seçerek afet farkındalık tatbikatına katıl.",
            new Vector2(0.08f, 0.64f), new Vector2(0.92f, 0.79f), 20, Color.white, TextAnchor.UpperLeft);

        AddButton(card, "Yeni Oyun", "YENİ TATBİKAT", new Vector2(0.08f, 0.47f), new Vector2(0.92f, 0.61f), true,
            ShowProvincePage);
        resetProgressButton = AddButton(card, "İlerlemeyi Sıfırla", "İLERLEMEYİ SIFIRLA",
            new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.44f), false, ResetProgress);
        AddText(card, "Kontroller", "WASD hareket   •   Fare bakış   •   E / Sol tık etkileşim\nShift koş   •   C çök   •   Esc duraklat",
            new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.25f), 16, new Color32(175, 193, 208, 255), TextAnchor.MiddleLeft);
        AddText(mainPage.transform, "Alt Bilgi", "3D OYNANABİLİR PROTOTİP     •     TÜRKÇE     •     WINDOWS",
            new Vector2(0.07f, 0.035f), new Vector2(0.93f, 0.085f), 15, new Color32(112, 137, 158, 255), TextAnchor.MiddleLeft);
    }

    private void BuildProvincePage()
    {
        provincePage = MakePage("İl Seçimi", new Color32(10, 20, 32, 250));
        AddText(provincePage.transform, "Başlık", "Türkiye haritasından il seç",
            new Vector2(0.04f, 0.87f), new Vector2(0.70f, 0.96f), 38, Color.white, TextAnchor.MiddleLeft);
        provinceMapSubtitle = AddText(provincePage.transform, "Harita açıklaması", "İl sınırlarının üzerine gel; tıklayarak veya ok tuşlarıyla seç",
            new Vector2(0.04f, 0.82f), new Vector2(0.70f, 0.88f), 17, new Color32(171, 193, 208, 255), TextAnchor.MiddleLeft);

        RectTransform mapFrame = MakeRect(provincePage.transform, "İl Sınırları Haritası Çerçevesi",
            new Vector2(0.035f, 0.155f), new Vector2(0.705f, 0.81f));
        AddImage(mapFrame.gameObject, new Color32(20, 37, 53, 245));
        RectTransform mapRect = MakeRect(mapFrame, "Türkiye İl Haritası",
            Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));
        RawImage mapImage = mapRect.gameObject.AddComponent<RawImage>();
        mapImage.texture = Resources.Load<Texture2D>("ProvinceMap/turkey-provinces");
        mapImage.color = Color.white;
        mapImage.raycastTarget = false;
        if (mapImage.texture == null)
            Debug.LogError("İl sınır haritası yüklenemedi: Resources/ProvinceMap/turkey-provinces.png");

        RectTransform overlayRect = MakeRect(mapRect, "İl Vurgu Katmanı", Vector2.zero, Vector2.one);
        RawImage overlay = overlayRect.gameObject.AddComponent<RawImage>();
        overlay.color = Color.white;
        overlay.raycastTarget = false;
        provinceMapView = mapRect.gameObject.AddComponent<ProvinceMapView>();
        provinceMapView.ProvinceHovered += HandleProvinceHover;
        provinceMapView.ProvinceSelected += HandleProvinceSelected;
        provinceMapView.ContinueRequested += ShowIntroPage;
        provinceMapView.Initialize(mapRect, overlay);

        RectTransform details = MakeRect(provincePage.transform, "İl Bilgi Kartı",
            new Vector2(0.73f, 0.155f), new Vector2(0.97f, 0.81f));
        AddImage(details.gameObject, new Color32(20, 37, 53, 245));
        AddText(details, "Seçim etiketi", "İl bilgisi", new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.96f),
            17, new Color32(94, 215, 196, 255), TextAnchor.MiddleLeft);
        provinceSelectedText = AddText(details, "İl Adı", "Haritada bir il seç",
            new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.84f), 31, Color.white, TextAnchor.MiddleLeft);
        provinceHoverText = AddText(details, "Harita Önizlemesi", "İl sınırlarının üzerine gelerek adını gör.",
            new Vector2(0.08f, 0.54f), new Vector2(0.92f, 0.65f), 16, new Color32(171, 193, 208, 255), TextAnchor.MiddleLeft);
        provinceScenarioText = AddText(details, "Senaryo Bilgisi", "İl seçimi yapılınca eğitim senaryosu burada gösterilir.",
            new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.52f), 18, new Color32(198, 211, 224, 255), TextAnchor.UpperLeft);
        provinceContinueButton = AddButton(details, "Tatbikata Devam", "SENARYOYU GÖR",
            new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.23f), true, ShowIntroPage);
        provinceContinueButton.interactable = false;
        AddText(details, "Klavye Yardımı", "← / → il seç   •   Enter devam",
            new Vector2(0.08f, 0.025f), new Vector2(0.92f, 0.09f), 15, new Color32(171, 193, 208, 255), TextAnchor.MiddleLeft);

        AddButton(provincePage.transform, "Geri", "ANA MENÜ",
            new Vector2(0.035f, 0.055f), new Vector2(0.155f, 0.12f), false, ShowMainPage);
        AddText(provincePage.transform, "Harita Atfı",
            "İl sınırları: geoBoundaries / OpenStreetMap contributors • CC BY-SA 2.0 • geoboundaries.org",
            new Vector2(0.18f, 0.045f), new Vector2(0.70f, 0.12f), 14, new Color32(142, 164, 180, 255), TextAnchor.MiddleLeft);
    }

    private void BuildIntroPage()
    {
        introPage = MakePage("Senaryo Tanıtımı", new Color32(10, 20, 32, 250));
        introProvinceLabel = AddText(introPage.transform, "Etiket", "İL SEÇİLMEDİ   /   EĞİTİM TATBİKATI",
            new Vector2(0.12f, 0.79f), new Vector2(0.88f, 0.90f), 20, new Color32(94, 215, 196, 255), TextAnchor.MiddleCenter);
        introScenarioTitle = AddText(introPage.transform, "Başlık", "Deprem: Ev içi hazırlık ve güvenli tahliye",
            new Vector2(0.12f, 0.65f), new Vector2(0.88f, 0.80f), 39, Color.white, TextAnchor.MiddleCenter);
        introScenarioInfo = AddText(introPage.transform, "Bilgi",
            "Rafı sabitle, ağır eşyayı aşağı al ve çıkış yolunu aç.\nSarsıntıda sağlam bir eşyanın yanında çök-kapan-tutun.\nSonrasında çevreni kontrol et; gaz kokusu varsa vanayı güvenliyse kapat, kapı ve pencereleri aç ve elektrik düğmelerine dokunmadan toplanma alanına ilerle.",
            new Vector2(0.20f, 0.37f), new Vector2(0.80f, 0.61f), 23, new Color32(198, 211, 224, 255), TextAnchor.MiddleCenter);
        AddButton(introPage.transform, "Başlat", "TATBİKATI BAŞLAT",
            new Vector2(0.36f, 0.20f), new Vector2(0.64f, 0.32f), true, owner.StartPlayableScenario);
        AddButton(introPage.transform, "Geri", "HARİTAYA DÖN",
            new Vector2(0.38f, 0.08f), new Vector2(0.62f, 0.17f), false, ShowProvincePage);
    }

    private void BuildHud()
    {
        hudPage = MakePage("Oyun Arayüzü", new Color(0f, 0f, 0f, 0f));
        RectTransform panel = MakeRect(hudPage.transform, "Görev Paneli", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(26f, -24f), new Vector2(430f, 300f), new Color(0f, 0f, 0f, 0f));
        hudPanelRect = panel;
        AddImage(panel.gameObject, new Color32(10, 20, 32, 220));
        phaseText = AddText(panel, "Aşama", "HAZIRLIK", new Vector2(0.06f, 0.78f), new Vector2(0.94f, 0.95f),
            19, new Color32(94, 215, 196, 255), TextAnchor.MiddleLeft);
        taskText = AddText(panel, "Görevler", "", new Vector2(0.06f, 0.25f), new Vector2(0.94f, 0.76f),
            18, Color.white, TextAnchor.UpperLeft);
        taskText.supportRichText = true;
        scoreText = AddText(panel, "Puan", "PUAN  0", new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.21f),
            17, new Color32(255, 206, 123, 255), TextAnchor.MiddleLeft);

        RectTransform fpsPanel = MakeRect(hudPage.transform, "FPS Sayacı", new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-24f, 24f), new Vector2(130f, 38f), new Color32(10, 20, 32, 190));
        fpsCounterText = AddText(fpsPanel, "FPS Değeri", "FPS  --", new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.96f),
            16, new Color32(235, 248, 244, 255), TextAnchor.MiddleCenter);

        RectTransform prompt = MakeRect(hudPage.transform, "Etkileşim İpucu", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 70f), new Vector2(800f, 54f), new Color32(10, 20, 32, 225));
        promptText = AddText(prompt, "İpucu", "", new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f),
            21, Color.white, TextAnchor.MiddleCenter);

        AddText(hudPage.transform, "Nişangâh", "•", new Vector2(0.49f, 0.48f), new Vector2(0.51f, 0.52f),
            26, new Color32(255, 255, 255, 220), TextAnchor.MiddleCenter);
        noticeText = AddText(hudPage.transform, "Bilgilendirme", "", new Vector2(0.20f, 0.20f), new Vector2(0.80f, 0.30f),
            21, new Color32(255, 225, 164, 255), TextAnchor.MiddleCenter);
        quakeStartHint = AddText(hudPage.transform, "Deprem Geri Sayımı", "",
            new Vector2(0.70f, 0.86f), new Vector2(0.98f, 0.94f), 17,
            new Color32(255, 225, 164, 255), TextAnchor.MiddleRight);
        landslideDirectionArrowRect = MakeRect(hudPage.transform, "Heyelan Güvenli Alan Yön Oku",
            new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(96f, 72f), new Color32(10, 20, 32, 210));
        AddText(landslideDirectionArrowRect, "Yön Oku", "▲", Vector2.zero, Vector2.one, 58, new Color32(94, 235, 195, 255), TextAnchor.MiddleCenter);
        landslideDirectionArrowRect.gameObject.SetActive(false);

        targetGuideRect = MakeRect(hudPage.transform, "Aktif Görev Yön İşareti",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 42f),
            new Color32(10, 20, 32, 215));
        targetGuideText = AddText(targetGuideRect, "Hedef Adı", "", Vector2.zero, Vector2.one, 16,
            new Color32(235, 248, 244, 255), TextAnchor.MiddleCenter);
        targetGuideRect.gameObject.SetActive(false);
        BuildFloodDialogueOverlay();
        BuildTrappedOverlay();
    }

    private void BuildFloodDialogueOverlay()
    {
        RectTransform overlay = MakeRect(hudPage.transform, "112 ve Resmî Duyuru Diyaloğu", Vector2.zero, Vector2.one);
        floodDialogueOverlay = overlay.gameObject;
        AddImage(floodDialogueOverlay, new Color32(5, 12, 20, 205));

        RectTransform card = MakeRect(overlay, "Diyalog Kartı", new Vector2(0.18f, 0.17f), new Vector2(0.82f, 0.83f));
        AddImage(card.gameObject, new Color32(18, 34, 48, 250));
        floodDialogueTitle = AddText(card, "Diyalog Başlığı", "112 ACİL ÇAĞRI MERKEZİ",
            new Vector2(0.07f, 0.84f), new Vector2(0.93f, 0.96f), 28,
            new Color32(94, 215, 196, 255), TextAnchor.MiddleCenter);
        floodDialogueBody = AddText(card, "Operatör Duyurusu", "",
            new Vector2(0.09f, 0.62f), new Vector2(0.91f, 0.83f), 22,
            Color.white, TextAnchor.MiddleCenter);
        floodDialogueFeedback = AddText(card, "Diyalog Geri Bildirimi", "",
            new Vector2(0.09f, 0.54f), new Vector2(0.91f, 0.62f), 17,
            new Color32(255, 206, 123, 255), TextAnchor.MiddleCenter);

        floodDialogueButtons = new Button[3];
        float[] buttonBottoms = { 0.39f, 0.24f, 0.09f };
        for (int i = 0; i < floodDialogueButtons.Length; i++)
        {
            int choice = i;
            floodDialogueButtons[i] = AddButton(card, "Diyalog Yanıtı " + (i + 1), "",
                new Vector2(0.08f, buttonBottoms[i]), new Vector2(0.92f, buttonBottoms[i] + 0.12f),
                false, () => owner.Scenario?.SelectFloodDialogueChoice(choice));
        }
        AddText(card, "Diyalog Kontrol İpucu", "Yanıtı fareyle seç veya 1 / 2 / 3 tuşuna bas",
            new Vector2(0.08f, 0.015f), new Vector2(0.92f, 0.075f), 14,
            new Color32(172, 191, 205, 255), TextAnchor.MiddleCenter);
        floodDialogueOverlay.SetActive(false);
    }

    public void ShowFloodDialogue(string title, string body, string[] options, string feedback)
    {
        if (floodDialogueOverlay == null)
            return;

        floodDialogueTitle.text = title;
        floodDialogueBody.text = body;
        floodDialogueFeedback.text = feedback;
        for (int i = 0; i < floodDialogueButtons.Length; i++)
        {
            bool visible = options != null && i < options.Length;
            floodDialogueButtons[i].gameObject.SetActive(visible);
            if (visible)
                floodDialogueButtons[i].GetComponentInChildren<Text>().text = (i + 1) + "   " + options[i];
        }
        floodDialogueOverlay.SetActive(true);
        floodDialogueOverlay.transform.SetAsLastSibling();
    }

    public void HideFloodDialogue()
    {
        if (floodDialogueOverlay != null)
            floodDialogueOverlay.SetActive(false);
    }

    private void BuildTrappedOverlay()
    {
        RectTransform panel = MakeRect(hudPage.transform, "Enkaz Altı Ekranı", Vector2.zero, Vector2.one);
        trappedOverlay = panel.gameObject;
        trappedOverlayImage = AddImage(trappedOverlay, Color.black);
        trappedOverlay.transform.SetAsLastSibling();
        trappedTitle = AddText(panel, "Enkaz Başlığı", "ENKAZ ALTINDASIN", new Vector2(0.10f, 0.79f),
            new Vector2(0.90f, 0.91f), 34, new Color32(255, 206, 123, 255), TextAnchor.MiddleCenter);
        trappedQuestion = AddText(panel, "Enkaz Sorusu", "", new Vector2(0.12f, 0.66f),
            new Vector2(0.88f, 0.78f), 24, Color.white, TextAnchor.MiddleCenter);
        trappedChoiceButtons = new Button[3];
        float[] top = { 0.62f, 0.47f, 0.32f };
        for (int i = 0; i < trappedChoiceButtons.Length; i++)
        {
            int choiceIndex = i;
            trappedChoiceButtons[i] = AddButton(panel, "Enkaz Seçeneği " + (i + 1), "",
                new Vector2(0.16f, top[i] - 0.11f), new Vector2(0.84f, top[i]), true,
                () => owner.Scenario?.SelectTrappedAnswer(choiceIndex));
        }
        trappedFeedback = AddText(panel, "Enkaz Geri Bildirimi", "", new Vector2(0.12f, 0.11f),
            new Vector2(0.88f, 0.19f), 18, new Color32(174, 230, 218, 255), TextAnchor.MiddleCenter);
        trappedOverlay.SetActive(false);
    }


    private void BuildPausePage()
    {
        pausePage = MakePage("Duraklatma", new Color32(7, 15, 25, 238));

        AddText(pausePage.transform, "Başlık", "OYUN DURAKLATILDI",
            new Vector2(0.22f, 0.68f), new Vector2(0.78f, 0.82f),
            42, Color.white, TextAnchor.MiddleCenter);

        AddButton(pausePage.transform, "Devam", "Oyuna Devam Et",
            new Vector2(0.35f, 0.51f), new Vector2(0.65f, 0.60f),
            true, owner.ResumeGame);

        AddButton(pausePage.transform, "Yeniden Başlat", "Yeniden Başlat",
            new Vector2(0.35f, 0.40f), new Vector2(0.65f, 0.49f),
            false, owner.RestartToMenu);

        AddButton(pausePage.transform, "Menüye Geri Dön", "Menüye Geri Dön",
            new Vector2(0.35f, 0.29f), new Vector2(0.65f, 0.38f),
            false, owner.RestartToMenu);

        AddButton(pausePage.transform, "Oyundan Çık", "Oyundan Çık",
            new Vector2(0.35f, 0.18f), new Vector2(0.65f, 0.27f),
            false, owner.QuitGame);
    }

    private void BuildResultPage()
    {
        resultPage = MakePage("Sonuç Ekranı", new Color32(7, 15, 25, 245));
        resultTitle = AddText(resultPage.transform, "Başlık", "Tatbikat tamamlandı",
            new Vector2(0.17f, 0.78f), new Vector2(0.83f, 0.90f), 42, Color.white, TextAnchor.MiddleCenter);
        resultScore = AddText(resultPage.transform, "Sonuç Puanı", "0 PUAN",
            new Vector2(0.30f, 0.65f), new Vector2(0.70f, 0.77f), 30, new Color32(255, 206, 123, 255), TextAnchor.MiddleCenter);
        resultReview = AddText(resultPage.transform, "Değerlendirme", "",
            new Vector2(0.20f, 0.35f), new Vector2(0.80f, 0.62f), 21, new Color32(198, 211, 224, 255), TextAnchor.MiddleCenter);
        AddButton(resultPage.transform, "Tekrar", "TEKRAR OYNA", new Vector2(0.37f, 0.18f), new Vector2(0.63f, 0.29f),
            true, owner.RestartToMenu);
        AddButton(resultPage.transform, "Menü", "ANA MENÜ", new Vector2(0.39f, 0.07f), new Vector2(0.61f, 0.16f),
            false, owner.RestartToMenu);
    }

    public void ShowMainPage()
    {
        SetPages(mainPage);
        GameManager.Instance?.ReturnToMenu();
        owner.SetPlayerControls(false);
    }

    private void ResetProgress()
    {
        ProvinceManager.Instance?.ResetCompletionProgress();
        provinceMapView?.RefreshProgress();
        if (resetProgressButton != null)
            resetProgressButton.GetComponentInChildren<Text>().text = "İLERLEMEYİ SIFIRLA";
    }

    private void ShowProvincePage()
    {
        SetPages(provincePage);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        GameManager.Instance?.StartProvinceSelection();
        owner.SetPlayerControls(false);
        provinceMapView.RefreshProgress();
        if (ProvinceManager.Instance != null && ProvinceManager.Instance.SelectedProvince != null)
            provinceMapView.FocusAndSelect(ProvinceManager.Instance.SelectedProvince.plateCode);
    }

    private void ShowIntroPage()
    {
        ProvinceInfo selected = ProvinceManager.Instance != null ? ProvinceManager.Instance.SelectedProvince : null;
        if (selected == null)
        {
            owner.ShowNotice("Önce Türkiye haritasından bir il seç.");
            return;
        }

        GameManager.DisasterType disaster = GameManager.Instance != null
            ? GameManager.Instance.SelectedDisaster : GameManager.DisasterType.Earthquake;
        bool flood = disaster == GameManager.DisasterType.Flood;
        bool landslide = disaster == GameManager.DisasterType.Landslide;
        bool wildfire = disaster == GameManager.DisasterType.Wildfire;
        bool storm = disaster == GameManager.DisasterType.Storm;
        bool tornado = disaster == GameManager.DisasterType.Tornado;
        introProvinceLabel.text = selected.name + "  •  PLAKA " + selected.PlateLabel + "  /  EĞİTİM TATBİKATI";
        introScenarioTitle.text = tornado ? "Hortum: hazırlık yap, iç odada korun ve güvenle yardım et"
            : flood ? "Sel ve taşkın: evden yüksek ve güvenli alana"
            : landslide ? "Heyelan: yamaçtan yana doğru güvenli alana"
            : wildfire ? "Orman yangını: dumanı bildir, güvenli alana tahliye ol"
            : storm ? "Fırtına: dışarıdaki eşyaları sabitle, sağlam yapıda korun"
            : selected.trainingScenarioTitle;
        introScenarioInfo.text = tornado
            ? "Hortum uyarısı öncesinde ilk yardım setini ve acil çantayı pencerelerden uzak iç bölüme taşı; bahçe sandalyesi ve masasını eve al.\n\nHORTUM UYARISI – GÜVENLİ ALANA GEÇİN! Hortumun mahalle yolundan geçişini güvenli iç bölümde bekle; başını ve boynunu koru, resmî güvenli uyarısı gelene kadar çıkma.\n\nHortum sonrasında hasarlı yapılar ve kopmuş hatlar için güvenli mesafeden kontrol yap; 112'ye bilgi ver ve tehlikesiz biçimde yardım bekleyen kişiyi yönlendir.\n\nBu, il için tehlike tahmini değil; afet farkındalık tatbikatıdır."
            : flood
            ? "Taşkın uyarısını izle, değerli eşyaları ve ilaçları yukarı kaldır; su basma riski varsa elektriği güvenli biçimde kes.\nSu yükselirken araca binme veya akıntıya girme; evin üst katına ya da belirlenmiş yüksek alana çık.\nSu çekilmeden geri dönme; kirli suya ve elektrik hatlarına yaklaşma, resmî duyuruları takip et.\n\nBu, il için tehlike tahmini değil; afet farkındalık tatbikatıdır."
            : landslide
            ? "Yağış sonrası yamaçta yeni çatlak, su sızıntısı, eğilen ağaç/direk veya alışılmadık sesleri fark edersen bölgeden uzaklaş ve 112'ye bildir.\nHeyelan başlarsa akış hattına yaklaşma; mümkünse hareket yönüne dik biçimde yan tarafa ve daha güvenli yüksek zemine ilerle. Vadiye, dere yatağına ve heyelan alanına girme.\nTehlike geçince geri dönme; resmî ekiplerin ve duyuruların yönlendirmesini bekle.\n\nBu, il için tehlike tahmini değil; afet farkındalık tatbikatıdır."
            : wildfire
            ? "Dumanı veya alevi güvenli bir mesafeden fark edersen konumu 112'ye bildir. Yangına müdahale etmeye çalışma; yetkililerin tahliye uyarılarını izle ve güvenli güzergâhtan açık alana çık. Toplanma alanında kal, yangın bölgesine geri dönme.\n\nBu, il için tehlike tahmini değil; afet farkındalık tatbikatıdır."
            : storm
            ? "Fırtına öncesinde dışarıdaki hafif eşyaları sabitle ve panjurları kapat. Uyarı sırasında sağlam bir binanın içine gir; pencerelerden, ağaçlardan, direklerden ve kıyıdaki dalga sıçrama alanından uzak iç odada kal.\nFırtına geçince kopmuş elektrik hatlarına yaklaşma; güvenli bir yerden 112'ye konum ve tehlikeyi bildir.\n\nBu, il için tehlike tahmini değil; afet farkındalık tatbikatıdır."
            : "Rafı sabitle, ağır eşyayı aşağı al ve çıkış yolunu aç.\nSarsıntıda sağlam bir eşyanın yanında çök-kapan-tutun.\nSonrasında çevreni kontrol et; gaz kokusu varsa vanayı güvenliyse kapat, kapı ve pencereleri aç ve elektrik düğmelerine dokunmadan toplanma alanına iler.\n\n" + selected.trainingScenarioNote;
        SetPages(introPage);
        owner.SetPlayerControls(false);
    }

    private void HandleProvinceHover(int plateCode)
    {
        if (provinceHoverText == null)
            return;

        if (plateCode < 0)
        {
            provinceHoverText.text = "Deniz alanı • il sınırının içine tıkla.";
            RestoreProvinceSelectionInfo();
        }
        else if (plateCode == 0)
        {
            provinceHoverText.text = "Harita üzerinde bir il sınırının üzerine gel.";
            RestoreProvinceSelectionInfo();
        }
        else
        {
            ProvinceInfo province = ProvinceManager.Instance != null ? ProvinceManager.Instance.GetProvince(plateCode) : null;
            if (province == null)
            {
                provinceHoverText.text = string.Empty;
                return;
            }

            GameManager.DisasterType disaster = ProvinceManager.Instance.GetPrimaryDisasterForProvince(plateCode);
            bool flood = disaster == GameManager.DisasterType.Flood;
            bool landslide = disaster == GameManager.DisasterType.Landslide;
            bool wildfire = disaster == GameManager.DisasterType.Wildfire;
            bool storm = disaster == GameManager.DisasterType.Storm;
            provinceHoverText.text = "ÖNİZLEME • " + province.name + " / " + province.PlateLabel
                + " • AFET: " + (disaster == GameManager.DisasterType.Tornado ? "HORTUM" : flood ? "SEL / TAŞKIN" : landslide ? "HEYELAN" : wildfire ? "ORMAN YANGINI" : storm ? "FIRTINA" : "DEPREM");
            DisplayProvinceInfo(plateCode, false);
        }
    }

    private void HandleProvinceSelected(int plateCode)
    {
        if (plateCode <= 0)
        {
            RestoreProvinceSelectionInfo();
            return;
        }
        DisplayProvinceInfo(plateCode, true);
    }

    private void DisplayProvinceInfo(int plateCode, bool selected)
    {
        ProvinceInfo province = ProvinceManager.Instance != null ? ProvinceManager.Instance.GetProvince(plateCode) : null;
        if (province == null)
            return;

        GameManager.DisasterType disaster = ProvinceManager.Instance.GetPrimaryDisasterForProvince(plateCode);
        if (selected)
            GameManager.Instance?.SelectDisaster(disaster);
        bool flood = disaster == GameManager.DisasterType.Flood;
        bool landslide = disaster == GameManager.DisasterType.Landslide;
        bool wildfire = disaster == GameManager.DisasterType.Wildfire;
        bool storm = disaster == GameManager.DisasterType.Storm;
        bool tornado = disaster == GameManager.DisasterType.Tornado;
        provinceSelectedText.text = province.name + "  •  PLAKA " + province.PlateLabel;
        bool completed = ProvinceManager.Instance.IsScenarioCompleted(plateCode, disaster);
        provinceScenarioText.text = "Afet türü: " + (tornado ? "Hortum" : flood ? "Sel ve taşkın" : landslide ? "Heyelan" : wildfire ? "Orman yangını" : storm ? "Fırtına" : "Deprem")
            + "\n" + (tornado ? "Hortum: hazırlık yap, pencerelerden uzak iç bölüme sığın, hasarı bildir"
                : flood ? "Sel ve taşkın: güvenli tahliye"
                : landslide ? "Heyelan: işaretleri fark et, akış hattından dik yönde uzaklaş"
                : wildfire ? "Orman yangını: dumanı bildir, açık alana tahliye ol"
                : storm ? "Fırtına: eşyaları sabitle, iç odada korun ve tehlikeyi bildir"
                : province.trainingScenarioTitle)
            + (completed ? "\n\n✓ Bu afet türü tamamlandı — ilgili iller gri; tekrar seçilebilir." : "\n\n" + (tornado
                ? "Hortum farkındalık tatbikatı için Konya eğitim ili; resmî risk sınıflaması değildir."
                : flood
                ? "Taşkın tehlikesine ve geçmiş taşkın olaylarına göre seçilmiş eğitim ili."
                : landslide ? "Heyelan farkındalık tatbikatı için seçilmiş eğitim ili; resmî risk sınıflaması değildir."
                : wildfire ? "Orman yangını farkındalık tatbikatı için seçilmiş eğitim ili; resmî risk sınıflaması değildir."
                : storm ? "Fırtına farkındalık tatbikatı için seçilmiş eğitim ili; resmî risk sınıflaması değildir."
                : province.trainingScenarioNote));
        provinceContinueButton.interactable = selected;
    }

    private void RestoreProvinceSelectionInfo()
    {
        int selectedPlate = provinceMapView != null ? provinceMapView.SelectedPlate : 0;
        if (selectedPlate > 0)
        {
            DisplayProvinceInfo(selectedPlate, true);
            return;
        }

        provinceSelectedText.text = "Haritada bir il seç";
        provinceScenarioText.text = "İl seçtiğinde o il için belirlenen yaygın afet burada gösterilir. Gri iller tamamlandı; yeniden seçilebilir.";
        provinceContinueButton.interactable = false;
    }

    public void ShowHud()
    {
        SetPages(hudPage);
    }

    public void ShowPause()
    {
        SetPages(pausePage);

        pausePage.transform.SetAsLastSibling();
        canvas.sortingOrder = 100;

        owner.SetPlayerControls(false);
    }
    public void ShowResults(int score, string review, string title = "Tatbikat tamamlandı")
    {
        resultTitle.text = title;
        resultScore.text = "PUAN   " + score + " puan";
        resultReview.text = review;
        SetPages(resultPage);
        owner.SetPlayerControls(false);
    }

    public void RefreshHud(string phase, string tasks, string shelter, string valve, string windows, string exit, int score, float holdProgress, float preparationCountdown)
    {
        if (phaseText == null)
            return;
        phaseText.text = phase;
        string fullTasks;
        if (phase.StartsWith("DEPREM ÖNCESİ"))
            fullTasks = tasks;
        else if (phase.StartsWith("SEL") || phase.StartsWith("ORMAN YANGINI") || phase.StartsWith("FIRTINA") || phase.StartsWith("HORTUM"))
            fullTasks = tasks;
        else if (phase.StartsWith("DEPREM ANI"))
        {
            PrototypeScenario scenario = owner != null ? owner.Scenario : null;
            bool crouchDone = scenario != null && scenario.CrouchConditionComplete;
            bool holdDone = scenario != null && scenario.TableHoldConditionComplete;
            bool holding = scenario != null && scenario.IsTableHoldActive;
            string crouch = (crouchDone ? "✓" : "○") + " Çökme: güvenli bölgede C ile çök";
            string hold = holdDone
                ? "✓ Masaya tutunma: tamamlandı"
                : holding ? "◉ Masaya tutunma: sürüyor"
                : "○ Masaya tutunma: E veya sol tıkla başlat";
            string progress = holdProgress > 0f && !holdDone
                ? "\nGüvenli pozisyonu koru: " + Mathf.RoundToInt(holdProgress * 100f) + "%"
                : string.Empty;
            fullTasks = "ÇÖK-KAPAN-TUTUN\n" + crouch + "\n" + hold + progress
                + "\n\nÖnce ışıklı halkaya geç, sonra çök ve sarsıntı boyunca masaya tutun.";
        }
        else if (phase.StartsWith("ENKAZ ALTINDA"))
            fullTasks = "Enkaz altında güvenli davranış adımlarını tamamla.";
        else
            fullTasks = shelter + "\n" + valve + "\n" + windows + "\n" + exit;
        taskText.text = fullTasks;
        if (hudPanelRect != null)
        {
            // Long flood warnings/tasks must push the score row downward, not overlap it.
            float requiredPanelHeight = (taskText.preferredHeight + 24f) / 0.55f;
            hudPanelRect.sizeDelta = new Vector2(430f, Mathf.Clamp(requiredPanelHeight, 300f, 560f));
        }
        scoreText.text = "PUAN   " + score;
        PrototypeScenario currentScenario = owner != null ? owner.Scenario : null;
        if (preparationCountdown > 0.05f && currentScenario != null && !currentScenario.IsLandslideScenario)
        {
            string disasterName = currentScenario.IsTornadoScenario ? "HORTUM "
                : currentScenario.IsStormScenario ? "FIRTINA "
                : currentScenario.IsWildfireScenario ? "ORMAN YANGINI "
                : currentScenario.IsFloodScenario ? "SEL " : "DEPREM ";
            quakeStartHint.text = disasterName + Mathf.CeilToInt(preparationCountdown) + " SANİYE İÇİNDE BAŞLIYOR";
        }
        else
            quakeStartHint.text = string.Empty;
    }

    public void SetLandslideDirectionArrow(bool visible, Vector3 targetPosition)
    {
        if (landslideDirectionArrowRect != null)
            landslideDirectionArrowRect.gameObject.SetActive(false);
        Camera camera = Camera.main;
        if (camera == null) return;
        if (landslideWorldArrow == null)
            CreateLandslideWorldArrow(camera.transform);
        if (landslideWorldArrow.parent != camera.transform)
            landslideWorldArrow.SetParent(camera.transform, false);
        landslideWorldArrow.localPosition = new Vector3(0f, -0.43f, 1.35f);
        if (landslideWorldArrowRenderer != null)
            landslideWorldArrowRenderer.enabled = visible;
        if (!visible) return;

        // Use compass bearing on the ground plane. Camera pitch and the safe-zone
        // elevation must not make the direction indicator swing up or down.
        Vector3 toTarget = targetPosition - camera.transform.position;
        toTarget.y = 0f;
        Vector3 horizontalForward = camera.transform.forward;
        horizontalForward.y = 0f;
        Vector3 horizontalRight = camera.transform.right;
        horizontalRight.y = 0f;
        if (horizontalForward.sqrMagnitude > 0.0001f) horizontalForward.Normalize();
        if (horizontalRight.sqrMagnitude > 0.0001f) horizontalRight.Normalize();
        Vector2 projected = new Vector2(Vector3.Dot(toTarget, horizontalRight),
            Vector3.Dot(toTarget, horizontalForward));
        if (projected.sqrMagnitude < 0.0001f) projected = Vector2.up;
        float angle = Mathf.Atan2(projected.y, projected.x) * Mathf.Rad2Deg - 90f;
        landslideWorldArrow.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void CreateLandslideWorldArrow(Transform cameraTransform)
    {
        GameObject arrow = new GameObject("3B Heyelan Güvenli Alan Oku");
        arrow.transform.SetParent(cameraTransform, false);
        Mesh mesh = new Mesh { name = "Kabartmalı Yön Oku" };
        Vector2[] outline =
        {
            new Vector2(-0.11f, -0.32f), new Vector2(0.11f, -0.32f),
            new Vector2(0.11f, 0.04f), new Vector2(0.29f, 0.04f),
            new Vector2(0f, 0.43f), new Vector2(-0.29f, 0.04f),
            new Vector2(-0.11f, 0.04f)
        };
        Vector3[] vertices = new Vector3[16];
        for (int i = 0; i < 7; i++)
        {
            vertices[i] = new Vector3(outline[i].x, outline[i].y, -0.035f);
            vertices[i + 8] = new Vector3(outline[i].x, outline[i].y, 0.035f);
        }
        vertices[7] = new Vector3(0f, 0.02f, -0.035f);
        vertices[15] = new Vector3(0f, 0.02f, 0.035f);
        int[] triangles = new int[7 * 3 * 2 + 7 * 6];
        int t = 0;
        for (int i = 0; i < 7; i++)
        {
            int next = (i + 1) % 7;
            triangles[t++] = 7; triangles[t++] = i; triangles[t++] = next;
            triangles[t++] = 15; triangles[t++] = next + 8; triangles[t++] = i + 8;
            triangles[t++] = i; triangles[t++] = i + 8; triangles[t++] = next + 8;
            triangles[t++] = i; triangles[t++] = next + 8; triangles[t++] = next;
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        arrow.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer meshRenderer = arrow.AddComponent<MeshRenderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader != null)
        {
            Material material = new Material(shader);
            material.name = "Heyelan Oku - Fosforlu Turkuaz";
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color32(73, 255, 210, 255));
            else if (material.HasProperty("_Color")) material.SetColor("_Color", new Color32(73, 255, 210, 255));
            meshRenderer.sharedMaterial = material;
        }
        landslideWorldArrow = arrow.transform;
        landslideWorldArrowRenderer = meshRenderer;
    }

    public void UpdateTargetGuide(Camera camera, Vector3 targetPosition, bool unobstructed,
        string targetDisplayName, string actionName, float distance)
    {
        if (targetGuideRect == null || camera == null)
            return;

        Vector3 viewport = camera.WorldToViewportPoint(targetPosition);
        bool behind = viewport.z <= 0f;
        if (behind)
        {
            viewport.x = 1f - viewport.x;
            viewport.y = 1f - viewport.y;
        }

        bool onScreen = !behind && unobstructed && viewport.x >= 0.08f && viewport.x <= 0.92f
            && viewport.y >= 0.12f && viewport.y <= 0.82f;
        targetGuideRect.gameObject.SetActive(true);
        if (onScreen)
        {
            targetGuideRect.anchoredPosition = new Vector2((viewport.x - 0.5f) * 1920f, (viewport.y - 0.5f) * 1080f + 34f);
            targetGuideRect.sizeDelta = new Vector2(390f, 42f);
            targetGuideText.text = "◆ HEDEF  " + targetDisplayName + "  •  " + actionName + "  •  " + distance.ToString("0") + " m";
        }
        else
        {
            Vector2 direction = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.up;
            direction.Normalize();
            float tx = Mathf.Abs(direction.x) > 0.001f ? 0.40f / Mathf.Abs(direction.x) : float.PositiveInfinity;
            float ty = Mathf.Abs(direction.y) > 0.001f ? 0.34f / Mathf.Abs(direction.y) : float.PositiveInfinity;
            Vector2 edge = new Vector2(0.5f, 0.5f) + direction * Mathf.Min(tx, ty);
            targetGuideRect.anchoredPosition = new Vector2((edge.x - 0.5f) * 1920f, (edge.y - 0.5f) * 1080f);
            targetGuideRect.sizeDelta = new Vector2(390f, 42f);
            targetGuideText.text = DirectionArrow(direction) + "  " + targetDisplayName + "  •  " + actionName + "  •  " + distance.ToString("0") + " m";
        }
    }

    public void HideTargetGuide()
    {
        if (targetGuideRect != null)
            targetGuideRect.gameObject.SetActive(false);
    }

    private string DirectionArrow(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        int index = Mathf.RoundToInt(angle / 45f);
        string[] arrows = { "→", "↗", "↑", "↖", "←", "↙", "↓", "↘" };
        index = (index + 8) % 8;
        return arrows[index];
    }

    public void SetPrompt(string prompt)
    {
        if (promptText != null)
            promptText.text = prompt;
    }

    public void SetNotice(string message)
    {
        if (noticeText != null)
            noticeText.text = message;
    }

    public void ShowBlackout()
    {
        trappedOverlay.SetActive(true);
        trappedOverlayImage.color = Color.black;
        trappedTitle.text = string.Empty;
        trappedQuestion.text = string.Empty;
        trappedFeedback.text = string.Empty;
        foreach (Button button in trappedChoiceButtons)
            button.gameObject.SetActive(false);
    }

    public void ShowTrappedQuestion(int step, string question, string[] choices, string feedback)
    {
        trappedOverlay.SetActive(true);
        trappedOverlayImage.color = Color.black;
        int totalSteps = owner != null && owner.Scenario != null ? owner.Scenario.TrappedQuestionCount : step;
        trappedTitle.text = "ENKAZ ALTINDASIN  •  ADIM " + step + "/" + totalSteps;
        trappedQuestion.text = question;
        trappedFeedback.text = feedback;
        for (int i = 0; i < trappedChoiceButtons.Length; i++)
        {
            Button button = trappedChoiceButtons[i];
            button.gameObject.SetActive(true);
            button.interactable = true;
            button.GetComponentInChildren<Text>().text = (i + 1) + "   " + choices[i];
        }
    }

    public void SetTrappedFeedback(string feedback)
    {
        if (trappedFeedback != null)
            trappedFeedback.text = feedback;
    }

    private GameObject MakePage(string name, Color color)
    {
        RectTransform rect = MakeRect(canvas.transform, name, Vector2.zero, Vector2.one);
        AddImage(rect.gameObject, color);
        return rect.gameObject;
    }

    private RectTransform MakeRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private RectTransform MakeRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rect = MakeRect(parent, name, anchorMin, anchorMax);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return rect;
    }

    private RectTransform MakeRect(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        return rect;
    }

    private Image AddImage(GameObject go, Color color)
    {
        Image image = go.GetComponent<Image>();
        if (image == null)
            image = go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private Text AddText(Transform parent, string name, string value, Vector2 anchorMin, Vector2 anchorMax, int size, Color color, TextAnchor alignment)
    {
        RectTransform rect = MakeRect(parent, name, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = false;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(12, size - 4);
        text.resizeTextMaxSize = size;
        return text;
    }

    private Button AddButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, bool primary, Action action)
    {
        RectTransform rect = MakeRect(parent, name, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = primary ? new Color32(35, 145, 127, 255) : new Color32(39, 59, 78, 255);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(220, 255, 247, 255);
        colors.pressedColor = new Color32(174, 230, 218, 255);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.12f;
        button.colors = colors;
        button.onClick.AddListener(() =>
        {
            PrototypeAudio.PlayUiSelect();
            action?.Invoke();
        });
        AddText(rect, "Metin", label, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.96f),
            18, Color.white, TextAnchor.MiddleCenter);
        return button;
    }

    private void SetPages(GameObject active)
    {
        mainPage.SetActive(active == mainPage);
        provincePage.SetActive(active == provincePage);
        introPage.SetActive(active == introPage);
        hudPage.SetActive(active == hudPage);
        pausePage.SetActive(active == pausePage);
        resultPage.SetActive(active == resultPage);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}





