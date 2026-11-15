using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public sealed class PrototypeBootstrap : MonoBehaviour
{
    public static PrototypeBootstrap Instance { get; private set; }

    private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    private sealed class LandslideDebrisBody
    {
        public Rigidbody Body;
        public float ReleaseAt;
        public Vector3 Impulse;
        public Vector3 StartPosition;
        public Quaternion StartRotation;
        public bool Released;
    }

    private readonly List<LandslideDebrisBody> landslideDebris = new List<LandslideDebrisBody>();
    private readonly List<Rigidbody> landslideTrees = new List<Rigidbody>();
    private readonly List<Rigidbody> landslideStructure = new List<Rigidbody>();
    private readonly List<float> landslideTreeReleaseTimes = new List<float>();
    private readonly List<float> landslideStructureReleaseTimes = new List<float>();
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
    private Vector3 landslideSafePosition;
    private ParticleSystem landslideDust;
    private int lastLandslideDustBurst = -1;

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
            if (ProvinceManager.Instance.GetPrimaryDisasterForProvince(savedPlate) != worldDisasterType)
                savedPlate = worldDisasterType switch
                {
                    GameManager.DisasterType.Flood => ProvinceManager.Instance.GetFirstFloodProvincePlate(),
                    GameManager.DisasterType.Landslide => ProvinceManager.Instance.GetFirstLandslideTrainingProvincePlate(),
                    _ => 6
                };
            ProvinceManager.Instance.SelectProvince(savedPlate);
        }

        string scenarioName = worldDisasterType switch
        {
            GameManager.DisasterType.Flood => "Sel ve Taşkın Senaryosu",
            GameManager.DisasterType.Landslide => "Heyelan Farkındalık Senaryosu",
            _ => "Deprem Senaryosu"
        };
        scenario = new GameObject(scenarioName).AddComponent<PrototypeScenario>();
        if (worldDisasterType == GameManager.DisasterType.Flood)
            BuildFloodWorld();
        else if (worldDisasterType == GameManager.DisasterType.Landslide)
            BuildLandslideWorld();
        else
            BuildWorld();

        GameObject playerObject = new GameObject("Oyuncu", typeof(CharacterController));
        player = playerObject.AddComponent<PrototypePlayerController>();
        Vector3 playerSpawn = worldDisasterType switch
        {
            GameManager.DisasterType.Flood => new Vector3(-8.6f, 0f, -2.6f),
            GameManager.DisasterType.Landslide => new Vector3(0f, 0f, 10f),
            _ => new Vector3(-0.4f, 0f, -2.3f)
        };
        float playerYaw = worldDisasterType switch
        {
            GameManager.DisasterType.Flood => 0f,
            GameManager.DisasterType.Landslide => 180f,
            _ => 255f
        };
        player.Initialize(scenario, playerSpawn, playerYaw);
        scenario.Initialize(this, player, safeTablePosition);
        if (worldDisasterType == GameManager.DisasterType.Flood)
            scenario.ConfigureFlood(floodWater, floodSafePosition);
        else if (worldDisasterType == GameManager.DisasterType.Landslide)
            scenario.ConfigureLandslide(landslideSafePosition);
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

        CreateBox("Taşkın Mahallesi Zemin", new Vector3(0f, -0.18f, 1f), new Vector3(56f, 0.36f, 32f), ground);
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

        BuildFloodHouse(wall, roof, wood, concrete, green);
        BuildFloodNeighborhood(concrete, roof, wall, wood);
        BuildFloodTasks(wood, green);
    }

    private void BuildLandslideWorld()
    {
        landslideDebris.Clear();
        landslideTrees.Clear();
        landslideStructure.Clear();
        landslideTreeReleaseTimes.Clear();
        landslideStructureReleaseTimes.Clear();
        landslideSafePosition = new Vector3(17f, 0f, 8f);

        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color32(166, 177, 176, 255);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0042f;
        RenderSettings.ambientSkyColor = new Color32(170, 190, 203, 255);
        RenderSettings.ambientEquatorColor = new Color32(116, 132, 128, 255);
        RenderSettings.ambientGroundColor = new Color32(58, 65, 57, 255);
        RenderSettings.ambientIntensity = 0.98f;

        Material meadow = MakeMaterial("Heyelan Dağ Çayırı", new Color32(77, 103, 76, 255));
        Material grassLight = MakeMaterial("Yamaç Güneş Alan Otlar", new Color32(105, 124, 83, 255));
        Material grassDark = MakeMaterial("Yamaç Gölgeli Otlar", new Color32(57, 77, 62, 255));
        Material soil = MakeMaterial("Nemli Yamaç Toprağı", new Color32(107, 78, 55, 255));
        Material clay = MakeMaterial("Killi Toprak Katmanı", new Color32(137, 93, 63, 255));
        Material shale = MakeMaterial("Açıkta Kalan Şist", new Color32(92, 94, 87, 255));
        Material stone = MakeMaterial("Yamaç Kayası", new Color32(106, 108, 100, 255));
        Material stoneLight = MakeMaterial("Açık Gri Kayalar", new Color32(132, 133, 123, 255));
        Material trunk = MakeMaterial("Islak Ağaç Kabuğu", new Color32(75, 57, 42, 255));
        Material pine = MakeMaterial("Dağ Çamı İğneleri", new Color32(47, 78, 58, 255));
        Material pineLight = MakeMaterial("Dağ Çamı Güneşli İğneleri", new Color32(68, 100, 67, 255));
        Material deciduous = MakeMaterial("Yapraklı Ağaç Tacı", new Color32(83, 110, 67, 255));
        Material wetMud = MakeMaterial("Akışkan Koyu Çamur", new Color32(91, 67, 48, 255));
        Material freshMud = MakeMaterial("Yeni Kopmuş Islak Toprak", new Color32(126, 88, 59, 255));
        Material road = MakeMaterial("Dağ Köyü Asfaltı", new Color32(55, 62, 61, 255));
        Material roadEdge = MakeMaterial("Yol Kenarı Taşı", new Color32(133, 132, 117, 255));
        Material wall = MakeMaterial("Köy Evi Sıvası", new Color32(185, 169, 137, 255));
        Material roof = MakeMaterial("Köy Evi Kiremiti", new Color32(128, 76, 57, 255));
        Material glass = MakeMaterial("Pencere Camı", new Color32(80, 126, 137, 255));
        Material safeGreen = MakeMaterial("Acil Toplanma Alanı Yeşili", new Color32(56, 140, 111, 255));
        Material amber = MakeMaterial("Acil Yönlendirme Sarısı", new Color32(220, 175, 78, 255));
        Material emergencyRed = MakeMaterial("Acil Çağrı Kırmızısı", new Color32(182, 59, 48, 255));
        Material darkMetal = MakeMaterial("Acil Çağrı Cihazı Metal", new Color32(39, 52, 54, 255));
        Material screen = MakeMaterial("Acil Çağrı Ekranı", new Color32(45, 104, 92, 255));

        CreateBox("Dağ Vadisi Alt Zemini", new Vector3(0f, -0.28f, 2f), new Vector3(52f, 0.56f, 54f), meadow);
        BuildLandslideMountain(meadow, grassLight, grassDark, soil, clay, shale, stone, stoneLight, trunk, pine, pineLight, deciduous);
        BuildLandslideRoad(road, roadEdge);
        BuildLandslideVillage(wall, roof, glass, trunk);
        BuildLandslideVegetation(meadow, trunk, pine, pineLight, deciduous, stone, stoneLight);
        BuildLandslideClues(trunk, grassDark, soil, clay, shale);
        BuildLandslideRefuge(safeGreen, amber, trunk, darkMetal, screen, emergencyRed);
        BuildLandslideDebris(soil, clay, shale, stone, stoneLight, wetMud, freshMud);
        CreateLandslideDust();
    }

    private void BuildLandslideMountain(Material grass, Material grassLight, Material grassDark,
        Material soil, Material clay, Material shale, Material stone, Material stoneLight,
        Material trunk, Material pine, Material pineLight, Material deciduous)
    {
        const int columns = 42;
        const int rows = 28;
        const float minX = -21f;
        const float maxX = 21f;
        const float rearZ = -22f;
        const float toeZ = -2f;
        Vector3[] vertices = new Vector3[(columns + 1) * (rows + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[columns * rows * 6];
        int vertex = 0;
        for (int zIndex = 0; zIndex <= rows; zIndex++)
        {
            float z = Mathf.Lerp(rearZ, toeZ, zIndex / (float)rows);
            for (int xIndex = 0; xIndex <= columns; xIndex++)
            {
                float x = Mathf.Lerp(minX, maxX, xIndex / (float)columns);
                vertices[vertex] = new Vector3(x, LandslideSlopeHeight(x, z), z);
                uv[vertex] = new Vector2(xIndex / (float)columns * 8f, zIndex / (float)rows * 6f);
                vertex++;
            }
        }
        int triangle = 0;
        for (int zIndex = 0; zIndex < rows; zIndex++)
        {
            for (int xIndex = 0; xIndex < columns; xIndex++)
            {
                int a = zIndex * (columns + 1) + xIndex;
                int b = a + columns + 1;
                triangles[triangle++] = a; triangles[triangle++] = b; triangles[triangle++] = a + 1;
                triangles[triangle++] = a + 1; triangles[triangle++] = b; triangles[triangle++] = b + 1;
            }
        }
        Mesh mountainMesh = new Mesh { name = "Düzensiz Doğal Dağ Yamacı" };
        mountainMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mountainMesh.vertices = vertices;
        mountainMesh.uv = uv;
        mountainMesh.triangles = triangles;
        mountainMesh.RecalculateNormals();
        mountainMesh.RecalculateBounds();
        GameObject mountain = new GameObject("Yüksek Kayalık Dağ Yamacı");
        mountain.AddComponent<MeshFilter>().sharedMesh = mountainMesh;
        mountain.AddComponent<MeshRenderer>().sharedMaterial = grass;
        mountain.AddComponent<MeshCollider>().sharedMesh = mountainMesh;

        // Irregular exposed rock and clay scars follow the slope rather than forming flat horizontal bands.
        Vector2[] rockPatches = { new Vector2(-14f,-9f), new Vector2(-10f,-15f), new Vector2(-4f,-11f), new Vector2(2f,-17f), new Vector2(8f,-12f), new Vector2(14f,-18f), new Vector2(16f,-8f), new Vector2(-17f,-17f), new Vector2(5f,-7f) };
        for (int i = 0; i < rockPatches.Length; i++)
        {
            float radiusX = 1.6f + (i % 3) * 0.75f;
            float radiusZ = 1.1f + (i % 2) * 0.8f;
            CreateLandslideSlopePatch("Düzensiz Kaya Yüzleği " + (i + 1), rockPatches[i], radiusX, radiusZ,
                i % 2 == 0 ? shale : stone, i * 47 + 11, 0.06f);
        }
        Vector2[] soilScars = { new Vector2(-8f,-5.2f), new Vector2(1.7f,-8.3f), new Vector2(11f,-5.9f), new Vector2(-2f,-15.5f) };
        for (int i = 0; i < soilScars.Length; i++)
            CreateLandslideSlopePatch("Açıkta Kalan Killi Toprak " + i, soilScars[i], 1.6f + (i % 2) * 0.8f, 1.2f,
                i % 2 == 0 ? clay : soil, 900 + i * 29, 0.09f);

        // Retaining trees and rock outcrops add scale across the uneven mountainside.
        for (int row = 0; row < 4; row++)
        {
            float z = -18f + row * 3.1f;
            for (int column = 0; column < 9; column++)
            {
                float x = -18f + column * 4.45f + Mathf.Sin((row + 1) * (column + 2)) * 0.8f;
                if (Mathf.Abs(x) < 2.8f && z > -13f)
                    continue;
                float y = LandslideSlopeHeight(x, z);
                if (y < 2f)
                    continue;
                CreateLandslideTree(new Vector3(x, y, z), row * 11 + column, trunk,
                    (row + column) % 3 == 0 ? deciduous : pine, (row + column) % 2 == 0 ? pineLight : grassDark,
                    3.2f + ((row + column) % 4) * 0.45f, false, 0f);
            }
        }

        for (int i = 0; i < 24; i++)
        {
            float x = -18.5f + ((i * 7.31f) % 37f);
            float z = -20.4f + ((i * 3.17f) % 15f);
            Vector3 scale = new Vector3(0.55f + (i % 4) * 0.25f, 0.45f + (i % 3) * 0.27f, 0.5f + (i % 5) * 0.18f);
            float y = LandslideSlopeHeight(x, z) + scale.y * 0.28f;
            GameObject rock = CreateLandslideRock("Yamaçtaki Düzensiz Kaya Çıkıntısı " + i,
                new Vector3(x, y, z), scale, i % 3 == 0 ? stoneLight : stone, false, 0f, i + 401);
        }
    }

    private float LandslideSlopeHeight(float x, float z)
    {
        float rise = Mathf.Clamp01(Mathf.InverseLerp(-2.5f, -22f, z));
        float broadRidge = Mathf.Sin(x * 0.22f + z * 0.12f) * 0.55f
            + Mathf.Sin(x * 0.43f - z * 0.19f) * 0.24f
            + (Mathf.PerlinNoise((x + 31f) * 0.12f, (z + 29f) * 0.105f) - 0.5f) * 1.2f;
        return Mathf.Max(0.22f, 0.32f + rise * (12.4f + broadRidge));
    }

    private void CreateLandslideSlopePatch(string name, Vector2 center, float radiusX, float radiusZ,
        Material material, int seed, float lift)
    {
        const int segments = 11;
        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];
        vertices[0] = new Vector3(center.x, LandslideSlopeHeight(center.x, center.y) + lift, center.y);
        UnityEngine.Random.State oldState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(seed);
        Vector2[] edge = new Vector2[segments];
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            float uneven = UnityEngine.Random.Range(0.72f, 1.22f);
            edge[i] = new Vector2(center.x + Mathf.Cos(angle) * radiusX * uneven,
                center.y + Mathf.Sin(angle) * radiusZ * uneven);
            vertices[i + 1] = new Vector3(edge[i].x, LandslideSlopeHeight(edge[i].x, edge[i].y) + lift, edge[i].y);
            int next = (i + 1) % segments;
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = next + 1;
            triangles[i * 3 + 2] = i + 1;
        }
        UnityEngine.Random.state = oldState;
        GameObject patch = new GameObject(name);
        Mesh mesh = new Mesh { name = name + " Mesh" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        patch.AddComponent<MeshFilter>().sharedMesh = mesh;
        patch.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private void BuildLandslideRoad(Material road, Material edge)
    {
        const int segments = 30;
        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        int[] triangles = new int[segments * 6];
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float x = Mathf.Lerp(-24f, 24f, t);
            float z = 0.4f + Mathf.Sin(t * Mathf.PI * 2.3f) * 1.35f + Mathf.Sin(t * Mathf.PI * 7f) * 0.24f;
            vertices[i * 2] = new Vector3(x, 0.035f, z - 1.8f);
            vertices[i * 2 + 1] = new Vector3(x, 0.035f, z + 1.8f);
            if (i < segments)
            {
                int a = i * 2;
                int b = a + 2;
                int index = i * 6;
                triangles[index] = a; triangles[index + 1] = a + 1; triangles[index + 2] = b;
                triangles[index + 3] = a + 1; triangles[index + 4] = b + 1; triangles[index + 5] = b;
            }
        }
        GameObject roadObject = new GameObject("Dağ Köyüne Giden Kıvrımlı Yol");
        Mesh mesh = new Mesh { name = "Kıvrımlı Köy Yolu Mesh" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        roadObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        roadObject.AddComponent<MeshRenderer>().sharedMaterial = road;
        roadObject.AddComponent<MeshCollider>().sharedMesh = mesh;
        for (int i = 0; i < 17; i++)
        {
            float t = (i + 0.5f) / 17f;
            float x = Mathf.Lerp(-23f, 23f, t);
            float z = 0.4f + Mathf.Sin(t * Mathf.PI * 2.3f) * 1.35f + Mathf.Sin(t * Mathf.PI * 7f) * 0.24f;
            CreateBox("Yol Kenarı Düzensiz Taş " + i, new Vector3(x, 0.14f, z + (i % 2 == 0 ? -2.0f : 2.0f)),
                new Vector3(0.32f + (i % 3) * 0.12f, 0.24f, 0.25f), edge).transform.rotation = Quaternion.Euler(0f, i * 31f, 0f);
        }
    }

    private void BuildLandslideVillage(Material wall, Material roof, Material glass, Material timber)
    {
        CreateLandslideVillageHouse(new Vector3(-15f, 0f, 4.5f), wall, roof, glass, timber, false);
        // Keep the vulnerable house beside the debris runout so the staged slide can reach it.
        // The clear lateral route to the refuge remains open on the eastern side of the valley.
        CreateLandslideVillageHouse(new Vector3(10f, 0f, 3.3f), wall, roof, glass, timber, true);
    }

    private void CreateLandslideVillageHouse(Vector3 center, Material wall, Material roof, Material glass,
        Material timber, bool vulnerable)
    {
        float width = vulnerable ? 5.0f : 4.4f;
        float depth = vulnerable ? 4.3f : 4.0f;
        CreateBox("Dağ Köyü Evi Temel Taşı", center + new Vector3(0f, 0.16f, 0f),
            new Vector3(width + 0.35f, 0.32f, depth + 0.35f), timber);
        CreateBox("Dağ Köyü Evi Arka Duvarı", center + new Vector3(0f, 1.45f, -depth * 0.5f),
            new Vector3(width, 2.9f, 0.18f), wall);
        CreateBox("Dağ Köyü Evi Sol Duvarı", center + new Vector3(-width * 0.5f, 1.45f, 0f),
            new Vector3(0.18f, 2.9f, depth), wall);
        CreateBox("Dağ Köyü Evi Sağ Duvarı", center + new Vector3(width * 0.5f, 1.45f, 0f),
            new Vector3(0.18f, 2.9f, depth), wall);

        if (vulnerable)
        {
            GameObject frontPanel = CreateBox("Heyelanda Hasar Alabilecek Ev Ön Duvarı", center + new Vector3(0f, 1.45f, depth * 0.5f),
                new Vector3(width, 2.9f, 0.18f), wall);
            Rigidbody panelBody = frontPanel.AddComponent<Rigidbody>();
            panelBody.isKinematic = true;
            panelBody.useGravity = false;
            panelBody.mass = 180f;
            landslideStructure.Add(panelBody);
            landslideStructureReleaseTimes.Add(7.1f);
            CreateBox("Heyelan Riski Altındaki Evin Penceresi", center + new Vector3(0.65f, 1.9f, depth * 0.5f + 0.1f),
                new Vector3(1.1f, 0.85f, 0.05f), glass);
            GameObject roofPanel = CreateBox("Heyelanda Yerinden Oynayan Kiremit Çatı", center + new Vector3(0f, 3.0f, 0f),
                new Vector3(width + 0.7f, 0.24f, depth + 0.65f), roof);
            Rigidbody roofBody = roofPanel.AddComponent<Rigidbody>();
            roofBody.isKinematic = true;
            roofBody.useGravity = false;
            roofBody.mass = 110f;
            landslideStructure.Add(roofBody);
            landslideStructureReleaseTimes.Add(8.0f);
        }
        else
        {
            CreateBox("Köy Evi Ön Kapısı", center + new Vector3(-0.75f, 0.95f, depth * 0.5f + 0.08f),
                new Vector3(0.82f, 1.9f, 0.08f), timber);
            CreateBox("Köy Evi Penceresi", center + new Vector3(0.8f, 1.85f, depth * 0.5f + 0.1f),
                new Vector3(1.05f, 0.82f, 0.05f), glass);
            CreateBox("Köy Evi Kiremit Çatısı", center + new Vector3(0f, 3.0f, 0f),
                new Vector3(width + 0.7f, 0.25f, depth + 0.65f), roof).transform.rotation = Quaternion.Euler(0f, 0f, 4f);
        }
    }

    private void BuildLandslideVegetation(Material meadow, Material trunk, Material pine,
        Material pineLight, Material deciduous, Material rock, Material rockLight)
    {
        for (int i = 0; i < 11; i++)
        {
            float x = -23f + (i * 4.37f) % 46f;
            float z = -1.5f + (i % 4) * 2.4f;
            if (x > 12.5f && z > 4f)
                continue;
            bool leaningClue = i == 2;
            CreateLandslideTree(new Vector3(x, 0f, z), 100 + i, trunk,
                i % 3 == 0 ? deciduous : pine, pineLight, 3.3f + (i % 4) * 0.5f, true,
                leaningClue ? -12f : 0f);
            if (i == 1 || i == 4 || i == 7 || i == 9)
            {
                float size = 0.65f + (i % 3) * 0.34f;
                CreateLandslideRock("Vadi Kenarı Düzensiz Kaya " + i,
                    new Vector3(x * 0.72f, size * 0.42f, z + 1.1f), new Vector3(size * 1.4f, size, size * 1.2f),
                    i % 2 == 0 ? rock : rockLight, false, 0f, 1200 + i);
            }
        }

        for (int i = 0; i < 25; i++)
        {
            float x = -23f + ((i * 11.9f) % 46f);
            float z = -2.5f + ((i * 6.37f) % 25f);
            if (Mathf.Abs(x) > 20f || (x > 13f && z > 5f))
                continue;
            float size = 0.3f + (i % 5) * 0.11f;
            GameObject shrub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shrub.name = "Vadi Çalısı " + i;
            shrub.transform.position = new Vector3(x, size * 0.45f, z);
            shrub.transform.localScale = new Vector3(size * 1.8f, size * 0.8f, size * 1.35f);
            shrub.GetComponent<Renderer>().sharedMaterial = i % 3 == 0 ? pineLight : meadow;
            Collider shrubCollider = shrub.GetComponent<Collider>();
            if (shrubCollider != null)
                shrubCollider.enabled = false;
        }
    }

    private Rigidbody CreateLandslideTree(Vector3 position, int variation, Material trunkMaterial,
        Material canopyMaterial, Material canopyLight, float height, bool canFall, float leanDegrees)
    {
        GameObject tree = new GameObject("Dağ Çamı ve Yapraklı Ağaç " + variation);
        tree.transform.position = position;
        tree.transform.rotation = Quaternion.Euler(0f, variation * 37f, leanDegrees);
        float width = 0.18f + (variation % 3) * 0.035f;
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Ağaç Gövdesi";
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
        trunk.transform.localScale = new Vector3(width, height * 0.5f, width);
        trunk.GetComponent<Renderer>().sharedMaterial = trunkMaterial;
        Collider trunkCollider = trunk.GetComponent<Collider>();
        if (trunkCollider != null)
            trunkCollider.enabled = false;

        for (int cluster = 0; cluster < 5; cluster++)
        {
            GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage.name = "Düzensiz Yaprak Kümesi " + cluster;
            foliage.transform.SetParent(tree.transform, false);
            foliage.transform.localPosition = new Vector3(Mathf.Sin(cluster * 2.2f + variation) * 0.36f,
                height * (0.62f + (cluster % 3) * 0.12f), Mathf.Cos(cluster * 1.7f + variation) * 0.3f);
            float crown = 0.8f + (cluster % 2) * 0.32f;
            foliage.transform.localScale = new Vector3(crown, crown * 0.92f, crown);
            foliage.GetComponent<Renderer>().sharedMaterial = cluster % 2 == 0 ? canopyMaterial : canopyLight;
            Collider foliageCollider = foliage.GetComponent<Collider>();
            if (foliageCollider != null)
                foliageCollider.enabled = false;
        }

        Rigidbody body = tree.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.mass = 85f + variation % 5 * 25f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        CapsuleCollider capsule = tree.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, height * 0.5f, 0f);
        capsule.height = height;
        capsule.radius = width * 1.2f;
        if (canFall)
        {
            landslideTrees.Add(body);
            landslideTreeReleaseTimes.Add(4.5f + (variation % 4) * 0.85f);
        }
        return body;
    }

    private void BuildLandslideClues(Material trunk, Material grassDark, Material soil, Material clay, Material shale)
    {
        GameObject cracks = new GameObject("İnceleme Noktası - Yeni Oluşmuş Zemin Çatlakları");
        const float crackX = -4.2f;
        const float crackZ = -3.25f;
        cracks.transform.position = new Vector3(crackX, LandslideSlopeHeight(crackX, crackZ) + 0.18f, crackZ);
        BoxCollider crackCollider = cracks.AddComponent<BoxCollider>();
        crackCollider.size = new Vector3(1.8f, 0.32f, 1.5f);
        PrototypeInteractable crackTask = cracks.AddComponent<PrototypeInteractable>();
        crackTask.Configure(PrototypeAction.InspectLandslideWarnings, "Genişleyen zemin çatlaklarını incele");
        Material fissure = MakeMaterial("Zemindeki Koyu Fissürler", new Color32(47, 43, 36, 255));
        Material rawEdge = MakeMaterial("Çatlak Kenarında Taze Toprak", new Color32(146, 104, 70, 255));
        for (int line = 0; line < 5; line++)
        {
            float angle = -12f + line * 9f;
            float x = -0.48f + line * 0.22f;
            float crackWorldX = cracks.transform.position.x + x;
            float crackWorldZ = cracks.transform.position.z + (line % 2 == 0 ? -0.12f : 0.12f);
            GameObject crack = CreateBox("Düzensiz Çatlak İzi " + line,
                new Vector3(crackWorldX, LandslideSlopeHeight(crackWorldX, crackWorldZ) + 0.035f, crackWorldZ),
                new Vector3(1.16f - (line % 3) * 0.12f, 0.035f, 0.045f + (line % 2) * 0.025f), fissure);
            crack.transform.rotation = Quaternion.Euler(0f, angle, 0f);
            Collider fissureCollider = crack.GetComponent<Collider>();
            if (fissureCollider != null) fissureCollider.enabled = false;
            GameObject edge = CreateBox("Çatlak Kenarındaki Açık Toprak " + line,
                crack.transform.position + new Vector3(-0.075f, 0.012f, 0.05f), new Vector3(0.06f, 0.025f, 0.88f), rawEdge);
            Collider edgeCollider = edge.GetComponent<Collider>();
            if (edgeCollider != null) edgeCollider.enabled = false;
        }
        scenario.AddLandslidePreparationTask(crackTask);

        const float leaningTreeX = -10.1f;
        const float leaningTreeZ = -2.85f;
        Rigidbody leaningTree = CreateLandslideTree(new Vector3(leaningTreeX,
            LandslideSlopeHeight(leaningTreeX, leaningTreeZ), leaningTreeZ), 303, trunk,
            MakeMaterial("Eğik Ağacın Yaprakları", new Color32(70, 91, 56, 255)), grassDark, 4.0f, false, 18f);
        PrototypeInteractable treeClue = leaningTree.gameObject.AddComponent<PrototypeInteractable>();
        treeClue.Configure(PrototypeAction.InspectLandslideWarnings, "Kök çevresi açılmış eğik ağacı incele");
        scenario.AddLandslidePreparationTask(treeClue);

        GameObject seepage = new GameObject("İnceleme Noktası - Yamaçtan Yeni Çıkan Su");
        const float seepageX = 4.0f;
        const float seepageZ = -3.15f;
        seepage.transform.position = new Vector3(seepageX, LandslideSlopeHeight(seepageX, seepageZ) + 0.16f, seepageZ);
        BoxCollider seepageCollider = seepage.AddComponent<BoxCollider>();
        seepageCollider.size = new Vector3(1.7f, 0.28f, 1.35f);
        PrototypeInteractable seepageTask = seepage.AddComponent<PrototypeInteractable>();
        seepageTask.Configure(PrototypeAction.InspectLandslideWarnings, "Yamaç eteğindeki yeni su sızıntısını incele");
        CreateLandslideSlopePatch("Sızıntının Islak Toprağı", new Vector2(seepageX, seepageZ), 1.1f, 0.75f, soil, 755, 0.12f);
        for (int i = 0; i < 4; i++)
        {
            float puddleX = seepageX - 0.3f + i * 0.18f;
            float puddleZ = seepageZ - 0.3f + i * 0.16f;
            GameObject water = CreateBox("Yamaç Eteğindeki Su Birikintisi " + i,
                new Vector3(puddleX, LandslideSlopeHeight(puddleX, puddleZ) + 0.035f, puddleZ),
                new Vector3(0.55f - i * 0.04f, 0.018f, 0.24f), shale);
            water.transform.rotation = Quaternion.Euler(0f, i * 22f, 0f);
            Collider waterCollider = water.GetComponent<Collider>();
            if (waterCollider != null) waterCollider.enabled = false;
        }
        GameObject waterTrail = CreateBox("Sızıntıdan Akan İnce Su İzi",
            new Vector3(3.35f, LandslideSlopeHeight(3.35f, -3.55f) + 0.028f, -3.55f),
            new Vector3(0.16f, 0.018f, 0.72f), MakeMaterial("Soğuk Yamaç Suyu", new Color32(90, 130, 137, 255)));
        waterTrail.transform.rotation = Quaternion.Euler(0f, 12f, 0f);
        Collider waterTrailCollider = waterTrail.GetComponent<Collider>();
        if (waterTrailCollider != null) waterTrailCollider.enabled = false;
        scenario.AddLandslidePreparationTask(seepageTask);

        // Fresh scar markings and exposed roots are environmental evidence, not instruction boards.
        CreateLandslideSlopePatch("Yamaç Üstündeki Taze Kayma İzi", new Vector2(0.5f, -4.8f), 2.0f, 1.0f, clay, 821, 0.12f);
        CreateLandslideSlopePatch("Kayma İzi Yanındaki Koyu Nem", new Vector2(-1.8f, -5.5f), 1.5f, 0.82f, shale, 822, 0.14f);
    }

    private void BuildLandslideRefuge(Material safeGreen, Material amber, Material timber,
        Material cabinet, Material screen, Material red)
    {
        GameObject clearing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        clearing.name = "Yamaçtan Uzak Güvenli Toplanma Çayırı";
        clearing.transform.position = landslideSafePosition + new Vector3(0f, 0.012f, 0f);
        clearing.transform.localScale = new Vector3(6.6f, 0.025f, 6.6f);
        clearing.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Güvenli Çayır Toprağı", new Color32(87, 120, 81, 255));
        Collider clearingCollider = clearing.GetComponent<Collider>();
        if (clearingCollider != null) clearingCollider.enabled = false;
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI * 2f / 12f;
            Vector3 position = landslideSafePosition + new Vector3(Mathf.Cos(angle) * 3.1f, 0.13f, Mathf.Sin(angle) * 3.1f);
            GameObject boundary = CreateBox("Güvenli Alan Alçak Yeşil Sınır Taşı " + i, position,
                new Vector3(0.24f, 0.22f, 0.24f), i % 3 == 0 ? amber : safeGreen);
            boundary.transform.rotation = Quaternion.Euler(0f, i * 27f, 0f);
        }

        // An actual emergency call station replaces the old instruction boards.
        Vector3 station = landslideSafePosition + new Vector3(0f, 0f, -1.5f);
        CreateBox("Acil Çağrı Cihazı Ayağı", station + new Vector3(0f, 0.65f, 0f), new Vector3(0.18f, 1.3f, 0.18f), timber);
        GameObject callDevice = CreateBox("Güvenli Alandaki 112 Acil Çağrı Cihazı", station + new Vector3(0f, 1.5f, 0f),
            new Vector3(0.86f, 1.18f, 0.38f), cabinet);
        PrototypeInteractable emergency = callDevice.AddComponent<PrototypeInteractable>();
        emergency.Configure(PrototypeAction.CallForLandslideHelp, "Acil cihazı kullan ve 112'yi ara");
        GameObject callScreen = CreateBox("Acil Çağrı Cihazı Ekranı", station + new Vector3(0f, 1.72f, 0.205f),
            new Vector3(0.61f, 0.29f, 0.035f), screen);
        GameObject callButton = CreateBox("112 Çağrı Düğmesi", station + new Vector3(0f, 1.25f, 0.225f),
            new Vector3(0.24f, 0.18f, 0.06f), red);
        GameObject handset = CreateBox("Acil Çağrı Cihazı Ahizesi", station + new Vector3(-0.24f, 1.48f, 0.25f),
            new Vector3(0.09f, 0.46f, 0.09f), amber);
        handset.transform.rotation = Quaternion.Euler(0f, 0f, -18f);
        GameObject beacon = CreateBox("Güvenli Alan Yeşil Feneri", station + new Vector3(0f, 2.3f, 0f),
            new Vector3(0.28f, 0.16f, 0.28f), safeGreen);
        foreach (Collider visualCollider in new[] { callScreen.GetComponent<Collider>(), callButton.GetComponent<Collider>(), handset.GetComponent<Collider>(), beacon.GetComponent<Collider>() })
            if (visualCollider != null) visualCollider.enabled = false;
        Renderer beaconRenderer = beacon.GetComponent<Renderer>();
        if (beaconRenderer != null && beaconRenderer.material.HasProperty("_EmissionColor"))
        {
            beaconRenderer.material.EnableKeyword("_EMISSION");
            beaconRenderer.material.SetColor("_EmissionColor", new Color(0.2f, 0.9f, 0.55f) * 1.2f);
        }
        scenario.SetLandslideAfterTask(emergency);
    }

    private void BuildLandslideDebris(Material soil, Material clay, Material shale,
        Material stone, Material stoneLight, Material wetMud, Material freshMud)
    {
        // Pebbles start first, followed by separate earth and boulder pulses.
        for (int i = 0; i < 22; i++)
        {
            float x = -9.6f + (i % 8) * 2.65f + (i / 8) * 0.48f;
            float z = -13.5f + (i / 8) * 2.0f + (i % 3) * 0.22f;
            Vector3 scale = new Vector3(0.28f + (i % 4) * 0.13f, 0.22f + (i % 3) * 0.12f, 0.3f + (i % 5) * 0.09f);
            float y = LandslideSlopeHeight(x, z) + scale.y * 0.44f;
            float releaseAt = 0.55f + (i % 7) * 0.48f;
            CreateLandslideMovingDebris("Önce Kopan Küçük Taş " + i, new Vector3(x, y, z), scale,
                i % 2 == 0 ? stoneLight : stone, 3f + i % 5 * 2f, 1400 + i, releaseAt,
                new Vector3((i % 3 - 1) * 0.55f, -0.3f, 1.7f));
        }
        for (int i = 0; i < 8; i++)
        {
            float x = -7.5f + i * 2.05f;
            float z = -9.1f + (i % 2) * 1.15f;
            Vector3 scale = new Vector3(1.1f + (i % 3) * 0.54f, 0.5f + (i % 2) * 0.32f, 1.25f + (i % 4) * 0.3f);
            float y = LandslideSlopeHeight(x, z) + scale.y * 0.46f;
            CreateLandslideMovingDebris("Koparak İnen Islak Toprak Kütlesi " + i,
                new Vector3(x, y, z), scale, i % 2 == 0 ? wetMud : freshMud,
                70f + i * 12f, 1650 + i, 2.1f + (i % 4) * 1.05f,
                new Vector3((i % 3 - 1) * 1.4f, -0.4f, 3.4f));
        }
        for (int i = 0; i < 9; i++)
        {
            float x = -8.3f + i * 2.05f;
            float z = -5.7f - (i % 3) * 0.7f;
            Vector3 scale = new Vector3(1.0f + (i % 3) * 0.65f, 0.8f + (i % 4) * 0.32f, 1.1f + (i % 2) * 0.7f);
            float y = LandslideSlopeHeight(x, z) + scale.y * 0.47f;
            CreateLandslideMovingDebris("Ana Heyelan Dalgasındaki Köşeli Kaya " + i,
                new Vector3(x, y, z), scale, i % 3 == 0 ? stoneLight : shale,
                120f + i * 38f, 1840 + i, 4.2f + (i % 5) * 0.7f,
                new Vector3((i % 4 - 1.5f) * 1.25f, 0.3f, 5.0f));
        }
        for (int i = 0; i < 5; i++)
        {
            float x = -7f + i * 3.0f;
            float z = -7.1f + (i % 2) * 0.8f;
            Vector3 scale = new Vector3(2.2f + (i % 2) * 1.2f, 0.55f + (i % 3) * 0.25f, 1.6f + (i % 2) * 0.7f);
            float y = LandslideSlopeHeight(x, z) + scale.y * 0.48f;
            CreateLandslideMovingDebris("Yol Üzerine İnen Çamurlu Toprak Dili " + i,
                new Vector3(x, y, z), scale, i % 2 == 0 ? wetMud : clay,
                160f + i * 25f, 2100 + i, 5.4f + i * 0.64f,
                new Vector3((i - 2) * 0.8f, -0.25f, 5.8f));
        }
    }

    private void CreateLandslideMovingDebris(string name, Vector3 position, Vector3 scale,
        Material material, float mass, int seed, float releaseAt, Vector3 impulse)
    {
        GameObject chunk = CreateLandslideRock(name, position, scale, material, true, mass, seed);
        Rigidbody body = chunk.GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        landslideDebris.Add(new LandslideDebrisBody
        {
            Body = body,
            ReleaseAt = releaseAt,
            Impulse = impulse,
            StartPosition = body.position,
            StartRotation = body.rotation
        });
    }

    private GameObject CreateLandslideRock(string name, Vector3 position, Vector3 scale,
        Material material, bool dynamic, float mass, int seed)
    {
        GameObject rock = new GameObject(name);
        rock.transform.position = position;
        rock.transform.rotation = Quaternion.Euler(seed * 13f % 360f, seed * 37f % 360f, seed * 23f % 360f);
        rock.transform.localScale = scale;
        Mesh mesh = CreateAngularRockMesh(name + " Mesh", seed, 0.22f);
        rock.AddComponent<MeshFilter>().sharedMesh = mesh;
        rock.AddComponent<MeshRenderer>().sharedMaterial = material;
        MeshCollider collider = rock.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        collider.convex = true;
        if (dynamic)
        {
            Rigidbody body = rock.AddComponent<Rigidbody>();
            body.mass = mass;
            body.useGravity = true;
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.maxAngularVelocity = 14f;
            rock.AddComponent<PrototypeLandslideDebrisImpact>();
        }
        return rock;
    }

    private Mesh CreateAngularRockMesh(string name, int seed, float roughness)
    {
        List<Vector3> points = new List<Vector3>
        {
            new Vector3(0f,1f,0f), new Vector3(-1f,0f,0f), new Vector3(0f,0f,1f),
            new Vector3(1f,0f,0f), new Vector3(0f,0f,-1f), new Vector3(0f,-1f,0f)
        };
        List<int> faces = new List<int>
        {
            0,2,1, 0,3,2, 0,4,3, 0,1,4,
            5,1,2, 5,2,3, 5,3,4, 5,4,1
        };
        for (int pass = 0; pass < 2; pass++)
        {
            List<int> refined = new List<int>(faces.Count * 4);
            Dictionary<long, int> midpointCache = new Dictionary<long, int>();
            for (int i = 0; i < faces.Count; i += 3)
            {
                int a = faces[i], b = faces[i + 1], c = faces[i + 2];
                int ab = Midpoint(a, b), bc = Midpoint(b, c), ca = Midpoint(c, a);
                refined.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }
            faces = refined;
            int Midpoint(int a, int b)
            {
                int low = Mathf.Min(a, b);
                int high = Mathf.Max(a, b);
                long key = ((long)low << 32) | (uint)high;
                if (midpointCache.TryGetValue(key, out int cached))
                    return cached;
                Vector3 point = (points[a] + points[b]).normalized;
                int index = points.Count;
                points.Add(point);
                midpointCache[key] = index;
                return index;
            }
        }

        UnityEngine.Random.State previous = UnityEngine.Random.state;
        UnityEngine.Random.InitState(seed);
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 direction = points[i].normalized;
            float radius = 0.78f + UnityEngine.Random.Range(0f, roughness * 2f);
            points[i] = direction * radius;
        }
        UnityEngine.Random.state = previous;
        for (int i = 0; i < faces.Count; i += 3)
        {
            Vector3 a = points[faces[i]];
            Vector3 b = points[faces[i + 1]];
            Vector3 c = points[faces[i + 2]];
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), (a + b + c) / 3f) < 0f)
            {
                int swap = faces[i + 1]; faces[i + 1] = faces[i + 2]; faces[i + 2] = swap;
            }
        }
        Mesh mesh = new Mesh { name = name };
        mesh.vertices = points.ToArray();
        mesh.triangles = faces.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void CreateLandslideDust()
    {
        GameObject dustObject = new GameObject("Heyelan Toz ve Toprak Bulutu");
        landslideDust = dustObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = landslideDust.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 3.6f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 3.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 5.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 1.5f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color32(126, 103, 81, 185));
        main.gravityModifier = -0.12f;
        main.maxParticles = 850;
        ParticleSystem.EmissionModule emission = landslideDust.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = landslideDust.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(16f, 1.4f, 2.5f);
        ParticleSystemRenderer renderer = landslideDust.GetComponent<ParticleSystemRenderer>();
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null)
            particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader != null)
        {
            Material particleMaterial = new Material(particleShader);
            particleMaterial.name = "Heyelan Tozu Şeffaf Parçacık Malzemesi";
            if (particleMaterial.HasProperty("_BaseColor"))
                particleMaterial.SetColor("_BaseColor", new Color32(156, 130, 104, 180));
            else if (particleMaterial.HasProperty("_Color"))
                particleMaterial.SetColor("_Color", new Color32(156, 130, 104, 180));
            renderer.material = particleMaterial;
        }
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    public void BeginLandslideVisual()
    {
        lastLandslideDustBurst = -1;
        for (int i = 0; i < landslideDebris.Count; i++)
        {
            LandslideDebrisBody item = landslideDebris[i];
            item.Released = false;
            item.Body.isKinematic = true;
            item.Body.useGravity = false;
            item.Body.position = item.StartPosition;
            item.Body.rotation = item.StartRotation;
            item.Body.linearVelocity = Vector3.zero;
            item.Body.angularVelocity = Vector3.zero;
        }
        for (int i = 0; i < landslideTrees.Count; i++)
        {
            if (landslideTrees[i] != null)
            {
                landslideTrees[i].isKinematic = true;
                landslideTrees[i].useGravity = false;
            }
        }
        for (int i = 0; i < landslideStructure.Count; i++)
        {
            if (landslideStructure[i] != null)
            {
                landslideStructure[i].isKinematic = true;
                landslideStructure[i].useGravity = false;
            }
        }
        if (landslideDust != null)
            landslideDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        PrototypeAudio.PlayLandslide();
    }

    public void UpdateLandslideVisual(float elapsed)
    {
        for (int i = 0; i < landslideDebris.Count; i++)
        {
            LandslideDebrisBody item = landslideDebris[i];
            if (item.Released || elapsed < item.ReleaseAt || item.Body == null)
                continue;
            item.Released = true;
            item.Body.useGravity = true;
            item.Body.isKinematic = false;
            // Values stored in Impulse describe launch velocity. Scale the impulse by mass
            // so large boulders and mud slabs move with the same intended downhill speed.
            item.Body.AddForce(item.Impulse * item.Body.mass, ForceMode.Impulse);
            item.Body.AddTorque(new Vector3(item.Impulse.z, item.Impulse.x * 0.7f, -item.Impulse.z * 0.5f)
                * item.Body.mass, ForceMode.Impulse);
        }
        for (int i = 0; i < landslideTrees.Count; i++)
        {
            Rigidbody tree = landslideTrees[i];
            if (tree == null || !tree.isKinematic || elapsed < landslideTreeReleaseTimes[i])
                continue;
            tree.useGravity = true;
            tree.isKinematic = false;
            tree.AddForce(new Vector3(-1.5f + (i % 3) * 1.1f, 0f, 2.4f) * tree.mass, ForceMode.Impulse);
            tree.AddTorque(new Vector3(0.9f, 0.2f, i % 2 == 0 ? 4.5f : -4.5f) * tree.mass, ForceMode.Impulse);
        }
        for (int i = 0; i < landslideStructure.Count; i++)
        {
            Rigidbody panel = landslideStructure[i];
            if (panel == null || !panel.isKinematic || elapsed < landslideStructureReleaseTimes[i])
                continue;
            panel.useGravity = true;
            panel.isKinematic = false;
            panel.AddForce(new Vector3(-1.8f, 0.9f, 2.8f) * panel.mass, ForceMode.Impulse);
            panel.AddTorque(new Vector3(2.6f, 0.7f, 1.4f) * panel.mass, ForceMode.Impulse);
        }

        int burst = elapsed < 0.7f ? -1 : elapsed < 2.4f ? 0 : elapsed < 4.6f ? 1 : elapsed < 7.0f ? 2 : 3;
        if (burst > lastLandslideDustBurst && landslideDust != null)
        {
            lastLandslideDustBurst = burst;
            float dustZ = -11f + burst * 4f;
            landslideDust.transform.position = new Vector3((burst - 1) * 1.2f,
                LandslideSlopeHeight(0f, dustZ) + 1.1f, dustZ);
            landslideDust.Emit(110 + burst * 35);
        }
    }

    private void BuildFloodHouse(Material wall, Material roof, Material wood, Material concrete,
        Material safeMat)
    {
        const float left = -11f;
        const float right = -3f;
        const float front = 2f;
        const float back = -6f;
        CreateBox("Sel Evi Zemin Kat Döşemesi", new Vector3(-7f, 0.035f, -2f), new Vector3(8f, 0.07f, 8f), wood);
        // Use single, aligned full-height walls so the ground floor and upper floor have no seam.
        const float houseWallHeight = 5.35f;
        CreateBox("Sel Evi Arka Duvarı", new Vector3(-7f, houseWallHeight * 0.5f, back),
            new Vector3(8f, houseWallHeight, 0.25f), wall);
        // Upper-floor window on the opposite side wall from the first placement.
        const float sideWindowWidth = 2.6f;
        const float sideWindowBottom = 3.55f;
        const float sideWindowTop = 4.9f;
        const float sideWindowCenterZ = 0.35f;
        CreateBox("Sel Evi Sol Duvarı", new Vector3(left, houseWallHeight * 0.5f, -2f),
            new Vector3(0.25f, houseWallHeight, 8f), wall);
        CreateBox("Sel Evi Sağ Duvarı Alt Bölüm", new Vector3(right, sideWindowBottom * 0.5f, -2f),
            new Vector3(0.25f, sideWindowBottom, 8f), wall);
        CreateBox("Sel Evi Sağ Duvarı Üst Bölüm", new Vector3(right, (sideWindowTop + houseWallHeight) * 0.5f, -2f),
            new Vector3(0.25f, houseWallHeight - sideWindowTop, 8f), wall);
        float sideWallBackWidth = sideWindowCenterZ - sideWindowWidth * 0.5f - back;
        float sideWallFrontWidth = front - (sideWindowCenterZ + sideWindowWidth * 0.5f);
        CreateBox("Sel Evi Sağ Duvarı Arka Pencere Yanı", new Vector3(right, (sideWindowBottom + sideWindowTop) * 0.5f, back + sideWallBackWidth * 0.5f),
            new Vector3(0.25f, sideWindowTop - sideWindowBottom, sideWallBackWidth), wall);
        CreateBox("Sel Evi Sağ Duvarı Ön Pencere Yanı", new Vector3(right, (sideWindowBottom + sideWindowTop) * 0.5f, sideWindowCenterZ + sideWindowWidth * 0.5f + sideWallFrontWidth * 0.5f),
            new Vector3(0.25f, sideWindowTop - sideWindowBottom, sideWallFrontWidth), wall);
        Material sideWindowGlass = MakeMaterial("Sel Evi Yan Pencere Camı", new Color32(105, 166, 190, 72));
        sideWindowGlass.SetOverrideTag("RenderType", "Transparent");
        if (sideWindowGlass.HasProperty("_Surface"))
            sideWindowGlass.SetFloat("_Surface", 1f);
        sideWindowGlass.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        sideWindowGlass.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        sideWindowGlass.SetInt("_ZWrite", 0);
        sideWindowGlass.DisableKeyword("_ALPHATEST_ON");
        sideWindowGlass.EnableKeyword("_ALPHABLEND_ON");
        sideWindowGlass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        sideWindowGlass.renderQueue = 3000;
        CreateBox("Sel Evi Yan Pencere Camı", new Vector3(right - 0.12f, (sideWindowBottom + sideWindowTop) * 0.5f, sideWindowCenterZ),
            new Vector3(0.045f, sideWindowTop - sideWindowBottom, sideWindowWidth), sideWindowGlass);
        CreateBox("Sel Evi Yan Pencere Üst Çerçevesi", new Vector3(right - 0.08f, sideWindowTop, sideWindowCenterZ),
            new Vector3(0.16f, 0.1f, sideWindowWidth + 0.18f), wood);
        CreateBox("Sel Evi Yan Pencere Alt Çerçevesi", new Vector3(right - 0.08f, sideWindowBottom, sideWindowCenterZ),
            new Vector3(0.16f, 0.1f, sideWindowWidth + 0.18f), wood);
        for (int pane = -1; pane <= 1; pane += 2)
            CreateBox("Sel Evi Yan Pencere Dikey Bölmesi " + pane,
                new Vector3(right - 0.055f, (sideWindowBottom + sideWindowTop) * 0.5f, sideWindowCenterZ + pane * sideWindowWidth * 0.25f),
                new Vector3(0.08f, sideWindowTop - sideWindowBottom, 0.055f), wood);
        CreateBox("Sel Evi Ön Duvarı Sol", new Vector3(-9.55f, houseWallHeight * 0.5f, front),
            new Vector3(2.9f, houseWallHeight, 0.25f), wall);
        CreateBox("Sel Evi Ön Duvarı Sağ", new Vector3(-4.45f, houseWallHeight * 0.5f, front),
            new Vector3(2.9f, houseWallHeight, 0.25f), wall);
        CreateBox("Sel Evi Kapı Üstü", new Vector3(-7f, (houseWallHeight + 2.1f) * 0.5f, front),
            new Vector3(2.2f, houseWallHeight - 2.1f, 0.25f), wall);
        // Split the upper floor around the stairwell so the player can actually climb through it.
        CreateBox("Sel Evi Üst Kat Döşemesi Sol", new Vector3(-8.2f, 2.78f, -2f), new Vector3(5.6f, 0.22f, 8f), wood);
        CreateBox("Sel Evi Üst Kat Döşemesi Ön", new Vector3(-7f, 2.78f, 0.4f), new Vector3(8f, 0.22f, 3.2f), wood);
        CreateBox("Sel Evi Üst Kat Döşemesi Arka", new Vector3(-7f, 2.78f, -5.85f), new Vector3(8f, 0.22f, 0.3f), wood);
        CreateBox("Sel Evi Kırma Çatı", new Vector3(-7f, 5.15f, -2f), new Vector3(8.8f, 0.35f, 8.8f), roof);
        CreateBox("Üst Kat Güvenli Alan İşareti", new Vector3(-7f, 2.91f, -2f), new Vector3(2.4f, 0.035f, 2.4f), safeMat);
        BuildFloodEntryDoor(wood, concrete);
        BuildFloodStairs(wood, safeMat);
        CreateBox("Ev Ön Sundurma", new Vector3(-7f, 0.13f, 2.65f), new Vector3(4.5f, 0.22f, 1.45f), concrete);
        CreateBox("Ev Sağanak Oluk", new Vector3(-7f, 5.0f, 2.55f), new Vector3(8.2f, 0.12f, 0.16f), wood);
    }

    private void BuildFloodEntryDoor(Material wood, Material trim)
    {
        // The front wall has a 2.2 m opening. Fit the door frame and leaf into that opening.
        CreateBox("Giriş Kapısı Sol Sövesi", new Vector3(-8.03f, 1.05f, 1.91f),
            new Vector3(0.14f, 2.1f, 0.28f), trim);
        CreateBox("Giriş Kapısı Sağ Sövesi", new Vector3(-5.97f, 1.05f, 1.91f),
            new Vector3(0.14f, 2.1f, 0.28f), trim);
        CreateBox("Giriş Kapısı Üst Sövesi", new Vector3(-7f, 2.10f, 1.91f),
            new Vector3(2.2f, 0.14f, 0.28f), trim);

        GameObject hinge = new GameObject("Giriş Kapısı Menteşesi");
        hinge.transform.position = new Vector3(-7.97f, 0f, 1.94f);
        // Keep the door shut and square in the frame; the previous yaw left a visible wedge-shaped gap.
        hinge.transform.rotation = Quaternion.identity;
        GameObject leaf = CreateChildBox(hinge.transform, "Ahşap Giriş Kapısı Kanadı",
            new Vector3(0.97f, 1.04f, 0f), new Vector3(1.94f, 2.08f, 0.12f),
            MakeMaterial("Giriş Kapısı Ceviz Ahşabı", new Color32(119, 78, 52, 255)));
        Collider leafCollider = leaf.GetComponent<Collider>();
        if (leafCollider != null) leafCollider.enabled = false;
        Material panelMat = MakeMaterial("Kapı İç Ahşap Panel", new Color32(91, 57, 39, 255));
        for (int panel = -1; panel <= 1; panel += 2)
        {
            GameObject inset = CreateChildBox(leaf.transform, "Kapı " + (panel > 0 ? "Üst" : "Alt") + " Göbek Paneli",
                new Vector3(0f, panel * 0.27f, -0.57f), new Vector3(0.66f, 0.245f, 0.20f), panelMat);
            Collider insetCollider = inset.GetComponent<Collider>();
            if (insetCollider != null) insetCollider.enabled = false;
        }

        Material hingeMetal = MakeMaterial("Kapı Menteşe Metali", new Color32(77, 83, 79, 255));
        for (int hingeIndex = 0; hingeIndex < 3; hingeIndex++)
        {
            GameObject doorHinge = CreateChildBox(leaf.transform, "Kapı Menteşesi " + (hingeIndex + 1),
                new Vector3(-0.455f, -0.36f + hingeIndex * 0.36f, -0.64f),
                new Vector3(0.045f, 0.075f, 0.20f), hingeMetal);
            Collider doorHingeCollider = doorHinge.GetComponent<Collider>();
            if (doorHingeCollider != null) doorHingeCollider.enabled = false;
        }

        Material brass = MakeMaterial("Kapı Kolu Pirinç", new Color32(188, 151, 81, 255));
        GameObject handlePlate = CreateChildBox(leaf.transform, "Giriş Kapısı Kol Rozeti",
            new Vector3(0.405f, 0f, -0.66f), new Vector3(0.09f, 0.105f, 0.18f), brass);
        Collider handlePlateCollider = handlePlate.GetComponent<Collider>();
        if (handlePlateCollider != null) handlePlateCollider.enabled = false;
        GameObject knob = CreateChildBox(leaf.transform, "Giriş Kapısı Pirinç Kolu",
            new Vector3(0.405f, 0f, -0.83f), new Vector3(0.09f, 0.045f, 0.35f), brass);
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
            // Each riser should only fill the 30 cm rise to this step, not a floor-to-step wall.
            CreateBox("Merdiven Basamak Önü " + (i + 1), new Vector3(-4.25f, top - 0.15f, z + 0.27f),
                new Vector3(1.7f, 0.30f, 0.08f), wood);
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

    }

    private void BuildFloodNeighborhood(Material concrete, Material roof, Material wall, Material wood)
    {
        Material window = MakeMaterial("Sel Mahallesi Camı", new Color32(103, 155, 173, 255));
        Material door = MakeMaterial("Sel Mahallesi Kapısı", new Color32(92, 62, 43, 255));
        for (int i = 0; i < 5; i++)
        {
            // Keep the first roadside house clear of the flood-house rear corner while
            // keeping the row evenly spaced and fully supported by the enlarged ground.
            float x = (i == 0 ? 0.5f : -1f + i * 6.3f);
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
        Material carPaint = MakeMaterial("Araç Gövde Boyası", new Color32(55, 91, 111, 255));
        Material carTrim = MakeMaterial("Araç Tampon ve Izgara", new Color32(37, 46, 50, 255));
        Material carGlass = MakeMaterial("Araç Koyu Cam", new Color32(49, 83, 94, 255));
        Material carChrome = MakeMaterial("Araç Metal Ayrıntı", new Color32(174, 184, 178, 255));
        Material headlight = MakeMaterial("Araç Far Camı", new Color32(235, 215, 151, 255));
        Material tailLight = MakeMaterial("Araç Stop Lambası", new Color32(174, 54, 45, 255));
        GameObject CreateCarDetail(string name, Vector3 position, Vector3 size, Material material)
        {
            GameObject detail = CreateBox(name, position, size, material);
            Collider detailCollider = detail.GetComponent<Collider>();
            if (detailCollider != null)
                detailCollider.enabled = false;
            return detail;
        }

        // Park between two streetlights and keep the full vehicle on the road.
        // All model pieces share this offset so the car stays assembled.
        const float carOffsetX = 0.79f;
        const float carOffsetZ = -0.66f;
        Vector3 CarPosition(Vector3 position) => position + new Vector3(carOffsetX, 0f, carOffsetZ);
        // The car is aligned along X: front at +X, rear at -X.
        CreateCarDetail("Park Edilmiş Araç Alt Gövdesi", CarPosition(new Vector3(2f, 0.62f, 5f)), new Vector3(3.7f, 0.82f, 1.72f), carPaint);
        CreateCarDetail("Araç Kaputu", CarPosition(new Vector3(2.95f, 1.02f, 5f)), new Vector3(1.45f, 0.24f, 1.62f), carPaint);
        CreateCarDetail("Araç Bagajı", CarPosition(new Vector3(0.95f, 0.98f, 5f)), new Vector3(0.92f, 0.20f, 1.62f), carPaint);
        CreateCarDetail("Araç Ön Tamponu", CarPosition(new Vector3(3.88f, 0.48f, 5f)), new Vector3(0.16f, 0.22f, 1.68f), carTrim);
        CreateCarDetail("Araç Arka Tamponu", CarPosition(new Vector3(0.12f, 0.48f, 5f)), new Vector3(0.16f, 0.22f, 1.68f), carTrim);
        CreateCarDetail("Araç Ön Izgarası", CarPosition(new Vector3(3.89f, 0.70f, 5f)), new Vector3(0.045f, 0.28f, 0.62f), carTrim);
        for (int side = -1; side <= 1; side += 2)
        {
            CreateCarDetail("Araç Ön Far " + side, CarPosition(new Vector3(3.895f, 0.84f, 5f + side * 0.53f)),
                new Vector3(0.055f, 0.17f, 0.25f), headlight);
            CreateCarDetail("Araç Arka Stop Lambası " + side, CarPosition(new Vector3(0.105f, 0.82f, 5f + side * 0.53f)),
                new Vector3(0.055f, 0.20f, 0.24f), tailLight);
            CreateCarDetail("Araç Yan Basamağı " + side, CarPosition(new Vector3(2f, 0.34f, 5f + side * 0.86f)),
                new Vector3(2.15f, 0.12f, 0.10f), carTrim);
            CreateCarDetail("Araç Yan Ayna " + side, CarPosition(new Vector3(2.83f, 1.27f, 5f + side * 0.82f)),
                new Vector3(0.24f, 0.12f, 0.18f), carPaint);
            for (int doorLine = 0; doorLine < 3; doorLine++)
            {
                float x = doorLine == 0 ? 1.05f : doorLine == 1 ? 2.08f : 3.02f;
                CreateCarDetail("Araç Kapı Panel Çizgisi " + side + " " + doorLine,
                    CarPosition(new Vector3(x, 0.66f, 5f + side * 0.866f)), new Vector3(0.025f, 0.48f, 0.018f), carTrim);
            }
            CreateCarDetail("Araç Kapı Kolu " + side, CarPosition(new Vector3(2.48f, 0.87f, 5f + side * 0.88f)),
                new Vector3(0.20f, 0.045f, 0.025f), carChrome);
        }

        // Open, distinct glazing and pillars make the passenger cabin readable from a distance.
        CreateCarDetail("Araç Kabin Alt Kuşağı", CarPosition(new Vector3(2.03f, 1.08f, 5f)), new Vector3(1.9f, 0.18f, 1.28f), carPaint);
        CreateCarDetail("Araç Tavanı", CarPosition(new Vector3(2.03f, 1.62f, 5f)), new Vector3(1.48f, 0.13f, 1.18f), carPaint);
        for (int side = -1; side <= 1; side += 2)
        {
            float z = 5f + side * 0.655f;
            CreateCarDetail("Araç Ön Yan Cam " + side, CarPosition(new Vector3(2.52f, 1.36f, z)), new Vector3(0.74f, 0.38f, 0.045f), carGlass);
            CreateCarDetail("Araç Arka Yan Cam " + side, CarPosition(new Vector3(1.56f, 1.36f, z)), new Vector3(0.70f, 0.38f, 0.045f), carGlass);
            CreateCarDetail("Araç Ön Cam Direği " + side, CarPosition(new Vector3(2.94f, 1.36f, 5f + side * 0.62f)), new Vector3(0.09f, 0.53f, 0.09f), carPaint);
            CreateCarDetail("Araç Arka Cam Direği " + side, CarPosition(new Vector3(1.13f, 1.36f, 5f + side * 0.62f)), new Vector3(0.09f, 0.53f, 0.09f), carPaint);
            CreateCarDetail("Araç Orta Direği " + side, CarPosition(new Vector3(2.03f, 1.36f, 5f + side * 0.66f)), new Vector3(0.09f, 0.53f, 0.08f), carPaint);
        }
        CreateCarDetail("Araç Ön Cam", CarPosition(new Vector3(2.99f, 1.36f, 5f)), new Vector3(0.045f, 0.37f, 1.02f), carGlass);
        CreateCarDetail("Araç Arka Cam", CarPosition(new Vector3(1.07f, 1.36f, 5f)), new Vector3(0.045f, 0.37f, 1.02f), carGlass);
        GameObject carCollisionBody = new GameObject("Park Edilmiş Araç Çarpışma Gövdesi");
        carCollisionBody.transform.position = CarPosition(new Vector3(2f, 0.82f, 5f));
        carCollisionBody.AddComponent<BoxCollider>().size = new Vector3(3.7f, 1.3f, 1.72f);

        Material tireMaterial = MakeMaterial("Araç Lastiği", new Color32(31, 35, 37, 255));
        Material hubMaterial = MakeMaterial("Araç Jantı", new Color32(157, 168, 168, 255));
        Material hubCenterMaterial = MakeMaterial("Araç Jant Merkezi", new Color32(78, 91, 94, 255));
        for (int xSide = -1; xSide <= 1; xSide += 2)
        for (int zSide = -1; zSide <= 1; zSide += 2)
        {
            Vector3 wheelPosition = CarPosition(new Vector3(2f + xSide * 1.12f, 0.35f, 5f + zSide * 0.88f));
            GameObject tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tire.name = "Taşkın Mahallesi Araç Lastiği " + xSide + " " + zSide;
            tire.transform.SetPositionAndRotation(wheelPosition, Quaternion.Euler(90f, 0f, 0f));
            tire.transform.localScale = new Vector3(0.62f, 0.20f, 0.62f);
            tire.GetComponent<Renderer>().sharedMaterial = tireMaterial;
            GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hub.name = "Taşkın Mahallesi Araç Jantı " + xSide + " " + zSide;
            hub.transform.SetPositionAndRotation(wheelPosition + new Vector3(0f, 0f, zSide * 0.115f),
                Quaternion.Euler(90f, 0f, 0f));
            hub.transform.localScale = new Vector3(0.36f, 0.055f, 0.36f);
            hub.GetComponent<Renderer>().sharedMaterial = hubMaterial;
            Collider hubCollider = hub.GetComponent<Collider>();
            if (hubCollider != null)
                hubCollider.enabled = false;
            GameObject hubCenter = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hubCenter.name = "Araç Jant Göbeği";
            hubCenter.transform.SetPositionAndRotation(wheelPosition + new Vector3(0f, 0f, zSide * 0.15f),
                Quaternion.Euler(90f, 0f, 0f));
            hubCenter.transform.localScale = new Vector3(0.12f, 0.06f, 0.12f);
            hubCenter.GetComponent<Renderer>().sharedMaterial = hubCenterMaterial;
            Collider centerCollider = hubCenter.GetComponent<Collider>();
            if (centerCollider != null)
                centerCollider.enabled = false;
        }
        CreateBox("Taşkın Tahliye Yolu", new Vector3(-0.1f, 0.18f, 1.0f), new Vector3(0.8f, 0.24f, 10f),
            MakeMaterial("Yükseltilmiş Yaya Yolu", new Color32(147, 139, 114, 255)));
    }

    private void BuildFloodTasks(Material wood, Material safeMat)
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
            new Vector3(-0.12f, 0.61f, 0.04f), new Vector3(0.46f, 0.08f, 0.62f), paper);
        Collider papersCollider = papers.GetComponent<Collider>();
        if (papersCollider != null) papersCollider.enabled = false;
        GameObject medicine = CreateChildBox(documents.transform, "İlk Yardım İlaç Paketi",
            new Vector3(0.20f, 0.62f, -0.20f), new Vector3(0.29f, 0.22f, 0.31f), medicalPack);
        Collider medicineCollider = medicine.GetComponent<Collider>();
        if (medicineCollider != null) medicineCollider.enabled = false;
        GameObject crossVertical = CreateChildBox(medicine.transform, "İlaç Paketi Artı İşareti Dikey",
            new Vector3(0f, 0.58f, 0f), new Vector3(0.108f, 0.095f, 0.505f), medicalMark);
        Collider crossVerticalCollider = crossVertical.GetComponent<Collider>();
        if (crossVerticalCollider != null) crossVerticalCollider.enabled = false;
        GameObject crossHorizontal = CreateChildBox(medicine.transform, "İlaç Paketi Artı İşareti Yatay",
            new Vector3(0f, 0.58f, 0f), new Vector3(0.345f, 0.095f, 0.126f), medicalMark);
        Collider crossHorizontalCollider = crossHorizontal.GetComponent<Collider>();
        if (crossHorizontalCollider != null) crossHorizontalCollider.enabled = false;
        PrototypeInteractable documentsTask = documents.AddComponent<PrototypeInteractable>();
        documentsTask.Configure(PrototypeAction.RaiseFloodValuables, "Önemli evrak ve ilaçları üst kata kaldır");
        documentsTask.ConfigureMove(documents.transform, new Vector3(-9.45f, 3.36f, -5.18f));
        scenario.AddFloodPreparationTask(documentsTask);

        Material modemMat = MakeMaterial("Modem Gövdesi", new Color32(52, 66, 74, 255));
        Material modemLight = MakeMaterial("Modem Gösterge Işıkları", new Color32(96, 198, 161, 255));
        GameObject modem = new GameObject("Alt Kattaki Modem");
        modem.transform.position = new Vector3(-6.35f, 0.14f, -0.1f);
        BoxCollider modemCollider = modem.AddComponent<BoxCollider>();
        modemCollider.center = new Vector3(0f, 0.14f, 0f);
        modemCollider.size = new Vector3(0.62f, 0.48f, 0.46f);
        GameObject modemBody = CreateChildBox(modem.transform, "Modem Gövdesi",
            Vector3.zero, new Vector3(0.48f, 0.12f, 0.34f), modemMat);
        Collider modemBodyCollider = modemBody.GetComponent<Collider>();
        if (modemBodyCollider != null) modemBodyCollider.enabled = false;
        for (int antennaIndex = -1; antennaIndex <= 1; antennaIndex += 2)
        {
            GameObject antenna = CreateChildBox(modem.transform, "Modem Anteni",
                new Vector3(antennaIndex * 0.16f, 0.18f, -0.11f), new Vector3(0.025f, 0.24f, 0.025f), modemMat);
            Collider antennaCollider = antenna.GetComponent<Collider>();
            if (antennaCollider != null) antennaCollider.enabled = false;
        }
        for (int lightIndex = 0; lightIndex < 3; lightIndex++)
        {
            GameObject indicator = CreateChildBox(modem.transform, "Modem Durum Işığı " + (lightIndex + 1),
                new Vector3(-0.12f + lightIndex * 0.08f, 0.025f, 0.174f), new Vector3(0.035f, 0.018f, 0.012f), modemLight);
            Collider indicatorCollider = indicator.GetComponent<Collider>();
            if (indicatorCollider != null) indicatorCollider.enabled = false;
        }
        CreateBox("Üst Kat Modem Rafı", new Vector3(-4.35f, 3.04f, -0.35f),
            new Vector3(1.05f, 0.12f, 0.55f), shelfMat);
        CreateBox("Modem Rafı Sol Ayağı", new Vector3(-4.78f, 2.90f, -0.35f),
            new Vector3(0.08f, 0.22f, 0.46f), shelfMat);
        CreateBox("Modem Rafı Sağ Ayağı", new Vector3(-3.92f, 2.90f, -0.35f),
            new Vector3(0.08f, 0.22f, 0.46f), shelfMat);
        PrototypeInteractable modemTask = modem.AddComponent<PrototypeInteractable>();
        modemTask.Configure(PrototypeAction.RaiseFloodElectronics, "Modemi alt kattan kuru üst kata taşı");
        modemTask.ConfigureMove(modem.transform, new Vector3(-4.35f, 3.22f, -0.35f));
        scenario.AddFloodPreparationTask(modemTask);

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

        // Fixed to the wall opposite the stair landing, above a narrow console.
        Material consoleMat = MakeMaterial("Resmî Bilgi Konsolu", new Color32(57, 67, 69, 255));
        CreateBox("Resmî Bilgi Konsolu Rafı", new Vector3(-5.20f, 3.02f, 1.40f),
            new Vector3(1.35f, 0.12f, 0.55f), tableMat);
        GameObject allClear = CreateBox("Duvara Sabit Resmî Bilgi Ekranı", new Vector3(-5.20f, 3.55f, 1.76f),
            new Vector3(1.12f, 0.74f, 0.14f), consoleMat);
        GameObject screen = CreateBox("Resmî Bilgi Ekranı Görüntüsü", new Vector3(-5.20f, 3.57f, 1.665f),
            new Vector3(0.94f, 0.54f, 0.035f), MakeMaterial("Resmî Bilgi Ekranı Yeşil", new Color32(45, 111, 96, 255)));
        Collider screenCollider = screen.GetComponent<Collider>();
        if (screenCollider != null) screenCollider.enabled = false;
        for (int line = 0; line < 3; line++)
        {
            GameObject noticeLine = CreateBox("Duyuru Ekranı Bilgi Satırı " + (line + 1),
                new Vector3(-5.20f, 3.72f - line * 0.15f, 1.642f),
                new Vector3(0.64f - line * 0.09f, 0.025f, 0.012f),
                MakeMaterial("Ekran Açık Yazı", new Color32(202, 227, 205, 255)));
            Collider lineCollider = noticeLine.GetComponent<Collider>();
            if (lineCollider != null) lineCollider.enabled = false;
        }
        PrototypeInteractable allClearTask = allClear.AddComponent<PrototypeInteractable>();
        allClearTask.Configure(PrototypeAction.WaitForFloodAllClear, "Resmî duyuruları izle; su çekilmeden geri dönme");
        scenario.AddFloodAfterTask(allClearTask);
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

    public void ShowResults(int score, string review, string title = "Tatbikat tamamlandı")
    {
        if (ui != null)
            ui.ShowResults(score, review, title);
    }
}




