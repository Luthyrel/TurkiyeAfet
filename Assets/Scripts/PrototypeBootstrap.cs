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
    private GameManager.DisasterType worldDisasterType;
    private Transform floodWater;
    private Vector3 floodSafePosition;

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

        new GameObject("Oyun Sesleri").AddComponent<PrototypeAudio>();
    }

    private void Start()
    {
        Time.timeScale = 1f;
        Application.targetFrameRate = 120;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        ConfigureLighting();

        worldDisasterType = GameManager.Instance != null
            ? GameManager.Instance.SelectedDisaster : GameManager.DisasterType.Earthquake;
        if (ProvinceManager.Instance != null)
        {
            int savedPlate = ProvinceManager.Instance.GetLastProvincePlate();
            if (worldDisasterType == GameManager.DisasterType.Flood
                && !ProvinceManager.Instance.IsFloodProvince(savedPlate))
                savedPlate = ProvinceManager.Instance.GetFirstFloodProvincePlate();
            ProvinceManager.Instance.SelectProvince(savedPlate);
        }

        string scenarioName = worldDisasterType == GameManager.DisasterType.Flood
            ? "Sel ve Taşkın Senaryosu" : "Deprem Senaryosu";
        scenario = new GameObject(scenarioName).AddComponent<PrototypeScenario>();
        if (worldDisasterType == GameManager.DisasterType.Flood)
            BuildFloodWorld();
        else
            BuildWorld();

        GameObject playerObject = new GameObject("Oyuncu", typeof(CharacterController));
        player = playerObject.AddComponent<PrototypePlayerController>();
        Vector3 playerSpawn = worldDisasterType == GameManager.DisasterType.Flood
            ? new Vector3(-8.6f, 0f, -2.6f) : new Vector3(-0.4f, 0f, -2.3f);
        float playerYaw = worldDisasterType == GameManager.DisasterType.Flood ? 0f : 255f;
        player.Initialize(scenario, playerSpawn, playerYaw);
        scenario.Initialize(this, player, safeTablePosition);
        if (worldDisasterType == GameManager.DisasterType.Flood)
            scenario.ConfigureFlood(floodWater, floodSafePosition);
        player.SetControlsEnabled(false);

        ui = new GameObject("Arayüz Yöneticisi").AddComponent<PrototypeUI>();
        ui.Initialize(this);
        scenario.SendMessage("RefreshHud", SendMessageOptions.DontRequireReceiver);
        if (GameManager.Instance != null && GameManager.Instance.ConsumeStartAfterReload())
            StartPlayableScenario();
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

    private void BuildFloodWorld()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color32(119, 143, 153, 255);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.ambientSkyColor = new Color32(116, 139, 151, 255);
        RenderSettings.ambientEquatorColor = new Color32(88, 107, 114, 255);
        RenderSettings.ambientGroundColor = new Color32(49, 63, 70, 255);

        Material ground = MakeMaterial("Sel Haritası Islak Zemin", new Color32(92, 102, 95, 255));
        Material asphalt = MakeMaterial("Islak Asfalt", new Color32(55, 66, 72, 255));
        Material wall = MakeMaterial("Sel Haritası Ev Duvarı", new Color32(188, 177, 151, 255));
        Material roof = MakeMaterial("Kiremit Çatı", new Color32(133, 72, 53, 255));
        Material wood = MakeMaterial("Sel Haritası Ahşap", new Color32(103, 75, 55, 255));
        Material concrete = MakeMaterial("Beton", new Color32(123, 131, 126, 255));
        Material green = MakeMaterial("Güvenli Bölge Yeşili", new Color32(75, 146, 101, 255));
        Material sandbag = MakeMaterial("Kum Torbası", new Color32(151, 134, 98, 255));
        Material water = MakeMaterial("Yükselen Taşkın Suyu", new Color32(33, 98, 119, 200));
        water.SetOverrideTag("RenderType", "Transparent");
        if (water.HasProperty("_Surface"))
            water.SetFloat("_Surface", 1f);
        water.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        water.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        water.SetInt("_ZWrite", 0);
        water.DisableKeyword("_ALPHATEST_ON");
        water.EnableKeyword("_ALPHABLEND_ON");
        water.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        water.renderQueue = 3000;

        CreateBox("Taşkın Mahallesi Zemin", new Vector3(0f, -0.18f, 1f), new Vector3(40f, 0.36f, 32f), ground);
        CreateBox("Mahalle Yolu", new Vector3(2f, 0.015f, 3f), new Vector3(25f, 0.05f, 6f), asphalt);
        CreateBox("Yol Orta Çizgisi", new Vector3(2f, 0.05f, 3f), new Vector3(23f, 0.025f, 0.10f),
            MakeMaterial("Yol Çizgisi", new Color32(224, 204, 145, 255)));
        CreateBox("Dere Yatağı", new Vector3(1f, -0.12f, 12.5f), new Vector3(38f, 0.45f, 5.0f),
            MakeMaterial("Dere Taşı", new Color32(88, 98, 91, 255)));
        GameObject riverSurface = CreateBox("Dere Suyu", new Vector3(1f, -0.04f, 12.5f),
            new Vector3(37f, 0.04f, 4.5f), water);
        Collider riverCollider = riverSurface.GetComponent<Collider>();
        if (riverCollider != null)
            riverCollider.enabled = false;
        CreateBox("Dere Kuzey Seti", new Vector3(1f, 0.42f, 15.2f), new Vector3(38f, 0.85f, 0.45f), concrete);
        CreateBox("Dere Güney Seti", new Vector3(1f, 0.38f, 9.8f), new Vector3(38f, 0.75f, 0.42f), concrete);

        GameObject floodSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floodSurface.name = "Yükselen Sel Suyu";
        floodSurface.transform.position = new Vector3(2f, -0.04f, 2f);
        floodSurface.transform.localScale = new Vector3(37f, 0.05f, 25f);
        floodSurface.GetComponent<Renderer>().sharedMaterial = water;
        Collider surfaceCollider = floodSurface.GetComponent<Collider>();
        if (surfaceCollider != null)
            surfaceCollider.enabled = false;
        floodWater = floodSurface.transform;

        BuildFloodHouse(wall, roof, wood, concrete, sandbag, green);
        BuildFloodNeighborhood(concrete, roof, wall, wood);
        BuildFloodTasks(wood, sandbag, green);
    }

    private void BuildFloodHouse(Material wall, Material roof, Material wood, Material concrete,
        Material sandbag, Material safeMat)
    {
        const float left = -11f;
        const float right = -3f;
        const float front = 2f;
        const float back = -6f;
        CreateBox("Sel Evi Zemin Kat Döşemesi", new Vector3(-7f, 0.035f, -2f), new Vector3(8f, 0.07f, 8f), wood);
        CreateBox("Sel Evi Arka Duvarı", new Vector3(-7f, 1.35f, back), new Vector3(8f, 2.7f, 0.25f), wall);
        CreateBox("Sel Evi Sol Duvarı", new Vector3(left, 1.35f, -2f), new Vector3(0.25f, 2.7f, 8f), wall);
        CreateBox("Sel Evi Sağ Duvarı", new Vector3(right, 1.35f, -2f), new Vector3(0.25f, 2.7f, 8f), wall);
        CreateBox("Sel Evi Ön Duvarı Sol", new Vector3(-9.55f, 1.35f, front), new Vector3(2.9f, 2.7f, 0.25f), wall);
        CreateBox("Sel Evi Ön Duvarı Sağ", new Vector3(-4.45f, 1.35f, front), new Vector3(2.9f, 2.7f, 0.25f), wall);
        CreateBox("Sel Evi Kapı Üstü", new Vector3(-7f, 2.45f, front), new Vector3(2.2f, 0.5f, 0.25f), wall);
        // Split the upper floor around the stairwell so the player can actually climb through it.
        CreateBox("Sel Evi Üst Kat Döşemesi Sol", new Vector3(-8.2f, 2.78f, -2f), new Vector3(5.6f, 0.22f, 8f), wood);
        CreateBox("Sel Evi Üst Kat Döşemesi Ön", new Vector3(-7f, 2.78f, 0.4f), new Vector3(8f, 0.22f, 3.2f), wood);
        CreateBox("Sel Evi Üst Kat Döşemesi Arka", new Vector3(-7f, 2.78f, -5.85f), new Vector3(8f, 0.22f, 0.3f), wood);
        CreateBox("Sel Evi Üst Kat Sol Duvarı", new Vector3(-10.88f, 3.88f, -2f), new Vector3(0.24f, 2f, 8f), wall);
        CreateBox("Sel Evi Üst Kat Sağ Duvarı", new Vector3(-3.12f, 3.88f, -2f), new Vector3(0.24f, 2f, 8f), wall);
        CreateBox("Sel Evi Üst Kat Arka Duvarı", new Vector3(-7f, 3.88f, -5.88f), new Vector3(8f, 2f, 0.24f), wall);
        CreateBox("Sel Evi Üst Kat Ön Duvarı", new Vector3(-7f, 3.88f, 1.88f), new Vector3(8f, 2f, 0.24f), wall);
        CreateBox("Sel Evi Kırma Çatı", new Vector3(-7f, 5.15f, -2f), new Vector3(8.8f, 0.35f, 8.8f), roof);
        CreateBox("Sel Evi Üst Kat Pencere Altlığı", new Vector3(-7f, 3.55f, -5.84f), new Vector3(2.1f, 0.15f, 0.35f), wood);
        CreateBox("Üst Kat Güvenli Alan İşareti", new Vector3(-7f, 2.91f, -2f), new Vector3(2.4f, 0.035f, 2.4f), safeMat);
        BuildFloodEntryDoor(wood, concrete);
        BuildFloodStairs(wood, safeMat);
        CreateBox("Ev Ön Sundurma", new Vector3(-7f, 0.13f, 2.65f), new Vector3(4.5f, 0.22f, 1.45f), concrete);
        CreateBox("Ev Sağanak Oluk", new Vector3(-7f, 5.0f, 2.55f), new Vector3(8.2f, 0.12f, 0.16f), wood);
    }

    private void BuildFloodEntryDoor(Material wood, Material trim)
    {
        // The front wall has a 2.2 m opening. Add a recognizable frame and an open door leaf
        // so the sandbag objective has an actual threshold to protect.
        CreateBox("Giriş Kapısı Sol Sövesi", new Vector3(-8.03f, 1.05f, 1.91f),
            new Vector3(0.14f, 2.1f, 0.28f), trim);
        CreateBox("Giriş Kapısı Sağ Sövesi", new Vector3(-5.97f, 1.05f, 1.91f),
            new Vector3(0.14f, 2.1f, 0.28f), trim);
        CreateBox("Giriş Kapısı Üst Sövesi", new Vector3(-7f, 2.10f, 1.91f),
            new Vector3(2.2f, 0.14f, 0.28f), trim);

        GameObject hinge = new GameObject("Giriş Kapısı Menteşesi");
        hinge.transform.position = new Vector3(-8.0f, 0.04f, 1.94f);
        hinge.transform.rotation = Quaternion.Euler(0f, -68f, 0f);
        GameObject leaf = CreateChildBox(hinge.transform, "Açık Ahşap Giriş Kapısı",
            new Vector3(0.94f, 1.0f, 0f), new Vector3(1.88f, 2.0f, 0.12f), wood);
        Collider leafCollider = leaf.GetComponent<Collider>();
        if (leafCollider != null) leafCollider.enabled = false;
        GameObject inset = CreateChildBox(leaf.transform, "Kapı İç Paneli",
            new Vector3(0f, 0f, -0.072f), new Vector3(1.46f, 1.52f, 0.025f),
            MakeMaterial("Kapı İç Ahşap Panel", new Color32(111, 73, 48, 255)));
        Collider insetCollider = inset.GetComponent<Collider>();
        if (insetCollider != null) insetCollider.enabled = false;
        GameObject knob = CreateChildBox(leaf.transform, "Giriş Kapısı Pirinç Kolu",
            new Vector3(0.70f, 0.96f, -0.092f), new Vector3(0.07f, 0.07f, 0.05f),
            MakeMaterial("Kapı Kolu Pirinç", new Color32(188, 151, 81, 255)));
        Collider knobCollider = knob.GetComponent<Collider>();
        if (knobCollider != null) knobCollider.enabled = false;
    }

    private void BuildFloodStairs(Material wood, Material safeMat)
    {
        const int steps = 9;
        Material tread = MakeMaterial("Açık Renk Merdiven Basamağı", new Color32(191, 157, 105, 255));
        for (int i = 0; i < steps; i++)
        {
            float top = 0.30f * (i + 1);
            // The low first step faces the room; the climb rises toward the back landing.
            float z = -1.81f - i * 0.43f;
            CreateBox("Üst Kata Çıkış Basamağı " + (i + 1), new Vector3(-4.25f, top - 0.09f, z),
                new Vector3(1.7f, 0.18f, 0.62f), tread);
            GameObject stepMarker = CreateBox("Basamak Fosforlu Yeşil Ön Şeridi " + (i + 1), new Vector3(-4.25f, top + 0.012f, z + 0.23f),
                new Vector3(1.52f, 0.035f, 0.055f), safeMat);
            Collider stepMarkerCollider = stepMarker.GetComponent<Collider>();
            if (stepMarkerCollider != null) stepMarkerCollider.enabled = false;
            CreateBox("Merdiven Basamak Önü " + (i + 1), new Vector3(-4.25f, top * 0.5f, z + 0.27f),
                new Vector3(1.7f, top, 0.08f), wood);
        }
        foreach (float side in new[] { -1f, 1f })
        {
            float x = -4.25f + side * 1.02f;
            for (int i = 0; i < steps; i += 2)
            {
                float z = -1.81f - i * 0.43f;
                float top = 0.30f * (i + 1);
                CreateBox("Merdiven Korkuluk Direği", new Vector3(x, top * 0.5f + 0.12f, z),
                    new Vector3(0.08f, top + 0.24f, 0.08f), wood);
            }
            GameObject handrail = CreateBox("Merdiven Tırabzanı", new Vector3(x, 1.38f, -3.35f),
                new Vector3(0.09f, 0.09f, 4.5f), wood);
            handrail.transform.rotation = Quaternion.Euler(37f, 0f, 0f);
        }

        GameObject routeObject = new GameObject("Merdivenlere Giden Yeşil Yön Çizgisi");
        LineRenderer route = routeObject.AddComponent<LineRenderer>();
        route.useWorldSpace = true;
        route.positionCount = 3;
        route.startWidth = 0.12f;
        route.endWidth = 0.12f;
        route.material = safeMat;
        route.numCapVertices = 4;
        route.SetPosition(0, new Vector3(-8.3f, 0.09f, -2.5f));
        route.SetPosition(1, new Vector3(-6.1f, 0.09f, -2.05f));
        route.SetPosition(2, new Vector3(-4.25f, 0.09f, -1.78f));
    }

    private void BuildFloodNeighborhood(Material concrete, Material roof, Material wall, Material wood)
    {
        Material window = MakeMaterial("Sel Mahallesi Camı", new Color32(103, 155, 173, 255));
        Material door = MakeMaterial("Sel Mahallesi Kapısı", new Color32(92, 62, 43, 255));
        for (int i = 0; i < 5; i++)
        {
            float x = -1f + i * 6.3f;
            float height = 3.0f + (i % 3) * 0.6f;
            CreateBox("Mahalle Evi " + (i + 1), new Vector3(x, height * 0.5f, -7.8f),
                new Vector3(4.3f, height, 4.1f), i % 2 == 0 ? wall : concrete);
            CreateBox("Mahalle Çatısı " + (i + 1), new Vector3(x, height + 0.2f, -7.8f),
                new Vector3(4.6f, 0.35f, 4.4f), roof);
            CreateBox("Mahalle Evi Kapısı " + (i + 1), new Vector3(x, 0.93f, -5.68f),
                new Vector3(0.9f, 1.82f, 0.12f), door);
            CreateBox("Mahalle Evi Kapı Kolu " + (i + 1), new Vector3(x + 0.27f, 0.96f, -5.59f),
                new Vector3(0.07f, 0.07f, 0.035f), MakeMaterial("Kapı Kolu Pirinç", new Color32(188, 151, 81, 255)));
            for (int side = -1; side <= 1; side += 2)
            {
                CreateBox("Mahalle Evi Pencere Çerçevesi " + (i + 1), new Vector3(x + side * 1.25f, 2.05f, -5.65f),
                    new Vector3(0.98f, 0.78f, 0.12f), wood);
                CreateBox("Mahalle Evi Camı " + (i + 1), new Vector3(x + side * 1.25f, 2.05f, -5.57f),
                    new Vector3(0.78f, 0.58f, 0.05f), window);
                CreateBox("Mahalle Evi Pencere Dikey Bölmesi " + (i + 1), new Vector3(x + side * 1.25f, 2.05f, -5.52f),
                    new Vector3(0.045f, 0.58f, 0.035f), wood);
            }
        }
        for (int i = 0; i < 4; i++)
        {
            float x = -5f + i * 8f;
            CreateBox("Sokak Lambası Direği " + (i + 1), new Vector3(x, 2.1f, 5.8f),
                new Vector3(0.16f, 4.2f, 0.16f), wood);
            CreateBox("Sokak Lambası Başlığı " + (i + 1), new Vector3(x + 0.45f, 4.0f, 5.8f),
                new Vector3(1.0f, 0.14f, 0.26f), concrete);
        }
        CreateBox("Park Edilmiş Araç Gövdesi", new Vector3(2f, 0.65f, 5f), new Vector3(3.4f, 1.1f, 1.6f),
            MakeMaterial("Araç Koyu Mavi", new Color32(49, 78, 93, 255)));
        CreateBox("Araç Üst Kabini", new Vector3(2f, 1.25f, 5f), new Vector3(1.8f, 0.72f, 1.42f),
            MakeMaterial("Araç Camı", new Color32(104, 136, 145, 255)));
        Material tireMaterial = MakeMaterial("Araç Lastiği", new Color32(38, 43, 44, 255));
        Material hubMaterial = MakeMaterial("Araç Jantı", new Color32(166, 171, 163, 255));
        for (int xSide = -1; xSide <= 1; xSide += 2)
        for (int zSide = -1; zSide <= 1; zSide += 2)
        {
            Vector3 wheelPosition = new Vector3(2f + xSide * 1.05f, 0.35f, 5f + zSide * 0.66f);
            GameObject tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tire.name = "Taşkın Mahallesi Araç Lastiği";
            tire.transform.SetPositionAndRotation(wheelPosition, Quaternion.Euler(90f, 0f, 0f));
            tire.transform.localScale = new Vector3(0.34f, 0.14f, 0.34f);
            tire.GetComponent<Renderer>().sharedMaterial = tireMaterial;
            GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hub.name = "Taşkın Mahallesi Araç Jantı";
            hub.transform.SetPositionAndRotation(wheelPosition + new Vector3(0f, 0f, zSide * 0.09f),
                Quaternion.Euler(90f, 0f, 0f));
            hub.transform.localScale = new Vector3(0.18f, 0.05f, 0.18f);
            hub.GetComponent<Renderer>().sharedMaterial = hubMaterial;
        }
        CreateBox("Taşkın Tahliye Yolu", new Vector3(-0.1f, 0.18f, 1.0f), new Vector3(0.8f, 0.24f, 10f),
            MakeMaterial("Yükseltilmiş Yaya Yolu", new Color32(147, 139, 114, 255)));
    }

    private void BuildFloodTasks(Material wood, Material sandbag, Material safeMat)
    {
        GameObject breaker = new GameObject("Elektrik Ana Şalteri");
        breaker.transform.position = new Vector3(-10.72f, 1.4f, -1.5f);
        BoxCollider breakerCollider = breaker.AddComponent<BoxCollider>();
        breakerCollider.size = new Vector3(0.38f, 0.62f, 0.5f);
        CreateChildBox(breaker.transform, "Şalter Kutusu",
            Vector3.zero, new Vector3(0.22f, 0.52f, 0.42f), MakeMaterial("Şalter Kutusu", new Color32(67, 78, 81, 255)));
        GameObject breakerFace = CreateChildBox(breaker.transform, "Şalter Açık Renkli Ön Panel",
            new Vector3(0.15f, 0f, 0f), new Vector3(0.06f, 0.39f, 0.31f), MakeMaterial("Şalter Ön Paneli", new Color32(194, 198, 184, 255)));
        Collider breakerFaceCollider = breakerFace.GetComponent<Collider>();
        if (breakerFaceCollider != null) breakerFaceCollider.enabled = false;
        GameObject togglePivot = new GameObject("Şalter Kolu Menteşesi");
        togglePivot.transform.SetParent(breaker.transform, false);
        togglePivot.transform.localPosition = new Vector3(0.20f, -0.02f, 0f);
        togglePivot.transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
        GameObject breakerToggle = CreateChildBox(togglePivot.transform, "Kırmızı Ana Şalter Kolu",
            new Vector3(0f, 0.075f, 0f), new Vector3(0.10f, 0.18f, 0.09f),
            MakeMaterial("Şalter Kolu Kırmızı", new Color32(181, 61, 49, 255)));
        Collider breakerToggleCollider = breakerToggle.GetComponent<Collider>();
        if (breakerToggleCollider != null) breakerToggleCollider.enabled = false;
        GameObject breakerIndicator = CreateChildBox(breaker.transform, "Şalter Durum Işığı",
            new Vector3(0.19f, -0.13f, 0f), new Vector3(0.045f, 0.065f, 0.10f),
            MakeMaterial("Şalter Kapalı Durum Işığı", new Color32(170, 54, 43, 255)));
        Collider breakerIndicatorCollider = breakerIndicator.GetComponent<Collider>();
        if (breakerIndicatorCollider != null) breakerIndicatorCollider.enabled = false;
        PrototypeInteractable breakerTask = breaker.AddComponent<PrototypeInteractable>();
        breakerTask.Configure(PrototypeAction.CutFloodPower, "Su ulaşmadan elektriği ana şalterden kes");
        breakerTask.ConfigureFloodSwitchVisual(togglePivot.transform, breakerIndicator.GetComponent<Renderer>());
        scenario.AddFloodPreparationTask(breakerTask);

        // Upper-floor storage shelf: valuables are moved to a dry raised surface beside the
        // refuge area, rather than being dropped into the middle of the safe-zone marker.
        Material shelfMat = MakeMaterial("Üst Kat Rafı", new Color32(118, 91, 64, 255));
        CreateBox("Değerli Eşyalar İçin Yüksek Raf", new Vector3(-9.45f, 3.04f, -5.18f),
            new Vector3(1.45f, 0.12f, 0.72f), shelfMat);
        CreateBox("Yüksek Raf Sol Destek", new Vector3(-10.03f, 2.88f, -5.18f),
            new Vector3(0.10f, 0.28f, 0.64f), shelfMat);
        CreateBox("Yüksek Raf Sağ Destek", new Vector3(-8.87f, 2.88f, -5.18f),
            new Vector3(0.10f, 0.28f, 0.64f), shelfMat);

        GameObject documents = CreateBox("Evrak ve İlaç Kutusu", new Vector3(-8.8f, 0.52f, -1.1f),
            new Vector3(0.8f, 0.48f, 0.64f), wood);
        Material paper = MakeMaterial("Açık Renk Evrak", new Color32(229, 220, 190, 255));
        Material medicalPack = MakeMaterial("İlaç Paketi Beyaz", new Color32(236, 236, 222, 255));
        Material medicalMark = MakeMaterial("İlaç Paketi Kırmızı İşaret", new Color32(190, 58, 51, 255));
        GameObject papers = CreateChildBox(documents.transform, "Görünen Evrak Dosyası",
            new Vector3(-0.12f, 0.255f, 0.04f), new Vector3(0.46f, 0.035f, 0.62f), paper);
        Collider papersCollider = papers.GetComponent<Collider>();
        if (papersCollider != null) papersCollider.enabled = false;
        GameObject medicine = CreateChildBox(documents.transform, "İlk Yardım İlaç Paketi",
            new Vector3(0.20f, 0.34f, -0.20f), new Vector3(0.29f, 0.16f, 0.31f), medicalPack);
        Collider medicineCollider = medicine.GetComponent<Collider>();
        if (medicineCollider != null) medicineCollider.enabled = false;
        GameObject crossVertical = CreateChildBox(medicine.transform, "İlaç Paketi Artı İşareti Dikey",
            new Vector3(0f, 0.55f, 0f), new Vector3(0.075f, 0.025f, 0.22f), medicalMark);
        Collider crossVerticalCollider = crossVertical.GetComponent<Collider>();
        if (crossVerticalCollider != null) crossVerticalCollider.enabled = false;
        GameObject crossHorizontal = CreateChildBox(medicine.transform, "İlaç Paketi Artı İşareti Yatay",
            new Vector3(0f, 0.55f, 0f), new Vector3(0.22f, 0.025f, 0.075f), medicalMark);
        Collider crossHorizontalCollider = crossHorizontal.GetComponent<Collider>();
        if (crossHorizontalCollider != null) crossHorizontalCollider.enabled = false;
        PrototypeInteractable documentsTask = documents.AddComponent<PrototypeInteractable>();
        documentsTask.Configure(PrototypeAction.RaiseFloodValuables, "Önemli evrak ve ilaçları üst kata kaldır");
        documentsTask.ConfigureMove(documents.transform, new Vector3(-9.45f, 3.30f, -5.18f));
        scenario.AddFloodPreparationTask(documentsTask);

        GameObject barrier = new GameObject("Kapı Önü Kum Torbası Bariyeri");
        barrier.transform.position = new Vector3(-9.25f, 0.28f, 0.80f);
        BoxCollider barrierCollider = barrier.AddComponent<BoxCollider>();
        barrierCollider.size = new Vector3(1.9f, 0.56f, 0.65f);
        Material individualSandbag = sandbag != null
            ? sandbag : MakeMaterial("Kum Torbası Beji", new Color32(174, 151, 105, 255));
        AddSandbag(barrier.transform, new Vector3(-0.62f, 0.14f, 0f), individualSandbag);
        AddSandbag(barrier.transform, new Vector3(0f, 0.14f, 0f), individualSandbag);
        AddSandbag(barrier.transform, new Vector3(0.62f, 0.14f, 0f), individualSandbag);
        AddSandbag(barrier.transform, new Vector3(-0.31f, 0.40f, 0f), individualSandbag);
        AddSandbag(barrier.transform, new Vector3(0.31f, 0.40f, 0f), individualSandbag);
        PrototypeInteractable barrierTask = barrier.AddComponent<PrototypeInteractable>();
        barrierTask.Configure(PrototypeAction.PlaceFloodBarrier, "Kapı önüne kum torbası bariyerini yerleştir");
        barrierTask.ConfigureMove(barrier.transform, new Vector3(-7f, 0.24f, 2.34f));
        scenario.AddFloodPreparationTask(barrierTask);

        floodSafePosition = new Vector3(-7f, 2.88f, -2f);
        // A small emergency table gives the phone a visible, believable resting place.
        Material tableMat = MakeMaterial("Acil Telefon Sehpası", new Color32(93, 71, 53, 255));
        CreateBox("Acil Telefon Sehpası Üstü", new Vector3(-8.65f, 3.05f, -4.25f),
            new Vector3(1.25f, 0.10f, 0.72f), tableMat);
        for (int xSide = -1; xSide <= 1; xSide += 2)
        for (int zSide = -1; zSide <= 1; zSide += 2)
            CreateBox("Acil Telefon Sehpası Ayağı", new Vector3(-8.65f + xSide * 0.49f, 2.98f, -4.25f + zSide * 0.25f),
                new Vector3(0.08f, 0.22f, 0.08f), tableMat);

        GameObject report = new GameObject("Masa Üstü Acil Telefon");
        report.transform.position = new Vector3(-8.65f, 3.14f, -4.25f);
        BoxCollider phoneCollider = report.AddComponent<BoxCollider>();
        phoneCollider.size = new Vector3(0.42f, 0.26f, 0.38f);
        Material phoneMat = MakeMaterial("Acil Telefon Gövdesi", new Color32(44, 52, 56, 255));
        Material phoneAccent = MakeMaterial("Acil Telefon Tuşları", new Color32(212, 214, 202, 255));
        Material phoneRed = MakeMaterial("Acil Telefon Acil Tuşu", new Color32(188, 56, 48, 255));
        GameObject phoneBase = CreateChildBox(report.transform, "Masa Telefonu Gövdesi",
            new Vector3(0f, 0f, 0f), new Vector3(0.38f, 0.09f, 0.32f), phoneMat);
        Collider phoneBaseCollider = phoneBase.GetComponent<Collider>();
        if (phoneBaseCollider != null) phoneBaseCollider.enabled = false;
        GameObject phoneKeys = CreateChildBox(report.transform, "Telefon Tuş Takımı",
            new Vector3(0.04f, 0.052f, -0.035f), new Vector3(0.20f, 0.025f, 0.13f), phoneAccent);
        Collider phoneKeysCollider = phoneKeys.GetComponent<Collider>();
        if (phoneKeysCollider != null) phoneKeysCollider.enabled = false;
        GameObject emergencyKey = CreateChildBox(report.transform, "Telefon Acil Çağrı Tuşu",
            new Vector3(-0.105f, 0.055f, 0.075f), new Vector3(0.07f, 0.025f, 0.06f), phoneRed);
        Collider emergencyKeyCollider = emergencyKey.GetComponent<Collider>();
        if (emergencyKeyCollider != null) emergencyKeyCollider.enabled = false;
        GameObject handsetPivot = new GameObject("Telefon Ahizesi Menteşesi");
        handsetPivot.transform.SetParent(report.transform, false);
        handsetPivot.transform.localPosition = new Vector3(-0.01f, 0.11f, 0.10f);
        GameObject handset = CreateChildBox(handsetPivot.transform, "Telefon Ahizesi",
            Vector3.zero, new Vector3(0.31f, 0.07f, 0.07f), phoneMat);
        Collider handsetCollider = handset.GetComponent<Collider>();
        if (handsetCollider != null) handsetCollider.enabled = false;
        GameObject handsetEndLeft = CreateChildBox(handsetPivot.transform, "Ahize Sol Ucu",
            new Vector3(-0.15f, -0.015f, 0f), new Vector3(0.055f, 0.10f, 0.085f), phoneMat);
        Collider handsetLeftCollider = handsetEndLeft.GetComponent<Collider>();
        if (handsetLeftCollider != null) handsetLeftCollider.enabled = false;
        GameObject handsetEndRight = CreateChildBox(handsetPivot.transform, "Ahize Sağ Ucu",
            new Vector3(0.15f, -0.015f, 0f), new Vector3(0.055f, 0.10f, 0.085f), phoneMat);
        Collider handsetRightCollider = handsetEndRight.GetComponent<Collider>();
        if (handsetRightCollider != null) handsetRightCollider.enabled = false;
        PrototypeInteractable reportTask = report.AddComponent<PrototypeInteractable>();
        reportTask.Configure(PrototypeAction.CallForFloodHelp, "Telefonu al ve 112'ye konumunu bildir");
        reportTask.ConfigureFloodPhoneVisual(handsetPivot.transform);
        scenario.AddFloodAfterTask(reportTask);

        // Fixed to the rear wall over a narrow console; the screen is no longer suspended in air.
        Material consoleMat = MakeMaterial("Resmî Bilgi Konsolu", new Color32(57, 67, 69, 255));
        CreateBox("Resmî Bilgi Konsolu Rafı", new Vector3(-5.20f, 3.02f, -5.40f),
            new Vector3(1.35f, 0.12f, 0.55f), tableMat);
        GameObject allClear = CreateBox("Duvara Sabit Resmî Bilgi Ekranı", new Vector3(-5.20f, 3.55f, -5.72f),
            new Vector3(1.12f, 0.74f, 0.14f), consoleMat);
        GameObject screen = CreateBox("Resmî Bilgi Ekranı Görüntüsü", new Vector3(-5.20f, 3.57f, -5.625f),
            new Vector3(0.94f, 0.54f, 0.035f), MakeMaterial("Resmî Bilgi Ekranı Yeşil", new Color32(45, 111, 96, 255)));
        Collider screenCollider = screen.GetComponent<Collider>();
        if (screenCollider != null) screenCollider.enabled = false;
        for (int line = 0; line < 3; line++)
        {
            GameObject noticeLine = CreateBox("Duyuru Ekranı Bilgi Satırı " + (line + 1),
                new Vector3(-5.20f, 3.72f - line * 0.15f, -5.598f),
                new Vector3(0.64f - line * 0.09f, 0.025f, 0.012f),
                MakeMaterial("Ekran Açık Yazı", new Color32(202, 227, 205, 255)));
            Collider lineCollider = noticeLine.GetComponent<Collider>();
            if (lineCollider != null) lineCollider.enabled = false;
        }
        PrototypeInteractable allClearTask = allClear.AddComponent<PrototypeInteractable>();
        allClearTask.Configure(PrototypeAction.WaitForFloodAllClear, "Resmî duyuruları izle; su çekilmeden geri dönme");
        scenario.AddFloodAfterTask(allClearTask);
    }

    private void AddSandbag(Transform parent, Vector3 localPosition, Material material)
    {
        GameObject bag = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bag.name = "Tekil Kum Torbası";
        bag.transform.SetParent(parent, false);
        bag.transform.localPosition = localPosition;
        bag.transform.localScale = new Vector3(0.26f, 0.32f, 0.26f);
        bag.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Renderer renderer = bag.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        Collider collider = bag.GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
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
            CreateSmartphone(dark, radioMat, silver, yellow, white),
            CreateRadio(radioMat, yellow, silver),
            CreateFoodPack(foodMat, accent),
            CreateSpareBattery(yellow, silver, dark)
        };
        string[] names = { "Su şişesi", "İlk yardım çantası", "Cep telefonu", "Acil durum radyosu", "Uzun ömürlü gıda", "Yedek pil" };
        // Kaynak eksenler model-local; hedef yönler avuç çerçevesindedir.
        // Su şişesi korunur; asimetrik eşyalar doğru yüz/top yönü için ayrı çevrilir.
        Vector3[] gripAxes = { Vector3.up, Vector3.right, Vector3.up, Vector3.right, Vector3.right, Vector3.up };
        Vector3[] gripDirections = { Vector3.up, Vector3.down, Vector3.up, Vector3.down, Vector3.down, Vector3.up };
        Vector3[] gripFaceAxes = { Vector3.back, Vector3.back, Vector3.back, Vector3.back, Vector3.back, Vector3.back };
        // Sabit yerler: bazıları mobilya altı/arkası; hepsi zeminde ve erişilebilir.
        Vector3[] floorSpots =
        {
            new Vector3(-3.10f, 0f, 0.92f),  // Yemek masasının altında
            new Vector3(3.8f, 0f, 2.6f),    // Masalardan uzakta, açık zeminde
            new Vector3(6.05f, 0f, -3.65f),  // Gaz vanasının gerisinde
            new Vector3(-4.05f, 0f, -3.70f), // Ayakkabılığın yanında
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

    private GameObject CreateSmartphone(Material bodyMaterial, Material screenMaterial, Material metalMaterial,
        Material accentMaterial, Material lightMaterial)
    {
        GameObject root = CreateBagItemRoot();
        CreateChildBox(root.transform, "Telefon Gövdesi", new Vector3(0f, 0.16f, 0f),
            new Vector3(0.18f, 0.31f, 0.035f), bodyMaterial);
        CreateChildBox(root.transform, "Telefon Ekranı", new Vector3(0f, 0.17f, -0.020f),
            new Vector3(0.15f, 0.235f, 0.008f), screenMaterial);
        CreateChildBox(root.transform, "Ekran Durum Çubuğu", new Vector3(0f, 0.272f, -0.025f),
            new Vector3(0.12f, 0.012f, 0.004f), bodyMaterial);
        CreateChildBox(root.transform, "Telefon Arama Kartı", new Vector3(0f, 0.19f, -0.026f),
            new Vector3(0.10f, 0.07f, 0.004f), accentMaterial);
        CreateChildBox(root.transform, "Telefon Alt Menü", new Vector3(0f, 0.075f, -0.026f),
            new Vector3(0.11f, 0.025f, 0.004f), bodyMaterial);
        CreateChildBox(root.transform, "Telefon Hoparlör Izgarası", new Vector3(0f, 0.303f, -0.022f),
            new Vector3(0.045f, 0.006f, 0.004f), metalMaterial);
        CreateChildCylinder(root.transform, "Ön Kamera", new Vector3(0.042f, 0.303f, -0.024f),
            new Vector3(0.008f, 0.003f, 0.008f), lightMaterial, Quaternion.Euler(90f, 0f, 0f));
        CreateChildCylinder(root.transform, "Ana Ekran Düğmesi", new Vector3(0f, 0.015f, -0.021f),
            new Vector3(0.012f, 0.003f, 0.012f), metalMaterial, Quaternion.Euler(90f, 0f, 0f));
        CreateChildBox(root.transform, "Ses Düğmesi", new Vector3(-0.094f, 0.22f, 0f),
            new Vector3(0.008f, 0.045f, 0.012f), metalMaterial);
        CreateChildBox(root.transform, "Güç Düğmesi", new Vector3(0.094f, 0.22f, 0f),
            new Vector3(0.008f, 0.035f, 0.012f), metalMaterial);
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

    private GameObject CreateSpareBattery(Material bodyMaterial, Material terminalMaterial, Material labelMaterial)
    {
        GameObject root = CreateBagItemRoot();
        CreateChildBox(root.transform, "Yedek Pil Gövdesi", new Vector3(0f, 0.14f, 0f),
            new Vector3(0.19f, 0.27f, 0.12f), bodyMaterial);
        CreateChildBox(root.transform, "Pil Ön Etiketi", new Vector3(0f, 0.14f, -0.063f),
            new Vector3(0.145f, 0.15f, 0.008f), labelMaterial);
        CreateChildBox(root.transform, "Pil Artı İşareti Dikey", new Vector3(-0.035f, 0.16f, -0.069f),
            new Vector3(0.012f, 0.05f, 0.006f), bodyMaterial);
        CreateChildBox(root.transform, "Pil Artı İşareti Yatay", new Vector3(-0.035f, 0.16f, -0.070f),
            new Vector3(0.05f, 0.012f, 0.006f), bodyMaterial);
        CreateChildBox(root.transform, "Pil Eksi İşareti", new Vector3(0.037f, 0.16f, -0.069f),
            new Vector3(0.045f, 0.012f, 0.006f), bodyMaterial);
        CreateChildBox(root.transform, "Pil Alt Dikişi", new Vector3(0f, 0.035f, -0.062f),
            new Vector3(0.15f, 0.012f, 0.008f), terminalMaterial);
        CreateChildCylinder(root.transform, "Pozitif Pil Kutbu", new Vector3(-0.045f, 0.29f, 0f),
            new Vector3(0.035f, 0.018f, 0.035f), terminalMaterial);
        CreateChildCylinder(root.transform, "Negatif Pil Kutbu", new Vector3(0.045f, 0.285f, 0f),
            new Vector3(0.035f, 0.012f, 0.035f), terminalMaterial);
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
        if (GameManager.Instance != null && GameManager.Instance.SelectedDisaster != worldDisasterType)
        {
            GameManager.Instance.RequestStartAfterReload();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

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

    public void ShowFloodDialogue(string title, string body, string[] options, string feedback)
    {
        if (ui != null)
            ui.ShowFloodDialogue(title, body, options, feedback);
    }

    public void HideFloodDialogue()
    {
        if (ui != null)
            ui.HideFloodDialogue();
    }

    public void ShowFloodDialogue(string title, string body, string[] options, string feedback)
    {
        if (ui != null)
            ui.ShowFloodDialogue(title, body, options, feedback);
    }

    public void HideFloodDialogue()
    {
        if (ui != null)
            ui.HideFloodDialogue();
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




