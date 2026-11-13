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
    private Text promptText;
    private Text noticeText;
    private Text resultTitle;
    private Text resultScore;
    private Text resultReview;
    private Text provinceHoverText;
    private Text provinceSelectedText;
    private Text provinceScenarioText;
    private Text provinceMapSubtitle;
    private Button earthquakeModeButton;
    private Button floodModeButton;
    private Text introProvinceLabel;
    private Text introScenarioTitle;
    private Text introScenarioInfo;
    private Text quakeStartHint;
    private RectTransform targetGuideRect;
    private Text targetGuideText;
    private GameObject trappedOverlay;
    private Image trappedOverlayImage;
    private Text trappedTitle;
    private Text trappedQuestion;
    private Text trappedFeedback;
    private Button[] trappedChoiceButtons;
    private Button provinceContinueButton;
    private Button continueButton;
    private ProvinceMapView provinceMapView;
    private bool hasSavedProgress;

    public void Initialize(PrototypeBootstrap bootstrap)
    {
        owner = bootstrap;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

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

        hasSavedProgress = ProvinceManager.Instance != null && GameManager.Instance != null
            && ProvinceManager.Instance.HasAnyCompletedScenario(GameManager.Instance.SelectedDisaster);
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
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

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
        AddText(card, "Kart Açıklaması", "Afet türünü ve ili seç; bağımsız 3D farkındalık tatbikatına katıl.",
            new Vector2(0.08f, 0.64f), new Vector2(0.92f, 0.79f), 20, Color.white, TextAnchor.UpperLeft);

        AddButton(card, "Yeni Oyun", "YENİ TATBİKAT", new Vector2(0.08f, 0.47f), new Vector2(0.92f, 0.61f), true,
            ShowProvincePage);
        continueButton = AddButton(card, "Devam Et", hasSavedProgress ? "KAYITLI İLERLEME" : "KAYIT YOK",
            new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.44f), false, ShowSavedProgress);
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
        provinceMapSubtitle = AddText(provincePage.transform, "Harita açıklaması", "81 il • Fareyle üzerine gel, tıklayarak seç veya ok tuşlarıyla ilerle",
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
        AddText(details, "Afet Türü Etiketi", "Afet türü", new Vector2(0.08f, 0.89f), new Vector2(0.92f, 0.98f),
            17, new Color32(94, 215, 196, 255), TextAnchor.MiddleLeft);
        earthquakeModeButton = AddButton(details, "Deprem Türü", "DEPREM",
            new Vector2(0.08f, 0.78f), new Vector2(0.48f, 0.88f), false,
            () => SelectDisaster(GameManager.DisasterType.Earthquake));
        floodModeButton = AddButton(details, "Sel Türü", "SEL / TAŞKIN",
            new Vector2(0.52f, 0.78f), new Vector2(0.92f, 0.88f), false,
            () => SelectDisaster(GameManager.DisasterType.Flood));
        provinceSelectedText = AddText(details, "İl Adı", "Haritada bir il seç",
            new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.77f), 28, Color.white, TextAnchor.MiddleLeft);
        provinceHoverText = AddText(details, "Harita Önizlemesi", "İl sınırlarının üzerine gelerek adını gör.",
            new Vector2(0.08f, 0.50f), new Vector2(0.92f, 0.61f), 16, new Color32(171, 193, 208, 255), TextAnchor.MiddleLeft);
        provinceScenarioText = AddText(details, "Senaryo Bilgisi", "İl seçimi yapılınca eğitim senaryosu burada gösterilir.",
            new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.48f), 17, new Color32(198, 211, 224, 255), TextAnchor.UpperLeft);
        provinceContinueButton = AddButton(details, "Tatbikata Devam", "SENARYOYU GÖR",
            new Vector2(0.08f, 0.09f), new Vector2(0.92f, 0.22f), true, ShowIntroPage);
        provinceContinueButton.interactable = false;
        AddText(details, "Klavye Yardımı", "← / → il seç   •   Enter devam",
            new Vector2(0.08f, 0.015f), new Vector2(0.92f, 0.08f), 15, new Color32(171, 193, 208, 255), TextAnchor.MiddleLeft);

        AddButton(provincePage.transform, "Geri", "ANA MENÜ",
            new Vector2(0.035f, 0.055f), new Vector2(0.155f, 0.12f), false, ShowMainPage);
        AddText(provincePage.transform, "Harita Atfı",
            "İl sınırları: geoBoundaries / OpenStreetMap contributors • CC BY-SA 2.0 • geoboundaries.org",
            new Vector2(0.18f, 0.045f), new Vector2(0.70f, 0.12f), 14, new Color32(142, 164, 180, 255), TextAnchor.MiddleLeft);
        UpdateDisasterSelection(false);
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
        AddImage(panel.gameObject, new Color32(10, 20, 32, 220));
        phaseText = AddText(panel, "Aşama", "HAZIRLIK", new Vector2(0.06f, 0.78f), new Vector2(0.94f, 0.95f),
            19, new Color32(94, 215, 196, 255), TextAnchor.MiddleLeft);
        taskText = AddText(panel, "Görevler", "", new Vector2(0.06f, 0.25f), new Vector2(0.94f, 0.76f),
            18, Color.white, TextAnchor.UpperLeft);
        taskText.supportRichText = true;
        scoreText = AddText(panel, "Puan", "PUAN  0", new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.21f),
            17, new Color32(255, 206, 123, 255), TextAnchor.MiddleLeft);

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

        targetGuideRect = MakeRect(hudPage.transform, "Aktif Görev Yön İşareti",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 42f),
            new Color32(10, 20, 32, 215));
        targetGuideText = AddText(targetGuideRect, "Hedef Adı", "", Vector2.zero, Vector2.one, 16,
            new Color32(235, 248, 244, 255), TextAnchor.MiddleCenter);
        targetGuideRect.gameObject.SetActive(false);
        BuildTrappedOverlay();
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
            new Vector2(0.35f, 0.49f), new Vector2(0.65f, 0.59f),
            true, owner.ResumeGame);

        AddButton(pausePage.transform, "Yeniden Başlat", "Yeniden Başlat",
            new Vector2(0.35f, 0.34f), new Vector2(0.65f, 0.44f),
            false, owner.RestartToMenu);

        AddButton(pausePage.transform, "Oyundan Çık", "Oyundan Çık",
            new Vector2(0.35f, 0.19f), new Vector2(0.65f, 0.29f),
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
        hasSavedProgress = ProvinceManager.Instance != null && GameManager.Instance != null
            && ProvinceManager.Instance.HasAnyCompletedScenario(GameManager.Instance.SelectedDisaster);
        if (continueButton != null)
            continueButton.GetComponentInChildren<Text>().text = hasSavedProgress ? "KAYITLI İLERLEME" : "KAYIT YOK";
        GameManager.Instance?.ReturnToMenu();
        owner.SetPlayerControls(false);
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

        bool flood = GameManager.Instance != null
            && GameManager.Instance.SelectedDisaster == GameManager.DisasterType.Flood;
        introProvinceLabel.text = selected.name + "  •  PLAKA " + selected.PlateLabel + "  /  EĞİTİM TATBİKATI";
        introScenarioTitle.text = flood ? "Sel ve taşkın: evden yüksek ve güvenli alana" : selected.trainingScenarioTitle;
        introScenarioInfo.text = flood
            ? "Taşkın uyarısını izle, değerli eşyaları ve ilaçları yukarı kaldır; su basma riski varsa elektriği güvenli biçimde kes.\nSu yükselirken araca binme veya akıntıya girme; evin üst katına ya da belirlenmiş yüksek alana çık.\nSu çekilmeden geri dönme; kirli suya ve elektrik hatlarına yaklaşma, resmî duyuruları takip et.\n\nBu, il için tehlike tahmini değil; afet farkındalık tatbikatıdır."
            : "Rafı sabitle, ağır eşyayı aşağı al ve çıkış yolunu aç.\nSarsıntıda sağlam bir eşyanın yanında çök-kapan-tutun.\nSonrasında çevreni kontrol et; gaz kokusu varsa vanayı güvenliyse kapat, kapı ve pencereleri aç ve elektrik düğmelerine dokunmadan toplanma alanına iler.\n\n" + selected.trainingScenarioNote;
        SetPages(introPage);
        owner.SetPlayerControls(false);
    }

    private void HandleProvinceHover(int plateCode)
    {
        if (provinceHoverText == null)
            return;

        if (plateCode < 0)
            provinceHoverText.text = "Deniz alanı • il sınırının içine tıkla.";
        else if (plateCode == -2)
            provinceHoverText.text = "Sel/taşkın senaryosu bu il için listelenmedi. Gri olmayan bir ilden seçim yap.";
        else if (plateCode == 0)
            provinceHoverText.text = "Harita üzerinde bir il sınırının üzerine gel.";
        else
        {
            ProvinceInfo province = ProvinceManager.Instance != null ? ProvinceManager.Instance.GetProvince(plateCode) : null;
            provinceHoverText.text = province != null
                ? "ÖNİZLEME  •  " + province.name + "  /  PLAKA " + province.PlateLabel
                : string.Empty;
        }
    }

    private void HandleProvinceSelected(int plateCode)
    {
        if (plateCode <= 0)
        {
            provinceSelectedText.text = "Haritada bir il seç";
            provinceScenarioText.text = GameManager.Instance != null
                && GameManager.Instance.SelectedDisaster == GameManager.DisasterType.Flood
                ? "Sel/taşkın tatbikatı için listelenen illerden birini seç. Gri iller tamamlandı; tekrar oynanabilir."
                : "Deprem tatbikatı için bir il seç. Gri iller tamamlandı; tekrar oynanabilir.";
            provinceContinueButton.interactable = false;
            return;
        }
        ProvinceInfo province = ProvinceManager.Instance != null ? ProvinceManager.Instance.GetProvince(plateCode) : null;
        if (province == null)
            return;

        provinceSelectedText.text = province.name + "  •  PLAKA " + province.PlateLabel;
        bool flood = GameManager.Instance != null
            && GameManager.Instance.SelectedDisaster == GameManager.DisasterType.Flood;
        bool completed = ProvinceManager.Instance.IsScenarioCompleted(plateCode,
            flood ? GameManager.DisasterType.Flood : GameManager.DisasterType.Earthquake);
        provinceScenarioText.text = "Afet türü: " + (flood ? "Sel ve taşkın" : "Deprem")
            + "\n" + (flood ? "Sel ve taşkın: güvenli tahliye" : province.trainingScenarioTitle)
            + (completed ? "\n\n✓ Bu ilde tamamlandı — yeniden oynayabilirsin." : "\n\n" + (flood
                ? "Taşkın tehlikesine ve geçmiş taşkın olaylarına göre seçilmiş eğitim ili."
                : province.trainingScenarioNote));
        provinceContinueButton.interactable = true;
    }

    private void SelectDisaster(GameManager.DisasterType disaster)
    {
        if (GameManager.Instance == null || GameManager.Instance.SelectedDisaster == disaster)
            return;
        GameManager.Instance.SelectDisaster(disaster);
        UpdateDisasterSelection(true);
    }

    private void UpdateDisasterSelection(bool resetMap)
    {
        bool flood = GameManager.Instance != null
            && GameManager.Instance.SelectedDisaster == GameManager.DisasterType.Flood;
        if (earthquakeModeButton != null) earthquakeModeButton.interactable = flood;
        if (floodModeButton != null) floodModeButton.interactable = !flood;
        if (provinceSelectedText != null) provinceSelectedText.text = "Haritada bir il seç";
        if (provinceScenarioText != null)
            provinceScenarioText.text = flood
                ? "Sel ve taşkın tatbikatı için listelenen illerden birini seç."
                : "Deprem tatbikatı için haritadan bir il seç.";
        if (provinceContinueButton != null) provinceContinueButton.interactable = false;
        if (resetMap) provinceMapView?.OnDisasterTypeChanged();
    }

    private void ShowSavedProgress()
    {
        if (!hasSavedProgress)
        {
            ShowProvincePage();
            return;
        }

        int plate = ProvinceManager.Instance != null ? ProvinceManager.Instance.GetLastCompletedProvincePlate() : 6;
        ProvinceManager.Instance?.SelectProvince(plate);
        bool flood = GameManager.Instance != null
            && GameManager.Instance.SelectedDisaster == GameManager.DisasterType.Flood;
        string scorePrefix = "TurkiyeAfet." + (flood ? "Sel" : "Deprem");
        string lastKey = ProvinceManager.ProgressKey(scorePrefix + ".LastScore", plate);
        string bestKey = ProvinceManager.ProgressKey(scorePrefix + ".BestScore", plate);
        int last = PlayerPrefs.GetInt(lastKey, plate == 6 ? PlayerPrefs.GetInt("TurkiyeAfet.Deprem.LastScore", 0) : 0);
        int best = PlayerPrefs.GetInt(bestKey, plate == 6 ? PlayerPrefs.GetInt("TurkiyeAfet.Deprem.BestScore", last) : last);
        ProvinceInfo province = ProvinceManager.Instance != null ? ProvinceManager.Instance.GetProvince(plate) : null;
        ShowResults(last, "Son tamamlanan senaryo: " + (province != null ? province.name : "seçilen il") +
            (flood ? " ilinde sel ve taşkından korunma." : " ilinde deprem hazırlığı ve güvenli tahliye.")
            + " En yüksek puanın: " + best + ".\nİlerlemen bu bilgisayarda saklanıyor.");
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
    public void ShowResults(int score, string review)
    {
        resultTitle.text = "Tatbikat tamamlandı";
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
        scoreText.text = "PUAN   " + score;
        quakeStartHint.text = preparationCountdown > 0.05f
            ? "DEPREM " + Mathf.CeilToInt(preparationCountdown) + " SANİYE İÇİNDE BAŞLIYOR"
            : string.Empty;
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





