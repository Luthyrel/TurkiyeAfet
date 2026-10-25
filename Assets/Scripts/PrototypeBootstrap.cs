using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public sealed class PrototypeBootstrap : MonoBehaviour
{
    public static PrototypeBootstrap Instance { get; private set; }

    private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    private PrototypeScenario scenario;
    private Material yellowBottleCap;
    private PrototypePlayerController player;
    private PrototypeUI ui;
    private Transform windowGlass;
    private GameObject holdZoneMarker;
    private bool gameStarted;
    private bool paused;
    private Vector3 safeTablePosition;

    public PrototypeScenario Scenario => scenario;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (GameManager.Instance == null)
            new GameObject("Oyun Yöneticisi").AddComponent<GameManager>();
        if (ProvinceManager.Instance == null)
            new GameObject("81 İl Verisi").AddComponent<ProvinceManager>();
    }

    private void Start()
    {
        Time.timeScale = 1f;
        Application.targetFrameRate = 120;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        ConfigureLighting();

        scenario = new GameObject("Deprem Senaryosu").AddComponent<PrototypeScenario>();
        BuildWorld();

        GameObject playerObject = new GameObject("Oyuncu", typeof(CharacterController));
        player = playerObject.AddComponent<PrototypePlayerController>();
        player.Initialize(scenario, new Vector3(-0.4f, 0f, -2.3f), 255f);
        scenario.Initialize(this, player, safeTablePosition);
        player.SetControlsEnabled(false);

        ui = new GameObject("Arayüz Yöneticisi").AddComponent<PrototypeUI>();
        ui.Initialize(this);
        scenario.SendMessage("RefreshHud", SendMessageOptions.DontRequireReceiver);
    }

    private void ConfigureLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.50f, 0.59f, 0.68f);
        RenderSettings.ambientEquatorColor = new Color(0.35f, 0.39f, 0.42f);
        RenderSettings.ambientGroundColor = new Color(0.15f, 0.16f, 0.17f);
        RenderSettings.ambientIntensity = 0.85f;
        RenderSettings.fog = false;

        GameObject sunObject = new GameObject("Yumuşak Gün Işığı");
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.88f, 0.72f);
        sun.intensity = 1.15f;
        sun.shadows = LightShadows.Soft;
        sunObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
    }

    private void BuildWorld()
    {
        Material floorMat = MakeMaterial("Sıcak Taş", new Color32(194, 185, 165, 255));
        Material wallMat = MakeMaterial("Kırık Beyaz Duvar", new Color32(225, 220, 207, 255));
        Material trimMat = MakeMaterial("Koyu Ahşap", new Color32(87, 67, 52, 255));
        Material tealMat = MakeMaterial("Petrol Mavisi", new Color32(45, 111, 112, 255));
        Material softTealMat = MakeMaterial("Yumuşak Yeşil", new Color32(102, 153, 139, 255));
        Material rustMat = MakeMaterial("Kiremit", new Color32(177, 94, 67, 255));
        Material brassMat = MakeMaterial("Pirinç", new Color32(184, 145, 74, 255));
        Material glassMat = MakeMaterial("Cam", new Color32(108, 157, 179, 180));
        Material outsideMat = MakeMaterial("Güvenli Alan", new Color32(105, 135, 113, 255));

        CreateBox("Ev Zemini", new Vector3(0f, -0.16f, 1f), new Vector3(14f, 0.32f, 11f), floorMat);
        CreateBox("Kuzey Duvarı Sol", new Vector3(-2.85f, 1.55f, 6.55f), new Vector3(8.3f, 3.1f, 0.25f), wallMat);
        CreateBox("Kuzey Duvarı Sağ", new Vector3(5.65f, 1.55f, 6.55f), new Vector3(2.7f, 3.1f, 0.25f), wallMat);
        CreateBox("Pencere Üst Duvarı", new Vector3(2.8f, 2.8375f, 6.55f), new Vector3(3f, 0.525f, 0.25f), wallMat);
        CreateBox("Pencere Alt Duvarı", new Vector3(2.8f, 0.55f, 6.55f), new Vector3(3f, 1.1f, 0.25f), wallMat);
        CreateBox("Güney Duvarı", new Vector3(0f, 1.55f, -4.55f), new Vector3(14f, 3.1f, 0.25f), wallMat);
        CreateBox("Doğu Duvarı", new Vector3(7f, 1.55f, 1f), new Vector3(0.25f, 3.1f, 11f), wallMat);
        CreateBox("Batı Duvarı Altı", new Vector3(-7f, 1.55f, -3.8f), new Vector3(0.25f, 3.1f, 1.5f), wallMat);
        CreateBox("Batı Duvarı Üstü", new Vector3(-7f, 1.55f, 2.50f), new Vector3(0.25f, 3.1f, 8f), wallMat);
        CreateBox(
            "Kapı Üstü Duvar",
            new Vector3(-7f, 2.57f, -2.3f),
            new Vector3(0.25f, 1.06f, 1.62f),
            wallMat
        );

        CreateBox("Dış Güvenli Yürüyüş Alanı", new Vector3(-9.8f, -0.14f, -2.3f), new Vector3(5.8f, 0.28f, 4.5f), outsideMat);
        CreateBox("Toplanma Alanı İşareti", new Vector3(-11.1f, 0.025f, -2.3f), new Vector3(1.3f, 0.04f, 1.4f),
            MakeMaterial("İşaret", new Color32(237, 199, 116, 255)));

        // Giriş çerçevesi ve açık kapı boşluğu
        CreateBox("Kapı Çerçevesi Sol", new Vector3(-6.82f, 1.05f, -3.02f), new Vector3(0.20f, 2.1f, 0.18f), trimMat);
        CreateBox("Kapı Çerçevesi Sağ", new Vector3(-6.82f, 1.05f, -1.58f), new Vector3(0.20f, 2.1f, 0.18f), trimMat);
        CreateBox("Kapı Üst Kiriş", new Vector3(-6.82f, 2.12f, -2.3f), new Vector3(0.20f, 0.16f, 1.62f), trimMat);
        BuildEntryDoor(trimMat, brassMat);

        // Pencere, perde ve yumuşak renkli iç mekân detayları
        GameObject glass = CreateBox("Pencere Camı", new Vector3(2.8f, 1.9f, 6.38f), new Vector3(3.0f, 1.35f, 0.05f), glassMat);
        windowGlass = glass.transform;
        CreateBox("Pencere Altlığı", new Vector3(2.8f, 1.15f, 6.28f), new Vector3(3.2f, 0.12f, 0.30f), trimMat);
        CreateBox("Sol Perde", new Vector3(1.0f, 2.0f, 6.2f), new Vector3(0.38f, 2.0f, 0.25f), rustMat);
        CreateBox("Sağ Perde", new Vector3(4.6f, 2.0f, 6.2f), new Vector3(0.38f, 2.0f, 0.25f), rustMat);
        CreateBox("Salon Halısı", new Vector3(0.2f, 0.015f, 2.3f), new Vector3(4.7f, 0.04f, 3.0f), softTealMat);

       CreateBox("Ev Tavanı", new Vector3(0f, 3.15f, 1f),
           new Vector3(14f, 0.25f, 11f), wallMat);

        BuildTable(tealMat, trimMat);
        BuildSofa(tealMat, rustMat);
        BuildBookshelf(tealMat, trimMat);
        BuildHeavyItem(brassMat, trimMat);
        BuildExitBlocker(rustMat, trimMat);
        BuildDisasterBag(tealMat, brassMat, trimMat);
        BuildDisasterBagItems();
        BuildGasValve(brassMat, trimMat);
        BuildWindowControl(brassMat);
        BuildLooseProps(rustMat, brassMat);
        BuildRoomDecor(wallMat, brassMat, trimMat);
        SinirEkle("Sol Sınır", new Vector3(-12.8f, 1.5f, -2.3f), new Vector3(0.2f, 3f, 5.5f));
        SinirEkle("Ön Sınır", new Vector3(-9.8f, 1.5f, -4.7f), new Vector3(5.8f, 3f, 0.2f));
        SinirEkle("Arka Sınır", new Vector3(-9.8f, 1.5f, 0.1f), new Vector3(5.8f, 3f, 0.2f));
    }

    private void BuildTable(Material topMat, Material legMat)
    {
        safeTablePosition = new Vector3(0.35f, 0f, 1.25f);
        CreateBox("Sağlam Masanın Üstü", safeTablePosition + new Vector3(0f, 1.25f, 0f),
            new Vector3(2.2f, 0.14f, 1.35f), topMat);
        for (int x = -1; x <= 1; x += 2)
        for (int z = -1; z <= 1; z += 2)
            CreateBox("Masa Ayağı", safeTablePosition + new Vector3(x * 0.94f, 0.615f, z * 0.53f),
                new Vector3(0.12f, 1.23f, 0.12f), legMat);
        CreateBox("Sağlam Koltuk", new Vector3(-1.9f, 0.55f, 1.45f), new Vector3(2.5f, 1.1f, 0.85f), topMat);
        BuildHoldZoneMarker();
    }

    private void BuildHoldZoneMarker()
    {
        holdZoneMarker = new GameObject("Çök-Kapan-Tutun Güvenli Bölgesi");
        holdZoneMarker.transform.position = safeTablePosition + new Vector3(-0.35f, 0f, -0.15f) + Vector3.up * 0.035f;

        GameObject ringObject = new GameObject("Güvenli Bölge Halkası");
        ringObject.transform.SetParent(holdZoneMarker.transform, false);
        LineRenderer ring = ringObject.AddComponent<LineRenderer>();
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = 48;
        ring.startWidth = 0.045f;
        ring.endWidth = 0.045f;
        ring.numCapVertices = 3;
        ring.material = MakeMaterial("Güvenli Bölge Işığı", new Color32(56, 207, 179, 255));
        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = Mathf.PI * 2f * i / ring.positionCount;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.72f, 0f, Mathf.Sin(angle) * 0.72f));
        }

        GameObject arrowObject = new GameObject("Masaya Yön Oku");
        arrowObject.transform.SetParent(holdZoneMarker.transform, false);
        LineRenderer arrow = arrowObject.AddComponent<LineRenderer>();
        arrow.useWorldSpace = false;
        arrow.positionCount = 4;
        arrow.startWidth = 0.055f;
        arrow.endWidth = 0.055f;
        arrow.material = ring.material;
        arrow.SetPosition(0, new Vector3(0f, 0.005f, 0.03f));
        arrow.SetPosition(1, new Vector3(0f, 0.005f, 0.48f));
        arrow.SetPosition(2, new Vector3(-0.16f, 0.005f, 0.31f));
        arrow.SetPosition(3, new Vector3(0f, 0.005f, 0.48f));
        holdZoneMarker.SetActive(false);
    }

    private void BuildSofa(Material bodyMat, Material cushionMat)
    {
        CreateBox("Koltuk Gövdesi", new Vector3(1.1f, 0.36f, 5.05f), new Vector3(3.3f, 0.65f, 0.95f), bodyMat);
        CreateBox("Koltuk Sırtı", new Vector3(1.1f, 1.0f, 5.48f), new Vector3(3.3f, 1.15f, 0.20f), bodyMat);
        CreateBox("Koltuk Minderi", new Vector3(1.1f, 0.75f, 4.94f), new Vector3(3.0f, 0.24f, 0.78f), cushionMat);
        CreateBox("Koltuk Kolçağı Sol", new Vector3(-0.62f, 0.70f, 5.0f), new Vector3(0.25f, 0.8f, 1.0f), bodyMat);
        CreateBox("Koltuk Kolçağı Sağ", new Vector3(2.82f, 0.70f, 5.0f), new Vector3(0.25f, 0.8f, 1.0f), bodyMat);
    }

    private void BuildBookshelf(Material bodyMat, Material shelfMat)
    {
        GameObject shelf = new GameObject("Kitaplık");
        shelf.transform.position = new Vector3(6.4f, 0f, 3.4f);
        BoxCollider collider = shelf.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 1.1f, 0f);
        collider.size = new Vector3(0.85f, 2.25f, 1.65f);
        Rigidbody body = shelf.AddComponent<Rigidbody>();
        body.isKinematic = true;
        PrototypeInteractable task = shelf.AddComponent<PrototypeInteractable>();
        task.Configure(PrototypeAction.SecureFurniture, "Rafı duvara sabitle");

        CreateChildBox(shelf.transform, "Kitaplık Gövdesi", new Vector3(0f, 1.10f, 0f), new Vector3(0.75f, 2.2f, 1.55f), bodyMat);
        for (int i = 0; i < 4; i++)
            CreateChildBox(shelf.transform, "Raf", new Vector3(-0.05f, 0.28f + i * 0.56f, 0f), new Vector3(0.82f, 0.08f, 1.56f), shelfMat);
        CreateChildBox(shelf.transform, "SabitlemeKayışı", new Vector3(-0.40f, 1.42f, 0f), new Vector3(0.04f, 0.07f, 1.50f), MakeMaterial("Kayış", new Color32(54, 168, 150, 255))).SetActive(false);
        CreateChildBox(shelf.transform, "SabitlemeKayışı", new Vector3(-0.40f, 0.72f, 0f), new Vector3(0.04f, 0.07f, 1.50f), MakeMaterial("Kayış", new Color32(54, 168, 150, 255))).SetActive(false);
        task.GetType();
        scenario.AddPreparationTask(task);
        scenario.RegisterFurniture(body, task);

        CreateBox("Kitaplık Üstündeki Kitaplar", new Vector3(6.3f, 2.4f, 3.5f), new Vector3(0.45f, 0.25f, 0.9f), MakeMaterial("Kitap", new Color32(183, 111, 75, 255)));
    }

    private void BuildHeavyItem(Material objectMat, Material standMat)
    {
        CreateBox("Düşük Konsol", new Vector3(2.9f, 0.55f, -2.9f), new Vector3(1.2f, 1.1f, 0.55f), standMat);
        GameObject heavy = CreateBox("Yüksek Raf Üstündeki Ağır Kutu", new Vector3(2.9f, 2.37f, -2.9f), new Vector3(0.72f, 0.62f, 0.60f), objectMat);
        PrototypeInteractable task = heavy.AddComponent<PrototypeInteractable>();
        task.Configure(PrototypeAction.LowerHeavyItem, "Ağır eşyayı alt rafa al");
        task.ConfigureMove(heavy.transform, new Vector3(2.9f, 1.41f, -2.9f));
        scenario.AddPreparationTask(task);
        CreateBox("Yüksek Raf", new Vector3(2.9f, 2.0f, -2.9f), new Vector3(1.3f, 0.10f, 0.7f), standMat);
    }

    private void BuildExitBlocker(Material boxMat, Material trimMat)
    {
        CreateBox("Giriş Paspası", new Vector3(-6.25f, 0.02f, -2.3f), new Vector3(0.9f, 0.06f, 1.1f),
            MakeMaterial("Paspas", new Color32(47, 75, 77, 255)));
        GameObject blocker = CreateBox("Çıkışı Kapatan Koli", new Vector3(-5.65f, 0.50f, -2.30f), new Vector3(0.9f, 1.0f, 1.0f), boxMat);
        PrototypeInteractable task = blocker.AddComponent<PrototypeInteractable>();
        task.Configure(PrototypeAction.ClearExit, "Çıkış yolunu açık tut");
        task.ConfigureMove(blocker.transform, new Vector3(-4.75f, 0.50f, -3.65f));
        scenario.AddPreparationTask(task);
        CreateBox("Ayakkabılık", new Vector3(-5.1f, 0.65f, -4.0f), new Vector3(1.4f, 1.3f, 0.7f), trimMat);
    }

    private void BuildEntryDoor(Material panelMat, Material trimMat)
    {
        GameObject hinge = new GameObject("Giriş Kapısı Menteşesi");
        hinge.transform.position = new Vector3(-6.80f, 0f, -3.02f);
        GameObject doorPivot = new GameObject("Kapı Kanadı ve Menteşe");
        doorPivot.transform.SetParent(hinge.transform, false);
        doorPivot.transform.localPosition = Vector3.zero;
        CreateChildBox(doorPivot.transform, "Ahşap Kapı", new Vector3(0f, 1.02f, 0.72f),
            new Vector3(0.12f, 2.02f, 1.40f), panelMat);
        CreateChildBox(doorPivot.transform, "Kapı İç Paneli", new Vector3(-0.066f, 1.02f, 0.72f),
            new Vector3(0.018f, 1.62f, 1.06f), trimMat);
        CreateChildBox(doorPivot.transform, "Kapı Üst Paneli", new Vector3(-0.07f, 1.54f, 0.72f),
            new Vector3(0.02f, 0.42f, 0.94f), panelMat);
        CreateChildBox(doorPivot.transform, "Kapı Alt Paneli", new Vector3(-0.07f, 0.50f, 0.72f),
            new Vector3(0.02f, 0.42f, 0.94f), panelMat);
        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        handle.name = "Kapı Tokmağı";
        handle.transform.SetParent(doorPivot.transform, false);
        handle.transform.localPosition = new Vector3(-0.09f, 1.02f, 0.28f);
        handle.transform.localScale = new Vector3(0.10f, 0.10f, 0.10f);
        handle.GetComponent<Renderer>().sharedMaterial = trimMat;
        PrototypeInteractable task = hinge.AddComponent<PrototypeInteractable>();
        task.Configure(PrototypeAction.OpenExitDoor, "Kapıyı aç");
        task.ConfigureExitDoor(doorPivot.transform);
    }

    private void BuildDisasterBag(Material bodyMat, Material strapMat, Material trimMat)
    {
        GameObject bag = new GameObject("Afet Çantası");
        bag.transform.position = new Vector3(1.1f, 0.88f, 4.90f);
        CreateChildBox(bag.transform, "Çanta Ana Gövdesi", new Vector3(0f, 0.17f, 0f),
            new Vector3(0.98f, 0.40f, 0.62f), bodyMat);
        CreateChildBox(bag.transform, "Ön Cep", new Vector3(0f, 0.10f, -0.325f),
            new Vector3(0.70f, 0.22f, 0.08f), strapMat);
        CreateChildBox(bag.transform, "Ön Cep Kapağı", new Vector3(0f, 0.235f, -0.37f),
            new Vector3(0.73f, 0.055f, 0.035f), trimMat);
        CreateChildBox(bag.transform, "Dikey Fermuar Şeridi", new Vector3(0.42f, 0.19f, -0.325f),
            new Vector3(0.025f, 0.25f, 0.02f), trimMat);
        CreateChildBox(bag.transform, "Yansıtıcı Şerit", new Vector3(-0.02f, 0.14f, -0.37f),
            new Vector3(0.52f, 0.035f, 0.018f), MakeMaterial("Yansıtıcı Sarı", new Color32(239, 207, 112, 255)));
        CreateChildBox(bag.transform, "Yan Cep", new Vector3(-0.50f, 0.12f, 0f),
            new Vector3(0.08f, 0.24f, 0.38f), strapMat);
        CreateChildBox(bag.transform, "Yan Cep", new Vector3(0.50f, 0.12f, 0f),
            new Vector3(0.08f, 0.24f, 0.38f), strapMat);
        GameObject closedLid = CreateChildBox(bag.transform, "Çanta Kapağı",
            new Vector3(0f, 0.405f, 0f), new Vector3(0.98f, 0.07f, 0.62f), strapMat);
        GameObject openInterior = new GameObject("Açık Çanta İçi");
        openInterior.transform.SetParent(bag.transform, false);
        CreateChildBox(openInterior.transform, "İç Taban", new Vector3(0f, 0.375f, 0f),
            new Vector3(0.76f, 0.025f, 0.42f), trimMat);
        CreateChildBox(openInterior.transform, "Arka İç Kenar", new Vector3(0f, 0.405f, 0.245f),
            new Vector3(0.80f, 0.07f, 0.06f), bodyMat);
        CreateChildBox(openInterior.transform, "Sol İç Kenar", new Vector3(-0.39f, 0.405f, 0f),
            new Vector3(0.06f, 0.07f, 0.43f), bodyMat);
        CreateChildBox(openInterior.transform, "Sağ İç Kenar", new Vector3(0.39f, 0.405f, 0f),
            new Vector3(0.06f, 0.07f, 0.43f), bodyMat);
        GameObject openFlap = new GameObject("Arkaya Katlanmış Çanta Kapağı");
        openFlap.transform.SetParent(openInterior.transform, false);
        openFlap.transform.localPosition = new Vector3(0f, 0.39f, 0.25f);
        openFlap.transform.localRotation = Quaternion.Euler(-58f, 0f, 0f);
        CreateChildBox(openFlap.transform, "Açık Kapak", new Vector3(0f, 0.02f, 0.14f),
            new Vector3(0.98f, 0.065f, 0.30f), strapMat);
        CreateChildBox(openFlap.transform, "Kapak Şeridi", new Vector3(0f, 0.06f, 0.14f),
            new Vector3(0.55f, 0.018f, 0.025f), MakeMaterial("Kapak Şeridi", new Color32(239, 207, 112, 255)));
        openInterior.SetActive(false);
        CreateChildBox(bag.transform, "Çanta Kulp Tabanı", new Vector3(0f, 0.49f, 0f),
            new Vector3(0.38f, 0.06f, 0.07f), strapMat);
        CreateChildBox(bag.transform, "Omuz Askısı Sol", new Vector3(-0.34f, 0.02f, 0.27f),
            new Vector3(0.12f, 0.36f, 0.05f), trimMat);
        CreateChildBox(bag.transform, "Omuz Askısı Sağ", new Vector3(0.34f, 0.02f, 0.27f),
            new Vector3(0.12f, 0.36f, 0.05f), trimMat);

        PrototypeInteractable task = bag.AddComponent<PrototypeInteractable>();
        task.Configure(PrototypeAction.PrepareDisasterBag, "Çantayı hazırla.");
        task.ConfigureBagVisual(closedLid, openInterior);
        scenario.AddPreparationTask(task);
        scenario.SetDisasterBag(task);
    }

    private void BuildDisasterBagItems()
    {
        Material blue = MakeMaterial("Su Şişesi", new Color32(47, 139, 188, 255));
        yellowBottleCap = MakeMaterial("Şişe Kapağı", new Color32(238, 190, 76, 255));
        Material red = MakeMaterial("İlk Yardım Çantası", new Color32(190, 54, 54, 255));
        Material white = MakeMaterial("İlk Yardım Beyazı", new Color32(247, 240, 221, 255));
        Material dark = MakeMaterial("Fener Gövdesi", new Color32(39, 52, 60, 255));
        Material silver = MakeMaterial("Metal Ayrıntı", new Color32(180, 190, 189, 255));
        Material yellow = MakeMaterial("Acil Durum Sarısı", new Color32(236, 190, 74, 255));
        Material radioMat = MakeMaterial("Telsiz Gövdesi", new Color32(52, 83, 77, 255));
        Material foodMat = MakeMaterial("Gıda Paketi", new Color32(190, 130, 56, 255));
        Material accent = MakeMaterial("Paket Ayrıntısı", new Color32(244, 224, 168, 255));

        GameObject[] items =
        {
            CreateWaterBottle(blue, silver),
            CreateFirstAidPack(red, white),
            CreateFlashlight(dark, yellow, silver),
            CreateRadio(radioMat, yellow, silver),
            CreateFoodPack(foodMat, accent),
            CreateWhistle(silver, yellow, dark)
        };
        string[] names = { "Su şişesi", "İlk yardım çantası", "El feneri", "Acil durum radyosu", "Uzun ömürlü gıda", "Düdük" };
        // Kaynak eksenler model-local; hedef yönler avuç çerçevesindedir.
        // Su şişesi korunur; asimetrik eşyalar doğru yüz/top yönü için ayrı çevrilir.
        Vector3[] gripAxes = { Vector3.up, Vector3.right, Vector3.right, Vector3.right, Vector3.right, Vector3.right };
        Vector3[] gripDirections = { Vector3.up, Vector3.down, Vector3.up, Vector3.down, Vector3.down, Vector3.up };
        Vector3[] gripFaceAxes = { Vector3.back, Vector3.back, Vector3.back, Vector3.back, Vector3.back, Vector3.back };
        // Sabit yerler: bazıları mobilya altı/arkası; hepsi zeminde ve erişilebilir.
        Vector3[] floorSpots =
        {
            new Vector3(-3.10f, 0f, 0.92f),  // Yemek masasının altında
            new Vector3(0.35f, 0f, 0.92f),   // Sağlam masanın altında
            new Vector3(5.08f, 0f, 0.35f),   // TV ünitesinin yanında
            new Vector3(5.45f, 0f, 2.20f),   // Kitaplığın yan tarafında
            new Vector3(2.20f, 0f, 5.84f),   // Koltuğun arkasında
            new Vector3(-5.52f, 0f, -3.48f)  // Ayakkabılığın ön yanında
        };

        for (int i = 0; i < items.Length; i++)
        {
            items[i].name = names[i];
            items[i].transform.position = floorSpots[i];
            PrototypeInteractable pickup = items[i].AddComponent<PrototypeInteractable>();
            pickup.Configure(PrototypeAction.CollectBagItem, names[i]);
            pickup.ConfigureHeldGrip(gripAxes[i], gripDirections[i], gripFaceAxes[i]);
        }
    }

    private GameObject CreateBagItemRoot()
    {
        return new GameObject("Yerdeki eşya");
    }

    private GameObject CreateWaterBottle(Material bottleMaterial, Material detailMaterial)
    {
        GameObject root = CreateBagItemRoot();
        GameObject bottle = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bottle.name = "Şeffaf Mavi Şişe";
        bottle.transform.SetParent(root.transform, false);
        bottle.transform.localPosition = new Vector3(0f, 0.19f, 0f);
        bottle.transform.localScale = new Vector3(0.16f, 0.18f, 0.16f);
        bottle.GetComponent<Renderer>().sharedMaterial = bottleMaterial;
        CreateChildCylinder(root.transform, "Şişe Boynu", new Vector3(0f, 0.385f, 0f), new Vector3(0.055f, 0.06f, 0.055f), detailMaterial);
        CreateChildCylinder(root.transform, "Şişe Kapağı", new Vector3(0f, 0.445f, 0f), new Vector3(0.065f, 0.035f, 0.065f), yellowBottleCap);
        CreateChildBox(root.transform, "Şişe Etiketi", new Vector3(0f, 0.18f, -0.075f), new Vector3(0.12f, 0.10f, 0.012f), detailMaterial);
        return root;
    }

    private GameObject CreateFirstAidPack(Material packMaterial, Material crossMaterial)
    {
        GameObject root = CreateBagItemRoot();
        CreateChildBox(root.transform, "Kırmızı İlk Yardım Kesesi", new Vector3(0f, 0.12f, 0f),
            new Vector3(0.40f, 0.22f, 0.30f), packMaterial);
        CreateChildBox(root.transform, "Beyaz Artı Dikey", new Vector3(0f, 0.14f, -0.157f),
            new Vector3(0.07f, 0.15f, 0.018f), crossMaterial);
        CreateChildBox(root.transform, "Beyaz Artı Yatay", new Vector3(0f, 0.14f, -0.16f),
            new Vector3(0.18f, 0.06f, 0.02f), crossMaterial);
        CreateChildBox(root.transform, "Kese Fermuarı", new Vector3(0f, 0.245f, 0f),
            new Vector3(0.31f, 0.025f, 0.24f), crossMaterial);
        CreateChildBox(root.transform, "Taşıma Sapı", new Vector3(0f, 0.275f, 0f),
            new Vector3(0.18f, 0.045f, 0.07f), packMaterial);
        return root;
    }

    private GameObject CreateFlashlight(Material bodyMaterial, Material lensMaterial, Material metalMaterial)
    {
        GameObject root = CreateBagItemRoot();
        CreateChildCylinder(root.transform, "El Feneri Gövdesi", new Vector3(0f, 0.08f, 0f),
            new Vector3(0.065f, 0.14f, 0.065f), bodyMaterial, Quaternion.Euler(0f, 0f, 90f));
        CreateChildCylinder(root.transform, "Geniş Fener Başlığı", new Vector3(0.15f, 0.08f, 0f),
            new Vector3(0.10f, 0.055f, 0.10f), metalMaterial, Quaternion.Euler(0f, 0f, 90f));
        // Mercek yüzü avuç kamerasına döner; gövdenin uzun ekseni parmak yönünde kalır.
        CreateChildCylinder(root.transform, "Kameraya Bakan Fener Merceği", new Vector3(0.15f, 0.08f, -0.102f),
            new Vector3(0.078f, 0.018f, 0.078f), lensMaterial, Quaternion.Euler(90f, 0f, 0f));
        CreateChildBox(root.transform, "Fener Düğmesi", new Vector3(-0.04f, 0.148f, 0f),
            new Vector3(0.055f, 0.025f, 0.045f), lensMaterial);
        for (int i = 0; i < 3; i++)
            CreateChildBox(root.transform, "Fener Kavrama Çizgisi", new Vector3(-0.08f + i * 0.055f, 0.08f, -0.068f),
                new Vector3(0.018f, 0.08f, 0.012f), metalMaterial);
        return root;
    }

    private GameObject CreateRadio(Material bodyMaterial, Material detailMaterial, Material metalMaterial)
    {
        GameObject root = CreateBagItemRoot();
        CreateChildBox(root.transform, "Telsiz Ana Gövde", new Vector3(0f, 0.12f, 0f),
            new Vector3(0.38f, 0.22f, 0.24f), bodyMaterial);
        CreateChildBox(root.transform, "Telsiz Ekranı", new Vector3(-0.075f, 0.14f, -0.128f),
            new Vector3(0.13f, 0.085f, 0.018f), detailMaterial);
        CreateChildBox(root.transform, "Telsiz Hoparlör Paneli", new Vector3(0.105f, 0.13f, -0.13f),
            new Vector3(0.13f, 0.12f, 0.018f), metalMaterial);
        for (int i = 0; i < 4; i++)
            CreateChildBox(root.transform, "Hoparlör Izgarası", new Vector3(0.105f, 0.09f + i * 0.025f, -0.143f),
                new Vector3(0.09f, 0.008f, 0.008f), bodyMaterial);
        CreateChildCylinder(root.transform, "Telsiz Kanal Düğmesi", new Vector3(0.12f, 0.245f, -0.09f),
            new Vector3(0.035f, 0.025f, 0.035f), detailMaterial);
        CreateChildCylinder(root.transform, "Telsiz Ses Düğmesi", new Vector3(0.19f, 0.245f, -0.09f),
            new Vector3(0.028f, 0.025f, 0.028f), metalMaterial);
        CreateChildCylinder(root.transform, "Telsiz Anteni", new Vector3(-0.15f, 0.29f, 0.04f),
            new Vector3(0.012f, 0.16f, 0.012f), metalMaterial, Quaternion.Euler(0f, 0f, -12f));
        return root;
    }

    private GameObject CreateFoodPack(Material bodyMaterial, Material detailMaterial)
    {
        GameObject root = CreateBagItemRoot();
        CreateChildBox(root.transform, "Gıda Paketi", new Vector3(0f, 0.11f, 0f),
            new Vector3(0.34f, 0.20f, 0.26f), bodyMaterial);
        CreateChildBox(root.transform, "Paket Üst Kaynağı", new Vector3(0f, 0.218f, 0f),
            new Vector3(0.35f, 0.018f, 0.27f), detailMaterial);
        CreateChildBox(root.transform, "Paket Ön Etiketi", new Vector3(0f, 0.12f, -0.135f),
            new Vector3(0.22f, 0.11f, 0.012f), detailMaterial);
        CreateChildCylinder(root.transform, "Paket Sembolü", new Vector3(0f, 0.12f, -0.15f),
            new Vector3(0.035f, 0.018f, 0.035f), bodyMaterial);
        return root;
    }

    private GameObject CreateWhistle(Material bodyMaterial, Material detailMaterial, Material apertureMaterial)
    {
        GameObject root = CreateBagItemRoot();
        CreateChildCylinder(root.transform, "Düdük Gövdesi", new Vector3(0f, 0.045f, 0f),
            new Vector3(0.038f, 0.12f, 0.038f), bodyMaterial, Quaternion.Euler(0f, 0f, 90f));
        CreateChildCylinder(root.transform, "Düdük Ağızlığı", new Vector3(0.12f, 0.045f, 0f),
            new Vector3(0.045f, 0.045f, 0.045f), detailMaterial, Quaternion.Euler(0f, 0f, 90f));
        CreateChildBox(root.transform, "Kameraya Bakan Düdük Açıklığı", new Vector3(0.12f, 0.045f, -0.047f),
            new Vector3(0.028f, 0.022f, 0.006f), apertureMaterial);
        CreateChildCylinder(root.transform, "Düdük Halkası", new Vector3(-0.10f, 0.045f, 0f),
            new Vector3(0.055f, 0.018f, 0.055f), detailMaterial, Quaternion.Euler(90f, 0f, 0f));
        return root;
    }

    private GameObject CreateChildCylinder(Transform parent, string name, Vector3 localPosition,
        Vector3 localScale, Material material, Quaternion? localRotation = null)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.localPosition = localPosition;
        cylinder.transform.localScale = localScale;
        if (localRotation.HasValue)
            cylinder.transform.localRotation = localRotation.Value;
        cylinder.GetComponent<Renderer>().sharedMaterial = material;
        return cylinder;
    }

    private void BuildGasValve(Material brassMat, Material pipeMat)
    {
        CreateBox("Duvar Gaz Borusu", new Vector3(6.55f, 0.75f, -3.25f), new Vector3(0.08f, 1.5f, 0.08f), pipeMat);
        GameObject valve = CreateBox("Gaz Vanası", new Vector3(6.35f, 0.85f, -3.25f), new Vector3(0.34f, 0.14f, 0.34f), brassMat);
        PrototypeInteractable task = valve.AddComponent<PrototypeInteractable>();
        task.Configure(PrototypeAction.ShutGasValve, "Gaz vanasını kapat");
        CreateBox("Gaz Vanası Etiketi", new Vector3(6.32f, 1.18f, -3.25f), new Vector3(0.60f, 0.28f, 0.06f),
            MakeMaterial("Etiket", new Color32(210, 226, 213, 255)));
    }

    private void BuildWindowControl(Material handleMat)
    {
        GameObject handle = CreateBox("Pencere Açma Kolu", new Vector3(4.15f, 1.72f, 6.12f), new Vector3(0.32f, 0.12f, 0.12f), handleMat);
        PrototypeInteractable task = handle.AddComponent<PrototypeInteractable>();
        task.Configure(PrototypeAction.OpenWindows, "Pencereyi aç");
        task.ConfigureMove(windowGlass, new Vector3(4.45f, 1.9f, 6.38f));
    }

    private void BuildLooseProps(Material vaseMat, Material goldMat)
    {
        GameObject stool = CreateBox("Hafif Tabure", new Vector3(-2.3f, 0.42f, 4.3f), new Vector3(0.65f, 0.8f, 0.65f), MakeMaterial("Tabure", new Color32(157, 115, 78, 255)));
        Rigidbody stoolBody = stool.AddComponent<Rigidbody>();
        stoolBody.isKinematic = true;
        scenario.RegisterLooseObject(stoolBody);
    }

    private void BuildRoomDecor(Material wallMat, Material brassMat, Material trimMat)
    {
        CreateBox("Duvar Rafı", new Vector3(-2.9f, 2.2f, 6.1f), new Vector3(2.0f, 0.12f, 0.50f), trimMat);
        CreateBox("Duvar Dekoru", new Vector3(-2.9f, 2.75f, 6.22f), new Vector3(0.95f, 0.85f, 0.10f), brassMat);
        CreateBox("Televizyon Ünitesi", new Vector3(5.8f, 0.65f, 0.2f), new Vector3(0.55f, 1.3f, 2.1f), trimMat);
        CreateBox("Televizyon Ekranı", new Vector3(6.25f, 1.62f, 0.2f), new Vector3(0.14f, 1.05f, 1.8f), MakeMaterial("Ekran", new Color32(30, 49, 59, 255)));
        CreateBox("Yemek Masası", new Vector3(-2.8f, 0.78f, 0.8f), new Vector3(2.1f, 0.14f, 1.15f), wallMat);
        for (int x = -1; x <= 1; x += 2)
        for (int z = -1; z <= 1; z += 2)
            CreateBox("Yemek Masası Ayağı", new Vector3(-2.8f + x * 0.86f, 0.38f, 0.8f + z * 0.43f),
                new Vector3(0.10f, 0.76f, 0.10f), trimMat);

        GameObject lightObject = new GameObject("Salon Lambası");
        lightObject.transform.position = new Vector3(0f, 2.8f, 1.8f);
        Light point = lightObject.AddComponent<Light>();
        point.type = LightType.Point;
        point.range = 11f;
        point.intensity = 2.2f;
        point.color = new Color(1f, 0.79f, 0.58f);
        point.shadows = LightShadows.None;
        CreateBox("Lamba Sarkıtı", new Vector3(0f, 2.98f, 1.8f), new Vector3(0.42f, 0.12f, 0.42f), brassMat);
    }

    private GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = position;
        box.transform.localScale = scale;
        Renderer renderer = box.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
        return box;
    }

    private GameObject CreateChildBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localScale = localScale;
        Renderer renderer = box.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        return box;
    }

    private void SinirEkle(string ad, Vector3 konum, Vector3 boyut)
    {
        GameObject nesne = new GameObject(ad);
        nesne.transform.position = konum;
        BoxCollider collider = nesne.AddComponent<BoxCollider>();
        collider.size = boyut;
    }

    private Material MakeMaterial(string name, Color color)
    {
        if (materials.TryGetValue(name, out Material existing))
            return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.name = name;
        material.color = color;
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.28f);
        materials.Add(name, material);
        return material;
    }

    public void StartPlayableScenario()
    {
        Time.timeScale = 1f;
        gameStarted = true;
        paused = false;
        GameManager.Instance?.StartScenario();
        ui.ShowHud();
        SetPlayerControls(true);
        ShowNotice("Hazırlık görevlerini tamamla. E veya sol tıkla nesnelerle etkileş.");
    }

    public void StartEarthquake()
    {
        scenario.StartEarthquake();
    }


    public void TogglePause()
    {
        Debug.Log("ESC: TogglePause çağrıldı");

        if (!gameStarted)
        {
            Debug.Log("HATA: gameStarted false");
            return;
        }

        if (scenario == null)
        {
            Debug.Log("HATA: scenario null");
            return;
        }

        if (scenario.CurrentPhase == PrototypeScenario.Phase.Completed)
        {
            Debug.Log("HATA: Senaryo tamamlanmış");
            return;
        }

        if (paused)
            ResumeGame();
        else
        {
            paused = true;
            Time.timeScale = 0f;
            ui.ShowPause();
            Debug.Log("DURAKLATMA MENÜSÜ AÇILDI");
        }
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        paused = false;
        ui.ShowHud();
        SetPlayerControls(true);
    }

    public void RestartToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }


    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }


    public void SetPlayerControls(bool enabled)
    {
        if (player != null)
            player.SetControlsEnabled(enabled);
    }

    public void SetPrompt(string text)
    {
        if (ui != null)
            ui.SetPrompt(text);
    }

    public void UpdateTargetGuide(Camera camera, Vector3 targetPosition, bool unobstructed,
        string targetDisplayName, string actionName, float distance)
    {
        if (ui != null)
            ui.UpdateTargetGuide(camera, targetPosition, unobstructed, targetDisplayName, actionName, distance);
    }

    public void HideTargetGuide()
    {
        if (ui != null)
            ui.HideTargetGuide();
    }

    public void SetHoldZoneVisible(bool visible)
    {
        if (holdZoneMarker != null)
            holdZoneMarker.SetActive(visible);
    }

    public void ShowNotice(string text)
    {
        if (ui != null)
            ui.SetNotice(text);
    }

    public void ShowBlackout()
    {
        if (ui != null)
            ui.ShowBlackout();
    }

    public void ShowTrappedQuestion(int step, string question, string[] choices, string feedback)
    {
        if (ui != null)
            ui.ShowTrappedQuestion(step, question, choices, feedback);
    }

    public void SetTrappedFeedback(string feedback)
    {
        if (ui != null)
            ui.SetTrappedFeedback(feedback);
    }

    public void RefreshHud(string phase, string tasks, string shelter, string valve, string windows, string exit, int score, float holdProgress)
    {
        if (ui != null)
            ui.RefreshHud(phase, tasks, shelter, valve, windows, exit, score, holdProgress,
                scenario != null ? scenario.PreparationCountdownRemaining : 0f);
    }

    public void ShowResults(int score, string review)
    {
        if (ui != null)
            ui.ShowResults(score, review);
    }
}




