using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

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
    private readonly List<GameObject> wildfireFlameLayers = new List<GameObject>();
    private readonly List<Light> wildfireFlameLights = new List<Light>();
    private readonly List<Vector3> wildfireTreeCanopies = new List<Vector3>();
    private Material wildfireSmokeMaterial;
    private Vector3 wildfireSafePosition;
    private readonly List<Transform> stormTrees = new List<Transform>();
    private readonly List<Quaternion> stormTreeBaseRotations = new List<Quaternion>();
    private readonly List<Transform> stormClouds = new List<Transform>();
    private readonly List<Vector3> stormCloudBasePositions = new List<Vector3>();
    private readonly List<Transform> stormWaveCrests = new List<Transform>();
    private readonly List<Vector3> stormWaveBasePositions = new List<Vector3>();
    private readonly List<GameObject> stormPostEventObjects = new List<GameObject>();
    private readonly List<ParticleSystem> stormRainSystems = new List<ParticleSystem>();
    private Mesh stormIndoorWaterMesh;
    private Mesh stormIndoorFoamMesh;
    private GameObject stormIndoorWaterObject;
    private GameObject stormIndoorFoamObject;
    private Vector3[] stormIndoorWaterVertices;
    private float[] stormIndoorWaterArrival;
    private bool[] stormIndoorWaterFilled;
    private readonly List<int> stormIndoorWaterTriangles = new List<int>();
    private readonly List<Vector3> stormIndoorFoamVertices = new List<Vector3>();
    private readonly List<int> stormIndoorFoamTriangles = new List<int>();
    private int stormIndoorWaterCellsX;
    private int stormIndoorWaterCellsZ;
    private float stormIndoorWaterProgress;
    private PrototypeInteractable stormDoorInteraction;
    private ParticleSystem stormWindDebris;
    private Light stormLightning;
    private Transform stormPowerPolePivot;
    private Vector3 stormShelterPosition = new Vector3(0f, 0f, 3.5f);
    private ParticleSystem tornadoFunnel;
    private ParticleSystem tornadoDebris;
    private Light tornadoCoreLight;
    private GameObject tornadoVfxRoot;
    private Texture2D tornadoParticleTexture;
    private Light tornadoSun;
    private float tornadoOriginalSunIntensity;
    private float tornadoOriginalAmbientIntensity;
    private UnityEngine.Rendering.AmbientMode tornadoOriginalAmbientMode;
    private Color tornadoOriginalAmbientSkyColor;
    private Color tornadoOriginalAmbientEquatorColor;
    private Color tornadoOriginalAmbientGroundColor;
    private Material tornadoOriginalSkybox;
    private Material tornadoRuntimeSkybox;
    private float tornadoOriginalSkyboxExposure;
    private bool tornadoSkyboxHasExposure;
    private bool tornadoOriginalFogEnabled;
    private FogMode tornadoOriginalFogMode;
    private float tornadoOriginalFogDensity;
    private Color tornadoOriginalFogColor;
    private readonly List<Transform> tornadoLooseObjects = new List<Transform>();
    private readonly List<Vector3> tornadoLooseObjectHomes = new List<Vector3>();
    private readonly List<Quaternion> tornadoLooseObjectHomeRotations = new List<Quaternion>();
    private readonly List<Vector3> tornadoLooseObjectVelocities = new List<Vector3>();
    private readonly List<PrototypeInteractable> tornadoLooseObjectPreparationTasks = new List<PrototypeInteractable>();
    private readonly List<GameObject> tornadoDamageProps = new List<GameObject>();
    private Animator tornadoResidentAnimator;
    private Transform tornadoResidentModelRoot;
    private Coroutine tornadoResidentPoseRoutine;

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
#if UNITY_EDITOR
        GameObject editingPreview = GameObject.Find("Oyun Haritası - Düzenleme Önizlemesi");
        if (editingPreview != null)
            Destroy(editingPreview);
#endif
        Time.timeScale = 1f;
        Application.targetFrameRate = 120;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        ConfigureLighting();

        worldDisasterType = GameManager.Instance != null
            ? GameManager.Instance.SelectedDisaster : GameManager.DisasterType.Earthquake;
        GameObject earthquakeTerrain = FindSceneObjectIncludingInactive("Deprem Haritası");
        if (earthquakeTerrain != null)
            earthquakeTerrain.SetActive(worldDisasterType == GameManager.DisasterType.Earthquake);

        if (ProvinceManager.Instance != null)
        {
            int savedPlate = ProvinceManager.Instance.GetLastProvincePlate();
            if (ProvinceManager.Instance.GetPrimaryDisasterForProvince(savedPlate) != worldDisasterType)
                savedPlate = worldDisasterType switch
                {
                    GameManager.DisasterType.Flood => ProvinceManager.Instance.GetFirstFloodProvincePlate(),
                    GameManager.DisasterType.Landslide => ProvinceManager.Instance.GetFirstLandslideTrainingProvincePlate(),
                    GameManager.DisasterType.Wildfire => 48,
                    GameManager.DisasterType.Storm => ProvinceManager.Instance.GetFirstStormTrainingProvincePlate(),
                    GameManager.DisasterType.Tornado => ProvinceManager.Instance.GetFirstTornadoTrainingProvincePlate(),
                    _ => 6
                };
            ProvinceManager.Instance.SelectProvince(savedPlate);
        }

        string scenarioName = worldDisasterType switch
        {
            GameManager.DisasterType.Flood => "Sel ve Taşkın Senaryosu",
            GameManager.DisasterType.Landslide => "Heyelan Farkındalık Senaryosu",
            GameManager.DisasterType.Wildfire => "Orman Yangını Tahliye Tatbikatı",
            GameManager.DisasterType.Storm => "Fırtına Güvenli Barınma Tatbikatı",
            GameManager.DisasterType.Tornado => "Hortum Güvenli Alan ve Tahliye Tatbikatı",
            _ => "Deprem Senaryosu"
        };
        scenario = new GameObject(scenarioName).AddComponent<PrototypeScenario>();
        if (worldDisasterType == GameManager.DisasterType.Flood)
            BuildFloodWorld();
        else if (worldDisasterType == GameManager.DisasterType.Landslide)
            BuildLandslideWorld();
        else if (worldDisasterType == GameManager.DisasterType.Wildfire)
            BuildWildfireWorld();
        else if (worldDisasterType == GameManager.DisasterType.Storm)
            BuildStormWorld();
        else if (worldDisasterType == GameManager.DisasterType.Tornado)
            BuildTornadoWorld();
        else
            BuildWorld();

        if (worldDisasterType == GameManager.DisasterType.Earthquake)
        {
            RenderSettings.ambientIntensity = 0.6f;
            Light earthquakeSun = GameObject.Find("Yumuşak Gün Işığı")?.GetComponent<Light>();
            if (earthquakeSun != null)
                earthquakeSun.intensity = 0f;
        }

        GameObject playerObject = new GameObject("Oyuncu", typeof(CharacterController));
        player = playerObject.AddComponent<PrototypePlayerController>();
        Vector3 playerSpawn = worldDisasterType switch
        {
            GameManager.DisasterType.Flood => new Vector3(-8.6f, 0f, -2.6f),
            GameManager.DisasterType.Landslide => new Vector3(0f, 0f, 10f),
            GameManager.DisasterType.Wildfire => new Vector3(0f, 0f, 13f),
            GameManager.DisasterType.Storm => new Vector3(0f, 0f, -7.5f),
            GameManager.DisasterType.Tornado => new Vector3(-8f, 0f, 1.6f),
            _ => new Vector3(-0.4f, 0f, -2.3f)
        };
        float playerYaw = worldDisasterType switch
        {
            GameManager.DisasterType.Flood => 0f,
            GameManager.DisasterType.Landslide => 180f,
            GameManager.DisasterType.Wildfire => 180f,
            GameManager.DisasterType.Storm => 0f,
            GameManager.DisasterType.Tornado => 180f,
            _ => 255f
        };
        player.Initialize(scenario, playerSpawn, playerYaw);
        scenario.Initialize(this, player, safeTablePosition);
        if (worldDisasterType == GameManager.DisasterType.Flood)
            scenario.ConfigureFlood(floodWater, floodSafePosition);
        else if (worldDisasterType == GameManager.DisasterType.Landslide)
            scenario.ConfigureLandslide(landslideSafePosition);
        else if (worldDisasterType == GameManager.DisasterType.Wildfire)
            scenario.ConfigureWildfire(wildfireSafePosition);
        else if (worldDisasterType == GameManager.DisasterType.Storm)
        {
            scenario.ConfigureStorm(stormShelterPosition);
            scenario.ConfigureStormDoor(stormDoorInteraction);
        }
        else if (worldDisasterType == GameManager.DisasterType.Tornado)
        {
            scenario.ConfigureTornado(new Vector3(-8f, 0f, -7.25f));
            scenario.ConfigureTornadoHazard(new Vector3(8f, 0f, 1f));
        }
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

    private static GameObject FindSceneObjectIncludingInactive(string objectName)
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == objectName)
                return roots[i];

            Transform[] children = roots[i].GetComponentsInChildren<Transform>(true);
            for (int childIndex = 0; childIndex < children.Length; childIndex++)
                if (children[childIndex].name == objectName)
                    return children[childIndex].gameObject;
        }

        return null;
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

#if UNITY_EDITOR
    public bool GenerateMapEditingPreviewForEditor(GameManager.DisasterType disasterType)
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Harita düzenleme", "Önce Play Mode'u durdur.", "Tamam");
            return false;
        }

        Scene scene = SceneManager.GetActiveScene();
        GameObject oldPreview = GameObject.Find("Oyun Haritası - Düzenleme Önizlemesi");
        if (oldPreview != null)
            Undo.DestroyObjectImmediate(oldPreview);

        GameObject placementRoot = GameObject.Find("Sahneye Yerleştirilen Harita Varlıkları");
        if (placementRoot == null)
        {
            placementRoot = new GameObject("Sahneye Yerleştirilen Harita Varlıkları");
            Undo.RegisterCreatedObjectUndo(placementRoot, "Harita nesne kökü oluştur");
        }

        GameObject previewRoot = new GameObject("Oyun Haritası - Düzenleme Önizlemesi");
        previewRoot.tag = "EditorOnly";
        GameObject previewScenarioObject = new GameObject("Harita Önizleme Senaryosu");
        HashSet<GameObject> existingRoots = new HashSet<GameObject>();
        GameObject[] rootsBeforeBuild = scene.GetRootGameObjects();
        for (int i = 0; i < rootsBeforeBuild.Length; i++)
            existingRoots.Add(rootsBeforeBuild[i]);

        materials.Clear();
        scenario = previewScenarioObject.AddComponent<PrototypeScenario>();
        worldDisasterType = disasterType;

        try
        {
            switch (disasterType)
            {
                case GameManager.DisasterType.Flood:
                    BuildFloodWorld();
                    break;
                case GameManager.DisasterType.Landslide:
                    BuildLandslideWorld();
                    break;
                case GameManager.DisasterType.Wildfire:
                    BuildWildfireWorld();
                    break;
                case GameManager.DisasterType.Storm:
                    BuildStormWorld();
                    break;
                case GameManager.DisasterType.Tornado:
                    BuildTornadoWorld();
                    break;
                default:
                    BuildWorld();
                    break;
            }

            GameObject[] rootsAfterBuild = scene.GetRootGameObjects();
            for (int i = 0; i < rootsAfterBuild.Length; i++)
            {
                GameObject root = rootsAfterBuild[i];
                if (root != previewRoot && !existingRoots.Contains(root))
                    root.transform.SetParent(previewRoot.transform, true);
            }

            SaveMapPreviewMaterials(previewRoot);
            DestroyImmediate(previewScenarioObject);
            scenario = null;
            materials.Clear();
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(disasterType + " haritası Scene görünümüne düzenleme önizlemesi olarak eklendi.");
            return true;
        }
        catch (System.Exception exception)
        {
            GameObject[] remainingRoots = scene.GetRootGameObjects();
            for (int i = 0; i < remainingRoots.Length; i++)
            {
                GameObject root = remainingRoots[i];
                if (root != null && !existingRoots.Contains(root))
                    Undo.DestroyObjectImmediate(root);
            }
            if (previewScenarioObject != null)
                DestroyImmediate(previewScenarioObject);
            if (previewRoot != null)
                Undo.DestroyObjectImmediate(previewRoot);
            scenario = null;
            materials.Clear();
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Harita önizlemesi oluşturulamadı",
                "Console'daki hatayı kontrol et. Sahnedeki kalıcı nesneler korunmuştur.", "Tamam");
            return false;
        }
    }

    private static void SaveMapPreviewMaterials(GameObject previewRoot)
    {
        const string parentFolder = "Assets/MapAuthoring";
        const string materialFolder = "Assets/MapAuthoring/PreviewMaterials";
        if (!AssetDatabase.IsValidFolder(parentFolder))
            AssetDatabase.CreateFolder("Assets", "MapAuthoring");
        if (!AssetDatabase.IsValidFolder(materialFolder))
            AssetDatabase.CreateFolder(parentFolder, "PreviewMaterials");

        Dictionary<Material, Material> savedBySource = new Dictionary<Material, Material>();
        Dictionary<Material, Material> transientBySource = new Dictionary<Material, Material>();
        Renderer[] renderers = previewRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] sharedMaterials = renderers[i].sharedMaterials;
            bool changed = false;
            for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
            {
                Material source = sharedMaterials[materialIndex];
                if (source == null)
                    continue;

                if (savedBySource.TryGetValue(source, out Material savedMaterial))
                {
                    sharedMaterials[materialIndex] = savedMaterial;
                    changed = true;
                    continue;
                }

                string fileName = MakeMapPreviewMaterialFileName(source.name);
                string path = materialFolder + "/" + fileName + ".mat";
                Material materialAsset = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (materialAsset == null)
                {
                    materialAsset = new Material(source);
                    materialAsset.name = source.name;
                    AssetDatabase.CreateAsset(materialAsset, path);
                }
                else
                {
                    EditorUtility.CopySerialized(source, materialAsset);
                    materialAsset.name = source.name;
                    EditorUtility.SetDirty(materialAsset);
                }

                savedBySource.Add(source, materialAsset);
                if (!AssetDatabase.Contains(source))
                    transientBySource[source] = source;
                sharedMaterials[materialIndex] = materialAsset;
                changed = true;
            }

            if (changed)
                renderers[i].sharedMaterials = sharedMaterials;
        }

        foreach (Material transient in transientBySource.Values)
        {
            if (transient != null)
                DestroyImmediate(transient);
        }
    }

    private static string MakeMapPreviewMaterialFileName(string materialName)
    {
        char[] invalidCharacters = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalidCharacters.Length; i++)
            materialName = materialName.Replace(invalidCharacters[i], '_');
        return string.IsNullOrWhiteSpace(materialName) ? "PreviewMaterial" : materialName;
    }
#endif

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
        BuildInvisibleMapBoundary("Sel", new Vector3(0f, 0f, 1f), 56f, 32f);
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
        BuildInvisibleMapBoundary("Heyelan", new Vector3(0f, 0f, 2f), 52f, 54f);
    }

    private void BuildWildfireWorld()
    {
        wildfireFlameLayers.Clear();
        wildfireFlameLights.Clear();
        wildfireTreeCanopies.Clear();
        wildfireSafePosition = new Vector3(17f, 0f, 7f);

        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color32(115, 123, 112, 255);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0028f;
        RenderSettings.ambientSkyColor = new Color32(153, 175, 192, 255);
        RenderSettings.ambientEquatorColor = new Color32(105, 112, 91, 255);
        RenderSettings.ambientGroundColor = new Color32(47, 52, 42, 255);
        RenderSettings.ambientIntensity = 0.9f;

        Material forestFloor = MakeMaterial("Yangın Tatbikatı Orman Zemini", new Color32(73, 91, 61, 255));
        Material grass = MakeMaterial("Kuru Orman Otu", new Color32(133, 125, 78, 255));
        Material pineDark = MakeMaterial("Çam İğnesi Koyu", new Color32(31, 65, 48, 255));
        Material pineLight = MakeMaterial("Çam İğnesi Aydınlık", new Color32(49, 91, 59, 255));
        Material bark = MakeMaterial("Orman Ağacı Kabuğu", new Color32(79, 57, 40, 255));
        Material rock = MakeMaterial("Orman Taşı", new Color32(92, 93, 83, 255));
        Material path = MakeMaterial("Orman Kaçış Patikası", new Color32(105, 91, 67, 255));
        Material pathLight = MakeMaterial("Tahliye Patikası Yansıtıcı İşaret", new Color32(241, 190, 77, 255));
        Material cabinWall = MakeMaterial("Orman Gözetleme Kulübesi Ahşabı", new Color32(119, 88, 60, 255));
        Material roof = MakeMaterial("Gözetleme Kulübesi Çatısı", new Color32(57, 61, 55, 255));
        Material darkMetal = MakeMaterial("Orman Acil Anons Kutusu", new Color32(38, 55, 56, 255));
        Material emergencyRed = MakeMaterial("Yangın İhbarı Kırmızı", new Color32(195, 48, 34, 255));
        Material safeGreen = MakeMaterial("Orman Yangını Güvenli Alan", new Color32(48, 139, 101, 255));
        Material beaconMat = MakeMaterial("Tahliye Feneri Yeşil", new Color32(69, 235, 172, 255));
        Texture2D flameTexture = CreateWildfireFlameTexture();
        Material flameParticles = CreateWildfireParticleMaterial(flameTexture, "Alev - Düzensiz Isı Katmanlı Parçacık Materyali");
        if (flameParticles != null && flameParticles.HasProperty("_EmissionColor"))
        {
            flameParticles.EnableKeyword("_EMISSION");
            flameParticles.SetColor("_EmissionColor", new Color(1f, 0.42f, 0.12f) * 1.35f);
            if (flameParticles.HasProperty("_EmissionMap"))
                flameParticles.SetTexture("_EmissionMap", flameTexture);
        }
        CreateBox("Geniş Orman Zemini", new Vector3(0f, -0.3f, 0f), new Vector3(76f, 0.6f, 64f), forestFloor);
        for (int i = 0; i < 65; i++)
        {
            float x = -29f + (i * 17.13f % 58f);
            float z = -24f + (i * 11.71f % 43f);
            GameObject mound = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mound.name = "Orman Arka Planındaki Alçak Sırt " + i;
            mound.transform.position = new Vector3(x, -2.6f + (i % 3) * 0.24f, z - 9f);
            mound.transform.localScale = new Vector3(8f + i % 5 * 1.6f, 4.5f + i % 4 * 0.9f, 7f + i % 3 * 1.3f);
            mound.GetComponent<Renderer>().sharedMaterial = i % 4 == 0 ? grass : forestFloor;
            Collider moundCollider = mound.GetComponent<Collider>();
            if (moundCollider != null) moundCollider.enabled = false;
        }

        // Thick pine stands frame a legible, open evacuation corridor to the east.
        int treeIndex = 0;
        for (int row = 0; row < 8; row++)
        {
            float z = -23f + row * 4.4f;
            for (int column = 0; column < 11; column++)
            {
                float x = -19f + column * 2.7f + Mathf.Sin((row + 2) * (column + 1)) * 0.65f;
                if (x > 6.8f || (z > 3f && x > -3f && x < 8f) ||
                    (Mathf.Abs(x - 17f) < 6f && z > 0f && z < 14f) || (Mathf.Abs(x) < 3.5f && z > 9f))
                    continue;
                BuildWildfireTree(new Vector3(x, 0f, z), treeIndex++, bark,
                    column % 3 == 0 ? pineLight : pineDark, 5.5f + (row + column) % 4 * 0.8f);
            }
        }

        // The open side trail is deliberately separated from the burning forest front.
        GameObject evacuationTrack = CreateBox("Ormandan Açık Alana Tahliye Patikası", new Vector3(10.8f, 0.025f, 8.4f),
            new Vector3(22f, 0.05f, 2.5f), path);
        Collider trackCollider = evacuationTrack.GetComponent<Collider>();
        if (trackCollider != null) trackCollider.enabled = false;
        for (int i = 0; i < 7; i++)
        {
            float x = 2.5f + i * 2.25f;
            CreateBox("Patika Kenarı Yansıtıcı İşaret " + i, new Vector3(x, 0.11f, 7.18f),
                new Vector3(0.35f, 0.12f, 0.12f), pathLight);
            CreateBox("Patika Kenarı Yansıtıcı İşaret Diğer Yan " + i, new Vector3(x, 0.11f, 9.62f),
                new Vector3(0.35f, 0.12f, 0.12f), pathLight);
        }

        // A small staffed trailhead shelter makes the 112 report an in-world action.
        CreateBox("Gözetleme Noktası Kulübe Arkası", new Vector3(-1.9f, 1.3f, 15.2f), new Vector3(4.2f, 2.6f, 0.25f), cabinWall);
        CreateBox("Gözetleme Noktası Kulübe Yanı Sol", new Vector3(-3.85f, 1.3f, 16.25f), new Vector3(0.25f, 2.6f, 2.1f), cabinWall);
        CreateBox("Gözetleme Noktası Kulübe Yanı Sağ", new Vector3(0.05f, 1.3f, 16.25f), new Vector3(0.25f, 2.6f, 2.1f), cabinWall);
        CreateBox("Gözetleme Noktası Kulübe Çatısı", new Vector3(-1.9f, 2.72f, 16.25f), new Vector3(4.5f, 0.26f, 2.6f), roof);
        CreateBox("Orman Acil Telefonu Direği", new Vector3(1.25f, 1.0f, 13.0f), new Vector3(0.16f, 2.0f, 0.16f), bark);
        GameObject radio = CreateBox("112 Orman Yangını Acil Telefonu", new Vector3(1.25f, 1.65f, 12.9f),
            new Vector3(0.82f, 0.92f, 0.36f), darkMetal);
        GameObject radioScreen = CreateBox("Acil Telefon Ekranı", new Vector3(1.25f, 1.82f, 12.69f),
            new Vector3(0.5f, 0.2f, 0.035f), beaconMat);
        GameObject radioButton = CreateBox("Acil Telefon Çağrı Tuşu", new Vector3(1.25f, 1.43f, 12.68f),
            new Vector3(0.23f, 0.2f, 0.06f), emergencyRed);
        GameObject wildfireHandsetPivot = new GameObject("Orman Acil Telefon Ahizesi Menteşesi");
        wildfireHandsetPivot.transform.position = new Vector3(1.25f, 2.02f, 12.67f);
        GameObject wildfireHandset = CreateBox("Orman Acil Telefon Ahizesi", wildfireHandsetPivot.transform.position,
            new Vector3(0.58f, 0.11f, 0.12f), darkMetal);
        wildfireHandset.transform.SetParent(wildfireHandsetPivot.transform, false);
        wildfireHandset.transform.localPosition = Vector3.zero;
        wildfireHandset.transform.localScale = new Vector3(0.58f, 0.11f, 0.12f);
        wildfireHandset.transform.rotation = Quaternion.Euler(0f, 0f, -8f);
        PrototypeInteractable reportTask = radio.AddComponent<PrototypeInteractable>();
        reportTask.Configure(PrototypeAction.CallForWildfireHelp, "Ahizeyi kaldır ve 112'yi ara");
        reportTask.ConfigureFloodPhoneVisual(wildfireHandsetPivot.transform);
        foreach (Collider visual in new[] { radioScreen.GetComponent<Collider>(), radioButton.GetComponent<Collider>(), wildfireHandset.GetComponent<Collider>() })
            if (visual != null) visual.enabled = false;
        scenario.AddWildfirePreparationTask(reportTask);

        wildfireSafePosition = new Vector3(17f, 0f, 7f);
        // Keep the clearing surface clearly above the meadow top (y=0) to prevent z-fighting.
        GameObject clearing = CreateBox("Açık ve Güvenli Tahliye Toplanma Alanı", wildfireSafePosition + new Vector3(0f, 0.035f, 0f),
            new Vector3(9f, 0.07f, 9f), safeGreen);
        Collider clearingCollider = clearing.GetComponent<Collider>();
        if (clearingCollider != null) clearingCollider.enabled = false;
        GameObject beaconPole = CreateBox("Tahliye Alanı Fener Direği", wildfireSafePosition + new Vector3(0f, 2.2f, 0f),
            new Vector3(0.22f, 4.4f, 0.22f), darkMetal);
        GameObject beacon = CreateBox("Uzakta Görünen Yeşil Tahliye Feneri", wildfireSafePosition + new Vector3(0f, 4.55f, 0f),
            new Vector3(0.85f, 0.42f, 0.85f), beaconMat);
        Renderer beaconRenderer = beacon.GetComponent<Renderer>();
        if (beaconRenderer != null && beaconRenderer.sharedMaterial != null && beaconRenderer.sharedMaterial.HasProperty("_EmissionColor"))
        {
            beaconRenderer.sharedMaterial.EnableKeyword("_EMISSION");
            beaconRenderer.sharedMaterial.SetColor("_EmissionColor", new Color(0.2f, 1f, 0.58f) * 2.2f);
        }
        Light beaconLight = beacon.AddComponent<Light>();
        beaconLight.type = LightType.Point;
        beaconLight.color = new Color(0.26f, 1f, 0.63f);
        beaconLight.intensity = 2.5f;
        beaconLight.range = 13f;
        Collider beaconCollider = beacon.GetComponent<Collider>();
        if (beaconCollider != null) beaconCollider.enabled = false;

        BuildWildfireFront(forestFloor, grass, flameParticles);
        BuildWildfireSmoke();
        BuildInvisibleMapBoundary("Orman Yangını", Vector3.zero, 76f, 64f);
    }

    private void BuildTornadoWorld()
    {
        tornadoLooseObjects.Clear();
        tornadoLooseObjectHomes.Clear();
        tornadoLooseObjectHomeRotations.Clear();
        tornadoLooseObjectVelocities.Clear();
        tornadoLooseObjectPreparationTasks.Clear();
        tornadoDamageProps.Clear();
        tornadoOriginalFogEnabled = RenderSettings.fog;
        tornadoOriginalFogMode = RenderSettings.fogMode;
        tornadoOriginalFogDensity = RenderSettings.fogDensity;
        tornadoOriginalFogColor = RenderSettings.fogColor;
        tornadoOriginalAmbientIntensity = RenderSettings.ambientIntensity;
        tornadoOriginalAmbientMode = RenderSettings.ambientMode;
        tornadoOriginalAmbientSkyColor = RenderSettings.ambientSkyColor;
        tornadoOriginalAmbientEquatorColor = RenderSettings.ambientEquatorColor;
        tornadoOriginalAmbientGroundColor = RenderSettings.ambientGroundColor;
        tornadoOriginalSkybox = RenderSettings.skybox;
        if (Application.isPlaying && tornadoOriginalSkybox != null)
        {
            tornadoRuntimeSkybox = new Material(tornadoOriginalSkybox);
            tornadoRuntimeSkybox.name = "Hortum Tatbikatı - Kararan Gökyüzü";
            tornadoSkyboxHasExposure = tornadoRuntimeSkybox.HasProperty("_Exposure");
            if (tornadoSkyboxHasExposure)
                tornadoOriginalSkyboxExposure = tornadoRuntimeSkybox.GetFloat("_Exposure");
            RenderSettings.skybox = tornadoRuntimeSkybox;
        }
        tornadoSun = GameObject.Find("Yumuşak Gün Işığı")?.GetComponent<Light>();
        tornadoOriginalSunIntensity = tornadoSun != null ? tornadoSun.intensity : 1f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color32(105, 111, 116, 255);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0015f;
        RenderSettings.ambientSkyColor = new Color32(134, 150, 162, 255);
        Material ground = MakeMaterial("Hortum Mahallesi Çim ve Zemin", new Color32(96, 112, 82, 255));
        Material lawn = MakeMaterial("Hortum Evi Ön Bahçesi", new Color32(76, 105, 70, 255));
        Material concrete = MakeMaterial("Hortum Mahallesi Yaya Yolu", new Color32(126, 124, 115, 255));
        Material floor = MakeMaterial("Hortum Evi İç Zemin", new Color32(135, 130, 117, 255));
        Material wall = MakeMaterial("Hortum Evi Dış Sıva", new Color32(185, 178, 160, 255));
        Material innerWall = MakeMaterial("Hortum Evi İç Sıva", new Color32(196, 190, 177, 255));
        Material trim = MakeMaterial("Hortum Evi Koyu Ahşap", new Color32(72, 61, 51, 255));
        Material roof = MakeMaterial("Hortum Evi Kiremit Çatısı", new Color32(102, 65, 52, 255));
        Material glass = MakeMaterial("Hortum Evi Pencere Camı", new Color32(71, 98, 111, 255));
        Material safe = MakeMaterial("Hortum Güvenli Oda Halısı", new Color32(89, 119, 96, 255));
        Material warning = MakeMaterial("Hortum Tehlike Uyarı Sarısı", new Color32(220, 164, 69, 255));
        Material metal = MakeMaterial("Hortum Hasarlı Direk Metali", new Color32(55, 61, 61, 255));
        Material road = MakeMaterial("Hortum Mahalle Yolu Asfaltı", new Color32(59, 65, 66, 255));
        Material binMaterial = MakeMaterial("Hortum Hafif Dış Mekân Eşyası", new Color32(55, 105, 92, 255));

        CreateBox("Hortum Mahallesi Zemin", new Vector3(0f, -0.25f, 0f), new Vector3(62f, 0.5f, 58f), ground);
        CreateBox("Mahalle Yolu", new Vector3(0f, 0.012f, 5.8f), new Vector3(54f, 0.04f, 3.6f), road);
        CreateBox("Evin Ön Bahçesi", new Vector3(-8f, 0.008f, 2.65f), new Vector3(10f, 0.025f, 6.0f), lawn);
        CreateBox("Bahçe Yaya Yolu", new Vector3(-8f, 0.027f, 1.55f), new Vector3(1.8f, 0.04f, 4.2f), concrete);
        CreateBox("Hortum Evi İç Zemin", new Vector3(-8f, -0.045f, -5f), new Vector3(7.95f, 0.11f, 8.75f), floor);
        CreateTornadoNeighborHouse("Batıdaki Komşu Ev", new Vector3(-21f, 0f, -5f), new Vector3(7f, 2.7f, 8f), wall, roof, trim, glass);
        CreateTornadoNeighborHouse("Doğudaki Komşu Ev", new Vector3(8f, 0f, -8f), new Vector3(7f, 2.7f, 8f), wall, roof, trim, glass);

        CreateBox("Hortum Evi Arka Duvarı", new Vector3(-8f, 1.45f, -9.4f), new Vector3(8f, 2.9f, 0.25f), wall);
        CreateBox("Hortum Evi Sol Duvarı", new Vector3(-12f, 1.45f, -5f), new Vector3(0.25f, 2.9f, 8.8f), wall);
        CreateBox("Hortum Evi Sağ Duvarı", new Vector3(-4f, 1.45f, -5f), new Vector3(0.25f, 2.9f, 8.8f), wall);
        CreateBox("Hortum Evi Ön Duvarı Sol", new Vector3(-10.35f, 1.45f, -0.6f), new Vector3(3.3f, 2.9f, 0.25f), wall);
        CreateBox("Hortum Evi Ön Duvarı Sağ", new Vector3(-5.65f, 1.45f, -0.6f), new Vector3(3.3f, 2.9f, 0.25f), wall);
        CreateBox("Hortum Evi Giriş Kapı Üstü", new Vector3(-8f, 2.48f, -0.6f), new Vector3(1.4f, 0.84f, 0.25f), wall);

        // A separate, enclosed rear room with its own door creates an actual windowless refuge.
        CreateBox("Güvenli Oda Bölme Duvarı Sol", new Vector3(-10.35f, 1.35f, -5f), new Vector3(3.3f, 2.7f, 0.22f), innerWall);
        CreateBox("Güvenli Oda Bölme Duvarı Sağ", new Vector3(-5.65f, 1.35f, -5f), new Vector3(3.3f, 2.7f, 0.22f), innerWall);
        CreateBox("Güvenli Oda Kapı Üstü", new Vector3(-8f, 2.42f, -5f), new Vector3(1.4f, 0.56f, 0.22f), innerWall);
        // Close the narrow eave gap above the refuge partition walls. This low ceiling
        // overlaps their tops and the roof edge so the outside sky cannot show through.
        CreateBox("Güvenli Oda Tavan Boşluğu Kapatması", new Vector3(-8f, 2.87f, -7.25f),
            new Vector3(8.05f, 0.34f, 4.45f), innerWall);
        CreateBox("Güvenli Oda Halısı", new Vector3(-8f, 0.018f, -7.25f), new Vector3(5.8f, 0.025f, 3.65f), safe);

        // Gabled roof and window trim make the house read as a small detached home, not a sealed cube.
        CreateBox("Hortum Evi Çatı Sol Eğim", new Vector3(-10.0f, 3.4f, -5f), new Vector3(4.45f, 0.24f, 9.35f), roof)
            .transform.rotation = Quaternion.Euler(0f, 0f, 10f);
        CreateBox("Hortum Evi Çatı Sağ Eğim", new Vector3(-6.0f, 3.4f, -5f), new Vector3(4.45f, 0.24f, 9.35f), roof)
            .transform.rotation = Quaternion.Euler(0f, 0f, -10f);
        CreateTornadoGableWall("Hortum Evi Ön Üçgen Duvarı", new Vector3(-8f, 2.9f, -0.47f), wall);
        CreateTornadoGableWall("Hortum Evi Arka Üçgen Duvarı", new Vector3(-8f, 2.9f, -9.27f), wall);
        CreateTornadoWindow("Hortum Evi Ön Pencere Sol", new Vector3(-10.25f, 1.78f, -0.43f), glass, trim);
        CreateTornadoWindow("Hortum Evi Ön Pencere Sağ", new Vector3(-5.75f, 1.78f, -0.43f), glass, trim);

        BuildTornadoPlayableTasks(warning, metal, binMaterial, roof, trim, safe, concrete, innerWall, glass);
        BuildTornadoVfx();
        BuildInvisibleMapBoundary("Hortum", Vector3.zero, 62f, 58f);
        if (!Application.isPlaying)
        {
            RenderSettings.fog = tornadoOriginalFogEnabled;
            RenderSettings.fogMode = tornadoOriginalFogMode;
            RenderSettings.fogDensity = tornadoOriginalFogDensity;
            RenderSettings.fogColor = tornadoOriginalFogColor;
            RenderSettings.ambientIntensity = tornadoOriginalAmbientIntensity;
            RenderSettings.ambientMode = tornadoOriginalAmbientMode;
            RenderSettings.ambientSkyColor = tornadoOriginalAmbientSkyColor;
            RenderSettings.ambientEquatorColor = tornadoOriginalAmbientEquatorColor;
            RenderSettings.ambientGroundColor = tornadoOriginalAmbientGroundColor;
            if (tornadoSun != null) tornadoSun.intensity = tornadoOriginalSunIntensity;
        }
    }

    private void CreateTornadoWindow(string name, Vector3 center, Material glass, Material frame)
    {
        CreateBox(name + " Cam", center, new Vector3(1.25f, 0.92f, 0.08f), glass);
        CreateBox(name + " Üst Kasa", center + Vector3.up * 0.53f, new Vector3(1.42f, 0.10f, 0.16f), frame);
        CreateBox(name + " Alt Kasa", center - Vector3.up * 0.53f, new Vector3(1.42f, 0.10f, 0.22f), frame);
        CreateBox(name + " Sol Kasa", center + Vector3.left * 0.68f, new Vector3(0.10f, 1.12f, 0.16f), frame);
        CreateBox(name + " Sağ Kasa", center + Vector3.right * 0.68f, new Vector3(0.10f, 1.12f, 0.16f), frame);
        CreateBox(name + " Orta Kayıt", center, new Vector3(0.055f, 0.92f, 0.13f), frame);
    }

    private void CreateTornadoGableWall(string name, Vector3 baseCenter, Material wallMaterial)
    {
        GameObject gable = new GameObject(name);
        gable.transform.position = baseCenter;
        Mesh mesh = new Mesh { name = name + " Mesh" };
        mesh.vertices = new[]
        {
            new Vector3(-4f, 0f, 0f), new Vector3(4f, 0f, 0f), new Vector3(0f, 0.85f, 0f),
            new Vector3(-4f, 0f, 0f), new Vector3(4f, 0f, 0f), new Vector3(0f, 0.85f, 0f)
        };
        mesh.triangles = new[] { 0, 1, 2, 5, 4, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        gable.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = gable.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = wallMaterial;
        MeshCollider collider = gable.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
    }

    private void CreateTornadoNeighborHouse(string name, Vector3 center, Vector3 size, Material wall, Material roof, Material trim, Material glass)
    {
        CreateBox(name + " Gövde", center + Vector3.up * (size.y * 0.5f), size, wall);
        CreateBox(name + " Çatı", center + Vector3.up * (size.y + 0.18f), new Vector3(size.x + 0.55f, 0.36f, size.z + 0.55f), roof);
        CreateBox(name + " Saçak", center + Vector3.up * (size.y - 0.08f), new Vector3(size.x + 0.8f, 0.12f, size.z + 0.8f), trim);
        CreateBox(name + " Ön Pencere", center + new Vector3(0f, 1.65f, size.z * 0.5f + 0.035f),
            new Vector3(1.45f, 0.95f, 0.08f), glass);
    }

    private void BuildTornadoPlayableTasks(Material warning, Material metal, Material binMaterial,
        Material roof, Material trim, Material safe, Material concrete, Material innerWall, Material glass)
    {
        Material chairPlastic = MakeMaterial("Hortum Bahçe Sandalyesi Füme Mavi Plastik", new Color32(73, 91, 105, 255));
        Material tableWood = MakeMaterial("Hortum Bahçe Masası Sıcak Ahşap", new Color32(139, 99, 65, 255));
        Material darkPlastic = MakeMaterial("Hortum Koyu Plastik", new Color32(48, 52, 55, 255));
        Material potMaterial = MakeMaterial("Hortum Bahçe Saksısı Seramik", new Color32(154, 92, 68, 255));
        Material leafMaterial = MakeMaterial("Hortum Saksı Bitkisi Yaprakları", new Color32(70, 103, 65, 255));
        Material bagFabric = MakeMaterial("Hortum Acil Çanta Turuncu Kırmızı Kumaş", new Color32(174, 69, 45, 255));
        Material medicalRed = MakeMaterial("Hortum İlk Yardım Seti Kırmızısı", new Color32(177, 47, 42, 255));
        Material cleanWhite = MakeMaterial("Hortum İlk Yardım Seti Beyazı", new Color32(239, 232, 214, 255));

        // A real hinged exterior door and a second hinged door divide the entrance from the refuge.
        CreateTornadoDoor("Bahçe Giriş Kapısı", new Vector3(-8.70f, 0f, -0.60f),
            1.40f, 2.12f, trim, false);
        PrototypeInteractable safeRoomDoor = CreateTornadoDoor("Penceresiz Oda Kapısı", new Vector3(-8.70f, 0f, -5f),
            1.40f, 2.12f, trim, false);

        GameObject storage = CreateTornadoProp("Eşya Sabitleme Rafı", new Vector3(-11.55f, 0f, -2.8f),
            new Vector3(0.44f, 1.5f, 0.62f));
        Collider storageBodyCollider = storage.GetComponent<Collider>();
        if (storageBodyCollider != null) storageBodyCollider.enabled = false;
        CreateTornadoPropPart(storage.transform, PrimitiveType.Cube, "Raf Arka Paneli", new Vector3(0f, 0.92f, 0f),
            new Vector3(0.12f, 1.82f, 3.4f), trim);
        CreateTornadoPropPart(storage.transform, PrimitiveType.Cube, "Raf Üst Tabla", new Vector3(0.38f, 1.48f, 0f),
            new Vector3(1.18f, 0.09f, 3.4f), tableWood);
        GameObject lowerShelf = CreateTornadoPropPart(storage.transform, PrimitiveType.Cube, "Raf Alt Eşya Tablası",
            new Vector3(0.38f, 0.12f, 0f), new Vector3(1.18f, 0.09f, 3.4f), tableWood);
        BoxCollider lowerShelfCollider = lowerShelf.GetComponent<BoxCollider>();
        if (lowerShelfCollider != null)
        {
            lowerShelfCollider.enabled = true;
            lowerShelfCollider.isTrigger = true;
        }
        for (int side = -1; side <= 1; side += 2)
            CreateTornadoPropPart(storage.transform, PrimitiveType.Cube, "Raf Yan Desteği",
                new Vector3(0.38f, 0.78f, side * 1.62f), new Vector3(1.18f, 1.30f, 0.10f), trim);
        GameObject storageLabel = CreateTornadoPropPart(storage.transform, PrimitiveType.Cube, "Raf Sabitleme Levhası",
            new Vector3(0.20f, 1.72f, 0f), new Vector3(0.10f, 0.30f, 1.65f), warning);
        BoxCollider storageLabelCollider = storageLabel.GetComponent<BoxCollider>();
        if (storageLabelCollider != null) storageLabelCollider.enabled = false;
        CreateTornadoPropPart(storage.transform, PrimitiveType.Cube, "Raf Levhası Siyah Şerit",
            new Vector3(0.26f, 1.72f, 0f), new Vector3(0.02f, 0.08f, 1.45f), darkPlastic);
        PrototypeInteractable gardenStorage = storage.AddComponent<PrototypeInteractable>();
        gardenStorage.Configure(PrototypeAction.TornadoPreparation, "Bahçedeki üç eşyayı güvenli rafa taşı (0/3)");

        GameObject chair = CreateTornadoProp("Bahçe Sandalyesi", new Vector3(-2.8f, 0f, 3.5f), new Vector3(0.74f, 0.92f, 0.72f));
        CreateTornadoPropPart(chair.transform, PrimitiveType.Cube, "Sandalye Oturağı", new Vector3(0f, 0.44f, 0f),
            new Vector3(0.60f, 0.10f, 0.58f), chairPlastic);
        CreateTornadoPropPart(chair.transform, PrimitiveType.Cube, "Sandalye Sırtlığı", new Vector3(0f, 0.70f, 0.23f),
            new Vector3(0.60f, 0.44f, 0.08f), chairPlastic);
        for (int sx = -1; sx <= 1; sx += 2)
        for (int sz = -1; sz <= 1; sz += 2)
            CreateTornadoPropPart(chair.transform, PrimitiveType.Cube, "Sandalye Ayağı",
                new Vector3(sx * 0.22f, 0.21f, sz * 0.20f), new Vector3(0.055f, 0.40f, 0.055f), darkPlastic);

        PrototypeInteractable chairTask = chair.AddComponent<PrototypeInteractable>();
        chairTask.Configure(PrototypeAction.TornadoPreparation, "Bahçe sandalyesi");
        chairTask.ConfigureHeldGrip(Vector3.up, Vector3.up, Vector3.back);

        scenario.AddTornadoGardenItem(chairTask);
        RegisterTornadoLooseObject(chair.transform, chairTask);

        GameObject table = CreateTornadoProp("Bahçe Masası", new Vector3(-13.3f, 0f, 4.1f), new Vector3(0.95f, 0.82f, 0.74f));
        CreateTornadoPropPart(table.transform, PrimitiveType.Cube, "Masa Tablası", new Vector3(0f, 0.72f, 0f),
            new Vector3(1.18f, 0.09f, 0.84f), tableWood);
        for (int sx = -1; sx <= 1; sx += 2)
        for (int sz = -1; sz <= 1; sz += 2)
            CreateTornadoPropPart(table.transform, PrimitiveType.Cube, "Masa Ayağı",
                new Vector3(sx * 0.48f, 0.35f, sz * 0.32f), new Vector3(0.07f, 0.68f, 0.07f), darkPlastic);
        PrototypeInteractable tableTask = table.AddComponent<PrototypeInteractable>();
        tableTask.Configure(PrototypeAction.TornadoPreparation, "Bahçe masası");
        tableTask.ConfigureHeldGrip(Vector3.up, Vector3.up, Vector3.back);
        scenario.AddTornadoGardenItem(tableTask);
        RegisterTornadoLooseObject(table.transform, tableTask);

        GameObject planter = CreateTornadoProp("Bahçe Saksısı", new Vector3(-3.2f, 0f, 1.1f), new Vector3(0.62f, 0.9f, 0.62f));
        CreateTornadoPropPart(planter.transform, PrimitiveType.Cylinder, "Seramik Saksı Gövdesi", new Vector3(0f, 0.28f, 0f),
            new Vector3(0.29f, 0.28f, 0.29f), potMaterial);
        CreateTornadoPropPart(planter.transform, PrimitiveType.Cylinder, "Saksı Kenarı", new Vector3(0f, 0.55f, 0f),
            new Vector3(0.34f, 0.055f, 0.34f), potMaterial);
        for (int leaf = 0; leaf < 5; leaf++)
        {
            float angle = leaf * 72f;
            float radians = angle * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(radians), 0.65f, Mathf.Sin(radians));
            GameObject blade = CreateTornadoPropPart(planter.transform, PrimitiveType.Sphere, "Saksı Bitkisi Yaprağı",
                new Vector3(direction.x * 0.16f, 0.73f + (leaf % 2) * 0.05f, direction.z * 0.16f),
                new Vector3(0.15f, 0.36f, 0.12f), leafMaterial);
            blade.transform.localRotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(18f, 0f, 0f);
        }
        PrototypeInteractable planterTask = planter.AddComponent<PrototypeInteractable>();
        planterTask.Configure(PrototypeAction.TornadoPreparation, "Bahçe saksısı");
        planterTask.ConfigureHeldGrip(Vector3.up, Vector3.up, Vector3.back);
        scenario.AddTornadoGardenItem(planterTask);
        RegisterTornadoLooseObject(planter.transform, planterTask);

        CreateBox("Acil Hazırlık Tezgâhı", new Vector3(-6.15f, 0.36f, -3.05f),
            new Vector3(2.75f, 0.72f, 0.9f), trim);
        CreateBox("Acil Hazırlık Tezgâhı Üstü", new Vector3(-6.15f, 0.76f, -3.05f),
            new Vector3(2.9f, 0.08f, 1.0f), concrete);

        GameObject bag = CreateTornadoProp("Acil Durum Çantası", new Vector3(-7.15f, 0.80f, -3.05f),
            new Vector3(0.65f, 0.48f, 0.46f));
        CreateTornadoPropPart(bag.transform, PrimitiveType.Cube, "Çanta Kumaş Gövdesi", new Vector3(0f, 0.17f, 0f),
            new Vector3(0.60f, 0.34f, 0.42f), bagFabric);
        GameObject bagPocket = CreateTornadoPropPart(bag.transform, PrimitiveType.Cube, "Çanta Ön Cebi", new Vector3(0f, 0.16f, -0.23f),
            new Vector3(0.37f, 0.19f, 0.035f), darkPlastic);
        BoxCollider bagPocketCollider = bagPocket.GetComponent<BoxCollider>();
        if (bagPocketCollider != null)
        {
            bagPocketCollider.enabled = true;
            bagPocketCollider.isTrigger = true;
        }
        CreateTornadoPropPart(bag.transform, PrimitiveType.Cube, "Acil Çanta Beyaz Artı Yatay", new Vector3(0f, 0.17f, -0.255f),
            new Vector3(0.20f, 0.045f, 0.025f), cleanWhite);
        CreateTornadoPropPart(bag.transform, PrimitiveType.Cube, "Acil Çanta Beyaz Artı Dikey", new Vector3(0f, 0.17f, -0.258f),
            new Vector3(0.045f, 0.17f, 0.03f), cleanWhite);
        CreateTornadoPropPart(bag.transform, PrimitiveType.Cube, "Çanta Taşıma Askısı", new Vector3(0f, 0.38f, 0.02f),
            new Vector3(0.055f, 0.035f, 0.40f), warning);
        GameObject bagLatch = CreateTornadoPropPart(bag.transform, PrimitiveType.Cube, "Acil Çanta Açma Tokası",
            new Vector3(0.23f, 0.29f, -0.265f), new Vector3(0.10f, 0.10f, 0.04f), warning);
        BoxCollider bagLatchCollider = bagLatch.GetComponent<BoxCollider>();
        if (bagLatchCollider != null) bagLatchCollider.enabled = true;
        GameObject closedBagLid = new GameObject("Kapalı Çanta Kapağı");
        closedBagLid.transform.SetParent(bag.transform, false);
        CreateTornadoPropPart(closedBagLid.transform, PrimitiveType.Cube, "Kapalı Kapak", new Vector3(0f, 0.37f, 0f),
            new Vector3(0.62f, 0.08f, 0.44f), bagFabric);
        GameObject openBagInterior = new GameObject("Açık Çanta İç Bölmesi");
        openBagInterior.transform.SetParent(bag.transform, false);
        GameObject openBagTray = CreateTornadoPropPart(openBagInterior.transform, PrimitiveType.Cube, "Açık Bölme Tabanı",
            new Vector3(0f, 0.35f, 0f), new Vector3(0.56f, 0.045f, 0.37f), darkPlastic);
        BoxCollider openBagTrayCollider = openBagTray.GetComponent<BoxCollider>();
        if (openBagTrayCollider != null)
        {
            openBagTrayCollider.enabled = true;
            openBagTrayCollider.isTrigger = true;
        }
        CreateTornadoPropPart(openBagInterior.transform, PrimitiveType.Cube, "Bölme Arka Kenarı", new Vector3(0f, 0.40f, 0.16f),
            new Vector3(0.56f, 0.10f, 0.05f), bagFabric);
        PrototypeInteractable emergencyBag = bag.AddComponent<PrototypeInteractable>();
        emergencyBag.Configure(PrototypeAction.TornadoPreparation, "Acil durum çantasını hazırla");
        emergencyBag.ConfigureBagVisual(closedBagLid, openBagInterior, true);

        GameObject water = CreateTornadoProp("İçme Suyu Şişesi", new Vector3(-6.55f, 0.80f, -3.05f), new Vector3(0.20f, 0.40f, 0.20f));
        CreateTornadoPropPart(water.transform, PrimitiveType.Cylinder, "Şeffaf Su Şişesi", new Vector3(0f, 0.20f, 0f),
            new Vector3(0.10f, 0.19f, 0.10f), cleanWhite);
        CreateTornadoPropPart(water.transform, PrimitiveType.Cylinder, "Mavi Şişe Kapağı", new Vector3(0f, 0.41f, 0f),
            new Vector3(0.055f, 0.025f, 0.055f), glass);
        PrototypeInteractable waterTask = water.AddComponent<PrototypeInteractable>();
        waterTask.Configure(PrototypeAction.CollectBagItem, "Su şişesi");
        waterTask.ConfigureHeldGrip(Vector3.up, Vector3.up, Vector3.back);
        scenario.AddTornadoEmergencySupply(waterTask);

        GameObject flashlight = CreateTornadoProp("El Feneri", new Vector3(-5.65f, 0.80f, -3.05f), new Vector3(0.34f, 0.18f, 0.18f));
        CreateTornadoPropPart(flashlight.transform, PrimitiveType.Cylinder, "Fener Gövdesi", new Vector3(0f, 0.09f, 0f),
            new Vector3(0.09f, 0.16f, 0.09f), darkPlastic, Quaternion.Euler(90f, 0f, 0f));
        CreateTornadoPropPart(flashlight.transform, PrimitiveType.Cylinder, "Fener Merceği", new Vector3(0f, 0.09f, -0.15f),
            new Vector3(0.11f, 0.035f, 0.11f), warning, Quaternion.Euler(90f, 0f, 0f));
        PrototypeInteractable flashlightTask = flashlight.AddComponent<PrototypeInteractable>();
        flashlightTask.Configure(PrototypeAction.CollectBagItem, "El feneri");
        flashlightTask.ConfigureHeldGrip(Vector3.back, Vector3.up, Vector3.up);
        scenario.AddTornadoEmergencySupply(flashlightTask);

        // Keep the kit fully inside the tabletop footprint; the old x position left it
        // hanging beyond the table edge despite having the correct tabletop height.
        GameObject firstAid = CreateTornadoProp("İlk Yardım Seti", new Vector3(-4.95f, 0.785f, -3.05f), new Vector3(0.42f, 0.28f, 0.34f));
        CreateTornadoPropPart(firstAid.transform, PrimitiveType.Cube, "İlk Yardım Çantası", new Vector3(0f, 0.14f, 0f),
            new Vector3(0.40f, 0.25f, 0.32f), medicalRed);
        CreateTornadoPropPart(firstAid.transform, PrimitiveType.Cube, "İlk Yardım Arması", new Vector3(0f, 0.15f, -0.17f),
            new Vector3(0.16f, 0.16f, 0.025f), cleanWhite);
        CreateTornadoPropPart(firstAid.transform, PrimitiveType.Cube, "İlk Yardım Artısı", new Vector3(0f, 0.15f, -0.19f),
            new Vector3(0.045f, 0.12f, 0.02f), medicalRed);
        CreateTornadoPropPart(firstAid.transform, PrimitiveType.Cube, "İlk Yardım Arması Arka Yüz",
            new Vector3(0f, 0.15f, 0.17f), new Vector3(0.16f, 0.16f, 0.025f), cleanWhite);
        CreateTornadoPropPart(firstAid.transform, PrimitiveType.Cube, "İlk Yardım Artısı Arka Yüz Yatay",
            new Vector3(0f, 0.15f, 0.19f), new Vector3(0.12f, 0.045f, 0.02f), medicalRed);
        CreateTornadoPropPart(firstAid.transform, PrimitiveType.Cube, "İlk Yardım Artısı Arka Yüz Dikey",
            new Vector3(0f, 0.15f, 0.19f), new Vector3(0.045f, 0.12f, 0.02f), medicalRed);
        PrototypeInteractable firstAidTask = firstAid.AddComponent<PrototypeInteractable>();
        firstAidTask.Configure(PrototypeAction.CollectBagItem, "İlk yardım seti");
        firstAidTask.ConfigureHeldGrip(Vector3.up, Vector3.up, Vector3.back);
        scenario.AddTornadoEmergencySupply(firstAidTask);

        // The room is identified by its windowless construction and the task panel;
        // do not place a fake, green, clickable-looking marker on its wall.
        GameObject safeRoomTaskObject = new GameObject("Penceresiz Güvenli Oda Hazırlık Görevi");
        safeRoomTaskObject.transform.position = new Vector3(-8f, 0.1f, -8f);
        PrototypeInteractable safeRoomTask = safeRoomTaskObject.AddComponent<PrototypeInteractable>();
        safeRoomTask.Configure(PrototypeAction.TornadoPreparation, "Penceresiz güvenli odaya gir ve kapıyı kapat");

        // A wired telephone is mounted inside the refuge, not at an empty marker in the garden.
        GameObject phone = CreateTornadoProp("Hortum Sonrası Acil Yardım Telefonu", new Vector3(-10.55f, 0.84f, -9.05f),
            new Vector3(0.48f, 0.72f, 0.18f));
        CreateTornadoPropPart(phone.transform, PrimitiveType.Cube, "Duvar Telefonu Gövdesi", new Vector3(0f, 0.34f, 0f),
            new Vector3(0.42f, 0.65f, 0.12f), metal);
        CreateTornadoPropPart(phone.transform, PrimitiveType.Cube, "Telefon Tuş Takımı", new Vector3(0f, 0.27f, -0.075f),
            new Vector3(0.23f, 0.25f, 0.035f), cleanWhite);
        GameObject handset = new GameObject("Hortum Ahize Yuvası");
        handset.transform.SetParent(phone.transform, false);
        handset.transform.localPosition = new Vector3(0f, 0.66f, -0.11f);
        CreateTornadoPropPart(handset.transform, PrimitiveType.Cylinder, "Kırmızı Ahize Sapı",
            Vector3.zero, new Vector3(0.045f, 0.19f, 0.045f), medicalRed, Quaternion.Euler(0f, 0f, 90f));
        CreateTornadoPropPart(handset.transform, PrimitiveType.Cylinder, "Kırmızı Ahize Kulaklığı Sol",
            new Vector3(-0.16f, 0f, 0f), new Vector3(0.065f, 0.055f, 0.065f), medicalRed,
            Quaternion.Euler(0f, 0f, 90f));
        CreateTornadoPropPart(handset.transform, PrimitiveType.Cylinder, "Kırmızı Ahize Kulaklığı Sağ",
            new Vector3(0.16f, 0f, 0f), new Vector3(0.065f, 0.055f, 0.065f), medicalRed,
            Quaternion.Euler(0f, 0f, 90f));
        PrototypeInteractable call = phone.AddComponent<PrototypeInteractable>();
        call.Configure(PrototypeAction.CallForTornadoHelp, "Duvar telefonundan 112'yi ara ve hasarı bildir");
        call.ConfigureTornadoPhoneVisual(handset.transform);
        phone.SetActive(false);

        scenario.AddTornadoPreparationTask(gardenStorage);
        scenario.AddTornadoPreparationTask(emergencyBag);
        scenario.AddTornadoPreparationTask(safeRoomTask);
        scenario.ConfigureTornadoTaskStations(gardenStorage, emergencyBag, safeRoomTask, safeRoomDoor, firstAidTask);

        // Unsecured yard props and a small amount of roadside clutter respond to the same moving vortex.
        GameObject roadsideBin = CreateTornadoProp("Yol Kenarı Tekerlekli Çöp Kutusu", new Vector3(-17.8f, 0f, 4.8f), new Vector3(0.62f, 0.98f, 0.62f));
        CreateTornadoPropPart(roadsideBin.transform, PrimitiveType.Cylinder, "Çöp Kutusu Gövdesi", new Vector3(0f, 0.42f, 0f),
            new Vector3(0.28f, 0.42f, 0.28f), binMaterial);
        CreateTornadoPropPart(roadsideBin.transform, PrimitiveType.Cylinder, "Çöp Kutusu Kapağı", new Vector3(0f, 0.85f, 0f),
            new Vector3(0.31f, 0.04f, 0.31f), darkPlastic);
        RegisterTornadoLooseObject(roadsideBin.transform, null);
        GameObject lightCanopy = CreateTornadoProp("Yol Kenarı Hafif Pazar Tentəsi", new Vector3(17f, 0f, 5.0f), new Vector3(1.1f, 1.8f, 1.0f));
        CreateTornadoPropPart(lightCanopy.transform, PrimitiveType.Cylinder, "Tente Direği", new Vector3(0f, 0.85f, 0f),
            new Vector3(0.045f, 0.82f, 0.045f), metal);
        CreateTornadoPropPart(lightCanopy.transform, PrimitiveType.Cube, "Katlanır Tente", new Vector3(0f, 1.62f, 0f),
            new Vector3(1.0f, 0.12f, 0.92f), warning);
        RegisterTornadoLooseObject(lightCanopy.transform, null);

        GameObject damagedPole = CreateTornadoProp("Hortum Sonrası Hasarlı Elektrik Direği", new Vector3(8f, 0f, 1f),
            new Vector3(0.30f, 3.25f, 0.30f));
        Collider poleBlocker = damagedPole.GetComponent<Collider>();
        if (poleBlocker != null) poleBlocker.enabled = false;
        CreateTornadoPropPart(damagedPole.transform, PrimitiveType.Cylinder, "Yere Yatmış Direk", new Vector3(-0.25f, 0.20f, 0f),
            new Vector3(0.14f, 1.75f, 0.14f), metal, Quaternion.Euler(0f, 0f, 83f));
        CreateTornadoPropPart(damagedPole.transform, PrimitiveType.Cube, "Direk Traversi", new Vector3(-1.65f, 0.30f, 0f),
            new Vector3(1.5f, 0.09f, 0.10f), trim);
        damagedPole.SetActive(false);
        tornadoDamageProps.Add(damagedPole);
        CreateTornadoCable("Kopmuş Elektrik Kablosu A", new Vector3(8.0f, 2.5f, 1.0f), new Vector3(7.4f, 0.12f, 1.9f), metal);
        CreateTornadoCable("Kopmuş Elektrik Kablosu B", new Vector3(7.8f, 2.4f, 1.0f), new Vector3(9.3f, 0.10f, 1.6f), metal);

        GameObject hazardSign = CreateTornadoProp("Elektrik Hattı Tehlike Bildirim Levhası", new Vector3(2.2f, 0f, 1f),
            new Vector3(1.55f, 1.55f, 0.6f));
        Collider signRootCollider = hazardSign.GetComponent<Collider>();
        if (signRootCollider != null) signRootCollider.enabled = false;
        // A grounded, two-sided road warning board replaces the thin floating-looking post.
        CreateTornadoPropPart(hazardSign.transform, PrimitiveType.Cube, "Yere Basan Geniş Levha Ayağı",
            new Vector3(0f, 0.055f, 0f), new Vector3(1.42f, 0.11f, 0.52f), darkPlastic);
        for (int side = -1; side <= 1; side += 2)
        {
            CreateTornadoPropPart(hazardSign.transform, PrimitiveType.Cube, "Levha Metal Taşıyıcı " + side,
                new Vector3(side * 0.48f, 0.49f, 0f), new Vector3(0.075f, 0.90f, 0.075f), metal,
                Quaternion.Euler(0f, 0f, side * -7f));
        }
        GameObject signBoard = CreateTornadoPropPart(hazardSign.transform, PrimitiveType.Cube, "Geniş Sarı Elektrik Tehlike Levhası",
            new Vector3(0f, 1.12f, 0f), new Vector3(1.50f, 0.88f, 0.12f), warning);
        BoxCollider signBoardCollider = signBoard.GetComponent<BoxCollider>();
        if (signBoardCollider != null) signBoardCollider.enabled = true;
        // Keep the warning readable as a simple physical symbol; world-space TextMesh
        // was oversized and showed through the house walls from the first-person view.
        for (int side = -1; side <= 1; side += 2)
        {
            float faceZ = side * 0.067f;
            Quaternion faceRotation = side < 0 ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
            GameObject bolt = new GameObject("Elektrik Uyarısı Yıldırım Sembolü " + side);
            bolt.transform.SetParent(hazardSign.transform, false);
            bolt.transform.localPosition = new Vector3(0f, 1.12f, faceZ);
            bolt.transform.localRotation = faceRotation;
            CreateTornadoPropPart(bolt.transform, PrimitiveType.Cube, "Yıldırım Üst Parçası",
                new Vector3(0.04f, 0.20f, 0f), new Vector3(0.22f, 0.10f, 0.025f), darkPlastic,
                Quaternion.Euler(0f, 0f, -35f));
            CreateTornadoPropPart(bolt.transform, PrimitiveType.Cube, "Yıldırım Orta Parçası",
                new Vector3(-0.015f, 0.015f, 0f), new Vector3(0.31f, 0.10f, 0.025f), darkPlastic,
                Quaternion.Euler(0f, 0f, 35f));
            CreateTornadoPropPart(bolt.transform, PrimitiveType.Cube, "Yıldırım Alt Parçası",
                new Vector3(0.035f, -0.18f, 0f), new Vector3(0.23f, 0.10f, 0.025f), darkPlastic,
                Quaternion.Euler(0f, 0f, -35f));
        }
        PrototypeInteractable inspect = hazardSign.AddComponent<PrototypeInteractable>();
        inspect.Configure(PrototypeAction.InspectTornadoHazard, "Kopmuş hattı levhadan güvenli mesafeden tespit et");
        scenario.AddTornadoAfterTask(inspect);
        hazardSign.SetActive(false);
        tornadoDamageProps.Add(hazardSign);

        GameObject roofDebris = CreateTornadoProp("Hortum Sonrası Kopmuş Çatı Parçaları", new Vector3(2.8f, 0f, -7.4f),
            new Vector3(2.8f, 0.28f, 1.1f));
        CreateTornadoPropPart(roofDebris.transform, PrimitiveType.Cube, "Kırık Kiremit Paneli", new Vector3(0f, 0.17f, 0f),
            new Vector3(2.65f, 0.23f, 0.90f), roof, Quaternion.Euler(5f, 18f, -7f));
        roofDebris.SetActive(false);
        tornadoDamageProps.Add(roofDebris);
        GameObject fallenBranch = CreateTornadoProp("Hortum Sonrası Düşen Ağaç Dalı", new Vector3(2.2f, 0f, 4.1f), new Vector3(2.5f, 0.7f, 0.65f));
        CreateTornadoPropPart(fallenBranch.transform, PrimitiveType.Cylinder, "Kırık Ana Dal", new Vector3(0f, 0.22f, 0f),
            new Vector3(0.15f, 1.2f, 0.15f), trim, Quaternion.Euler(0f, 0f, 76f));
        for (int twig = -1; twig <= 1; twig++)
            CreateTornadoPropPart(fallenBranch.transform, PrimitiveType.Cylinder, "Kırık Yan Dal",
                new Vector3(twig * 0.45f, 0.38f, 0f), new Vector3(0.055f, 0.45f, 0.055f), trim,
                Quaternion.Euler(0f, 0f, 32f + twig * 24f));
        fallenBranch.SetActive(false);
        tornadoDamageProps.Add(fallenBranch);

        GameObject fenceDamage = new GameObject("Hortum Sonrası Devrilmiş Bahçe Çiti");
        fenceDamage.transform.position = new Vector3(4.7f, 0f, -0.65f);
        CreateTornadoPropPart(fenceDamage.transform, PrimitiveType.Cube, "Yerdeki Çit Tahtası A", new Vector3(0f, 0.09f, 0f),
            new Vector3(3.2f, 0.16f, 0.12f), trim, Quaternion.Euler(0f, 12f, 7f));
        CreateTornadoPropPart(fenceDamage.transform, PrimitiveType.Cube, "Yerdeki Çit Tahtası B", new Vector3(0.15f, 0.13f, 0.42f),
            new Vector3(2.4f, 0.16f, 0.12f), trim, Quaternion.Euler(0f, -17f, -6f));
        fenceDamage.SetActive(false);
        tornadoDamageProps.Add(fenceDamage);

        GameObject survivor = BuildTornadoResident(new Vector3(0.8f, 0f, 1.9f));
        PrototypeInteractable assist = survivor.AddComponent<PrototypeInteractable>();
        assist.Configure(PrototypeAction.AssistTornadoResident, "Yaralı komşunun durumunu kontrol et ve ilk yardım uygula");
        scenario.AddTornadoAfterTask(call);
        scenario.AddTornadoAfterTask(assist);
        tornadoDamageProps.Add(phone);
        tornadoDamageProps.Add(survivor);
    }

    private PrototypeInteractable CreateTornadoDoor(string name, Vector3 hinge, float width, float height,
        Material wood, bool initiallyOpen)
    {
        GameObject root = new GameObject(name);
        root.transform.position = hinge;
        // Let the closed leaf overlap the jambs and lintel slightly so camera motion
        // cannot expose thin slivers through the doorway.
        float leafWidth = width + 0.06f;
        float leafHeight = height + 0.035f;
        BoxCollider panelCollider = root.AddComponent<BoxCollider>();
        panelCollider.center = new Vector3(leafWidth * 0.5f, leafHeight * 0.5f, 0f);
        panelCollider.size = new Vector3(leafWidth, leafHeight, 0.16f);
        CreateTornadoPropPart(root.transform, PrimitiveType.Cube, name + " Kapı Kanadı",
            new Vector3(leafWidth * 0.5f, leafHeight * 0.5f, 0f), new Vector3(leafWidth, leafHeight, 0.12f), wood);
        CreateTornadoPropPart(root.transform, PrimitiveType.Cube, name + " Kapı Kolu",
            new Vector3(leafWidth - 0.13f, 1.02f, -0.08f), new Vector3(0.06f, 0.16f, 0.06f), wood);
        PrototypeInteractable interaction = root.AddComponent<PrototypeInteractable>();
        interaction.Configure(PrototypeAction.ToggleTornadoDoor, name);
        interaction.ConfigureTornadoDoor(root.transform, panelCollider, initiallyOpen);
        return interaction;
    }

    private GameObject BuildTornadoResident(Vector3 position)
    {
        GameObject root = new GameObject("Yardım Bekleyen Yaralı Mahalle Sakini");
        root.transform.position = position;
        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.direction = 2;
        collider.center = new Vector3(0f, 0.31f, 0f);
        collider.height = 1.65f;
        collider.radius = 0.30f;

        GameObject civilianPrefab = Resources.Load<GameObject>("PlayerCharacter/Models/CivilianMale");
        if (civilianPrefab != null)
        {
            GameObject model = Instantiate(civilianPrefab, root.transform, false);
            model.name = "Yaralı Sivil Karakter Modeli";
            tornadoResidentModelRoot = model.transform;
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                float scale = bounds.size.y > 0.01f ? 1.62f / bounds.size.y : 1f;
                model.transform.localScale = Vector3.one * scale;
                model.transform.localPosition = new Vector3(0f, -(bounds.min.y - root.transform.position.y) * scale, 0f);
            }
            Animator animator = model.GetComponentInChildren<Animator>(true);
            RuntimeAnimatorController controller = Resources.Load<RuntimeAnimatorController>("PlayerCharacter/PlayerCharacterController");
            if (animator != null)
            {
                if (controller != null) animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                tornadoResidentAnimator = animator;

            }
        }
        else
        {
            Material clothes = MakeMaterial("Yaralı Sivil Kıyafeti", new Color32(69, 102, 119, 255));
            Material skin = MakeMaterial("Yaralı Sivil Ten", new Color32(179, 132, 101, 255));
            CreateTornadoPropPart(root.transform, PrimitiveType.Capsule, "Sivil Gövde", new Vector3(0f, 0.72f, 0f),
                new Vector3(0.42f, 0.48f, 0.3f), clothes);
            CreateTornadoPropPart(root.transform, PrimitiveType.Sphere, "Sivil Baş", new Vector3(0f, 1.25f, 0f),
                new Vector3(0.24f, 0.28f, 0.24f), skin);
        }
        root.SetActive(false);
        return root;
    }

    private void CreateTornadoCable(string name, Vector3 start, Vector3 end, Material material)
    {
        GameObject cable = new GameObject(name);
        LineRenderer line = cable.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 4;
        line.SetPosition(0, start);
        line.SetPosition(1, Vector3.Lerp(start, end, 0.35f) + Vector3.down * 0.6f);
        line.SetPosition(2, Vector3.Lerp(start, end, 0.72f) + Vector3.down * 0.35f);
        line.SetPosition(3, end);
        line.startWidth = 0.055f;
        line.endWidth = 0.075f;
        line.sharedMaterial = material;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        Vector3[] points = { start, line.GetPosition(1), line.GetPosition(2), end };
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 delta = points[i + 1] - points[i];
            GameObject segment = new GameObject("Fiziksel Kablo Bölümü " + (i + 1));
            segment.transform.SetParent(cable.transform, true);
            segment.transform.position = (points[i] + points[i + 1]) * 0.5f;
            segment.transform.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            CapsuleCollider collider = segment.AddComponent<CapsuleCollider>();
            collider.direction = 1;
            collider.radius = 0.075f;
            collider.height = delta.magnitude + 0.15f;
        }
        cable.SetActive(false);
        tornadoDamageProps.Add(cable);
    }

    private GameObject CreateTornadoProp(string name, Vector3 groundPosition, Vector3 colliderSize)
    {
        GameObject root = new GameObject(name);
        root.transform.position = groundPosition;
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.size = colliderSize;
        collider.center = new Vector3(0f, colliderSize.y * 0.5f, 0f);
        return root;
    }

    private GameObject CreateTornadoPropPart(Transform parent, PrimitiveType shape, string name,
        Vector3 localPosition, Vector3 localScale, Material material, Quaternion localRotation = default)
    {
        GameObject part = GameObject.CreatePrimitive(shape);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        float rotationMagnitude = localRotation.x * localRotation.x + localRotation.y * localRotation.y
            + localRotation.z * localRotation.z + localRotation.w * localRotation.w;
        part.transform.localRotation = rotationMagnitude < 0.0001f ? Quaternion.identity : localRotation;
        part.transform.localScale = localScale;
        Collider childCollider = part.GetComponent<Collider>();
        if (childCollider != null) childCollider.enabled = false;
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
        return part;
    }

    private void RegisterTornadoLooseObject(Transform item, PrototypeInteractable preparationTask)
    {
        if (item == null) return;
        tornadoLooseObjects.Add(item);
        tornadoLooseObjectHomes.Add(item.position);
        tornadoLooseObjectHomeRotations.Add(item.rotation);
        tornadoLooseObjectVelocities.Add(Vector3.zero);
        tornadoLooseObjectPreparationTasks.Add(preparationTask);
    }

    private void BuildTornadoVfx()
    {
        tornadoVfxRoot = new GameObject("Hortum VFX - Dönen Hacimli Toz ve Moloz Sütunu");
        tornadoVfxRoot.transform.position = new Vector3(0f, 0f, 20f);
        tornadoVfxRoot.SetActive(false);
        Material dust = MakeMaterial("Hortum Toz ve Moloz Parçacığı", Color.white);
        dust.SetOverrideTag("RenderType", "Transparent");
        if (dust.HasProperty("_Surface")) dust.SetFloat("_Surface", 1f);
        if (dust.HasProperty("_SrcBlend")) dust.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (dust.HasProperty("_DstBlend")) dust.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (dust.HasProperty("_ZWrite")) dust.SetInt("_ZWrite", 0);
        dust.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        dust.EnableKeyword("_ALPHABLEND_ON");
        dust.renderQueue = 3000;
        Texture2D softParticle = Application.isPlaying ? CreateTornadoParticleTexture() : Texture2D.whiteTexture;
        if (dust.HasProperty("_BaseMap")) dust.SetTexture("_BaseMap", softParticle);
        if (dust.HasProperty("_MainTex")) dust.SetTexture("_MainTex", softParticle);

        // Six independently noisy orbital bands create a ragged, rotating funnel; no cone mesh is used.
        for (int ring = 0; ring < 6; ring++)
        {
            GameObject ringObject = new GameObject("Dönen Hacimli Toz Bandı " + (ring + 1));
            ringObject.transform.SetParent(tornadoVfxRoot.transform, false);
            ringObject.transform.localPosition = new Vector3(0f, 0.75f + ring * 2.05f, 0f);
            ParticleSystem ps = ringObject.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = true; main.loop = true; main.duration = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 4.2f);
            main.startSpeed = 0f; main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.65f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.28f, 0.31f, 0.32f, 0.32f), new Color(0.55f, 0.52f, 0.46f, 0.72f));
            main.maxParticles = 170; main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emission = ps.emission; emission.rateOverTime = 27f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Circle;
            shape.rotation = new Vector3(90f, 0f, 0f);
            shape.radius = Mathf.Lerp(0.52f, 3.15f, ring / 5f); shape.radiusThickness = 0.08f;
            var vel = ps.velocityOverLifetime; vel.enabled = true;
            // Unity requires x/y/z velocity curves to use the same mode. Keep all three
            // as constants; turbulence and orbital motion provide the natural variation.
            vel.x = new ParticleSystem.MinMaxCurve(0f);
            vel.y = new ParticleSystem.MinMaxCurve(1.5f + ring * 0.12f);
            vel.z = new ParticleSystem.MinMaxCurve(0f);
            vel.orbitalX = new ParticleSystem.MinMaxCurve(0f);
            vel.orbitalY = new ParticleSystem.MinMaxCurve(2.4f + ring * 0.32f);
            vel.orbitalZ = new ParticleSystem.MinMaxCurve(0f);
            var noise = ps.noise; noise.enabled = true; noise.strength = new ParticleSystem.MinMaxCurve(0.55f, 1.15f); noise.frequency = 0.36f; noise.scrollSpeed = 0.8f; noise.damping = true;
            var size = ps.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.35f), new Keyframe(0.38f, 1f), new Keyframe(1f, 0.22f)));
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard; renderer.sharedMaterial = dust;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
        }

        GameObject debrisObject = new GameObject("Yükselen Yaprak ve Hafif Moloz");
        debrisObject.transform.SetParent(tornadoVfxRoot.transform, false);
        debrisObject.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        tornadoDebris = debrisObject.AddComponent<ParticleSystem>();
        var debrisMain = tornadoDebris.main;
        debrisMain.playOnAwake = true; debrisMain.loop = true; debrisMain.duration = 4f;
        debrisMain.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 2.7f);
        debrisMain.startSpeed = 0f; debrisMain.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.48f);
        debrisMain.startColor = new ParticleSystem.MinMaxGradient(new Color(0.18f, 0.19f, 0.17f, 0.82f), new Color(0.4f, 0.34f, 0.27f, 0.9f));
        debrisMain.maxParticles = 300; debrisMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        var debrisEmission = tornadoDebris.emission; debrisEmission.rateOverTime = 38f;
        var debrisShape = tornadoDebris.shape; debrisShape.shapeType = ParticleSystemShapeType.Circle;
        debrisShape.rotation = new Vector3(90f, 0f, 0f); debrisShape.radius = 2.55f; debrisShape.radiusThickness = 0.08f;
        var debrisVelocity = tornadoDebris.velocityOverLifetime; debrisVelocity.enabled = true;
        debrisVelocity.x = new ParticleSystem.MinMaxCurve(0f);
        debrisVelocity.y = new ParticleSystem.MinMaxCurve(2.4f);
        debrisVelocity.z = new ParticleSystem.MinMaxCurve(0f);
        debrisVelocity.orbitalX = new ParticleSystem.MinMaxCurve(0f);
        debrisVelocity.orbitalY = new ParticleSystem.MinMaxCurve(7f);
        debrisVelocity.orbitalZ = new ParticleSystem.MinMaxCurve(0f);
        var debrisNoise = tornadoDebris.noise; debrisNoise.enabled = true; debrisNoise.strength = new ParticleSystem.MinMaxCurve(0.35f, 0.85f); debrisNoise.frequency = 0.65f;
        var debrisRenderer = tornadoDebris.GetComponent<ParticleSystemRenderer>();
        debrisRenderer.renderMode = ParticleSystemRenderMode.Billboard; debrisRenderer.sharedMaterial = dust;
        debrisRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; debrisRenderer.receiveShadows = false;

        GameObject groundDustObject = new GameObject("Yerde Genişleyen Hortum Tozu");
        groundDustObject.transform.SetParent(tornadoVfxRoot.transform, false);
        groundDustObject.transform.localPosition = new Vector3(0f, 0.18f, 0f);
        tornadoFunnel = groundDustObject.AddComponent<ParticleSystem>();
        var dustMain = tornadoFunnel.main; dustMain.playOnAwake = true; dustMain.loop = true; dustMain.startLifetime = new ParticleSystem.MinMaxCurve(2.4f, 4.6f); dustMain.startSpeed = 0f; dustMain.startSize = new ParticleSystem.MinMaxCurve(1.4f, 3.6f); dustMain.startColor = new Color(0.40f, 0.38f, 0.34f, 0.52f); dustMain.maxParticles = 160;
        var dustEmit = tornadoFunnel.emission; dustEmit.rateOverTime = 20f;
        var dustShape = tornadoFunnel.shape; dustShape.shapeType = ParticleSystemShapeType.Circle; dustShape.rotation = new Vector3(90f, 0f, 0f); dustShape.radius = 2.35f; dustShape.radiusThickness = 0.06f;
        var dustVel = tornadoFunnel.velocityOverLifetime; dustVel.enabled = true;
        dustVel.x = new ParticleSystem.MinMaxCurve(0f);
        dustVel.y = new ParticleSystem.MinMaxCurve(0.65f);
        dustVel.z = new ParticleSystem.MinMaxCurve(0f);
        dustVel.orbitalX = new ParticleSystem.MinMaxCurve(0f);
        dustVel.orbitalY = new ParticleSystem.MinMaxCurve(4.4f);
        dustVel.orbitalZ = new ParticleSystem.MinMaxCurve(0f);
        var dustRenderer = tornadoFunnel.GetComponent<ParticleSystemRenderer>(); dustRenderer.sharedMaterial = dust;
        tornadoCoreLight = tornadoVfxRoot.AddComponent<Light>(); tornadoCoreLight.type = LightType.Point; tornadoCoreLight.color = new Color(0.56f, 0.51f, 0.43f); tornadoCoreLight.range = 7f; tornadoCoreLight.intensity = 0.6f; tornadoCoreLight.shadows = LightShadows.None;
    }

    private Texture2D CreateTornadoParticleTexture()
    {
        tornadoParticleTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);
        tornadoParticleTexture.name = "Yumuşak Hortum Toz Parçacığı";
        tornadoParticleTexture.wrapMode = TextureWrapMode.Clamp;
        tornadoParticleTexture.filterMode = FilterMode.Bilinear;
        Color32[] pixels = new Color32[64 * 64];
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
        {
            float dx = (x + 0.5f - 32f) / 32f;
            float dy = (y + 0.5f - 32f) / 32f;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 1.8f) * 0.82f;
            byte a = (byte)Mathf.RoundToInt(alpha * 255f);
            pixels[y * 64 + x] = new Color32(255, 255, 255, a);
        }
        tornadoParticleTexture.SetPixels32(pixels);
        tornadoParticleTexture.Apply(false, true);
        return tornadoParticleTexture;
    }

    public void BeginTornadoVisual()
    {
        if (tornadoVfxRoot != null) tornadoVfxRoot.SetActive(true);
        RenderSettings.fog = true;
    }

    public void UpdateTornadoVisual(float elapsed, float duration)
    {
        if (tornadoVfxRoot == null) return;
        float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, duration));
        Vector3 routeStart = new Vector3(-27f, 0f, 5.8f);
        Vector3 routeEnd = new Vector3(27f, 0f, 5.8f);
        tornadoVfxRoot.transform.position = Vector3.Lerp(routeStart, routeEnd, progress);
        float darkening = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 5.5f));
        if (tornadoCoreLight != null) tornadoCoreLight.intensity = 0.45f + Mathf.PerlinNoise(Time.time * 1.7f, 3.2f) * 0.7f;
        if (tornadoSun != null) tornadoSun.intensity = Mathf.Lerp(tornadoOriginalSunIntensity, 0.16f, darkening);
        RenderSettings.ambientIntensity = Mathf.Lerp(tornadoOriginalAmbientIntensity, 0.32f, darkening);
        if (tornadoRuntimeSkybox != null && tornadoSkyboxHasExposure)
            tornadoRuntimeSkybox.SetFloat("_Exposure", Mathf.Lerp(tornadoOriginalSkyboxExposure, 0.22f, darkening));
        RenderSettings.fogDensity = Mathf.Lerp(0.0015f, 0.0065f, Mathf.Clamp01(elapsed / 6f));
        RenderSettings.fogColor = Color.Lerp(new Color32(105, 111, 116, 255), new Color32(63, 68, 72, 255), progress);
        for (int i = 0; i < tornadoLooseObjects.Count; i++)
            if (tornadoLooseObjects[i] != null
                && (tornadoLooseObjectPreparationTasks[i] == null || !tornadoLooseObjectPreparationTasks[i].IsComplete))
            {
                Transform item = tornadoLooseObjects[i];
                Vector3 position = item.position;
                Vector3 toVortex = tornadoVfxRoot.transform.position - position;
                toVortex.y = 0f;
                float distance = toVortex.magnitude;
                float influence = 1f - Mathf.InverseLerp(3.5f, 10f, distance);
                Vector3 velocity = tornadoLooseObjectVelocities[i];
                if (influence <= 0f && velocity.sqrMagnitude < 0.0025f)
                    continue;
                if (influence > 0f)
                {
                    Vector3 toward = distance > 0.01f ? toVortex / distance : Vector3.right;
                    Vector3 tangent = new Vector3(-toward.z, 0f, toward.x) * ((i & 1) == 0 ? 1f : -1f);
                    float gust = 0.78f + 0.22f * Mathf.Sin(elapsed * 3.3f + i * 1.37f);
                    velocity += (toward * 3.8f + tangent * 6.2f + Vector3.up * 4.6f) * (influence * gust * Time.deltaTime);
                }
                velocity += Vector3.down * (2.8f * Time.deltaTime);
                velocity *= Mathf.Exp(-0.18f * Time.deltaTime);
                velocity = Vector3.ClampMagnitude(velocity, 8f);
                position += velocity * Time.deltaTime;
                if (position.y < 0.015f)
                {
                    position.y = 0.015f;
                    velocity.y = Mathf.Abs(velocity.y) * 0.22f;
                    velocity.x *= 0.82f;
                    velocity.z *= 0.82f;
                }
                item.position = position;
                item.rotation = item.rotation * Quaternion.Euler(
                    velocity.z * 22f * Time.deltaTime,
                    (14f + influence * 220f) * Time.deltaTime,
                    -velocity.x * 22f * Time.deltaTime);
                tornadoLooseObjectVelocities[i] = velocity;
            }
    }

    public void BeginTornadoDeparture()
    {
        if (tornadoVfxRoot == null) return;
        ParticleSystem[] systems = tornadoVfxRoot.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
            systems[i].Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }

    public void UpdateTornadoDeparture(float elapsed, float duration)
    {
        if (tornadoVfxRoot == null) return;
        float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(0.1f, duration)));
        tornadoVfxRoot.transform.position = Vector3.Lerp(new Vector3(27f, 0f, 5.8f), new Vector3(45f, 0f, 5.8f), progress);
        float stormLook = 1f - progress;
        if (tornadoCoreLight != null)
            tornadoCoreLight.intensity = Mathf.Lerp(0f, 0.45f + Mathf.PerlinNoise(Time.time * 1.7f, 3.2f) * 0.7f, stormLook);
        if (tornadoSun != null)
            tornadoSun.intensity = Mathf.Lerp(tornadoOriginalSunIntensity, 0.16f, stormLook);
        RenderSettings.ambientIntensity = Mathf.Lerp(tornadoOriginalAmbientIntensity, 0.32f, stormLook);
        if (tornadoRuntimeSkybox != null && tornadoSkyboxHasExposure)
            tornadoRuntimeSkybox.SetFloat("_Exposure", Mathf.Lerp(tornadoOriginalSkyboxExposure, 0.22f, stormLook));
        RenderSettings.fogDensity = Mathf.Lerp(tornadoOriginalFogDensity, 0.0065f, stormLook);
        RenderSettings.fogColor = Color.Lerp(tornadoOriginalFogColor, new Color32(63, 68, 72, 255), stormLook);

        // Airborne props keep falling while the funnel moves away; they are not snapped home.
        for (int i = 0; i < tornadoLooseObjects.Count; i++)
        {
            Transform item = tornadoLooseObjects[i];
            if (item == null || tornadoLooseObjectPreparationTasks[i] != null
                && tornadoLooseObjectPreparationTasks[i].IsComplete)
                continue;
            Vector3 velocity = tornadoLooseObjectVelocities[i];
            if (velocity.sqrMagnitude < 0.0025f) continue;
            velocity += Vector3.down * (2.8f * Time.deltaTime);
            velocity *= Mathf.Exp(-0.55f * Time.deltaTime);
            Vector3 position = item.position + velocity * Time.deltaTime;
            if (position.y < 0.015f)
            {
                position.y = 0.015f;
                velocity.y = 0f;
                velocity.x *= 0.76f;
                velocity.z *= 0.76f;
            }
            item.position = position;
            item.rotation = item.rotation * Quaternion.Euler(velocity.z * 12f * Time.deltaTime,
                velocity.magnitude * 14f * Time.deltaTime, -velocity.x * 12f * Time.deltaTime);
            tornadoLooseObjectVelocities[i] = velocity;
        }
    }

    public void EndTornadoVisual()
    {
        if (tornadoVfxRoot != null) tornadoVfxRoot.SetActive(false);
        if (tornadoSun != null) tornadoSun.intensity = tornadoOriginalSunIntensity;
        RenderSettings.ambientIntensity = tornadoOriginalAmbientIntensity;
        RenderSettings.ambientMode = tornadoOriginalAmbientMode;
        RenderSettings.ambientSkyColor = tornadoOriginalAmbientSkyColor;
        RenderSettings.ambientEquatorColor = tornadoOriginalAmbientEquatorColor;
        RenderSettings.ambientGroundColor = tornadoOriginalAmbientGroundColor;
        if (tornadoRuntimeSkybox != null)
        {
            RenderSettings.skybox = tornadoOriginalSkybox;
            Destroy(tornadoRuntimeSkybox);
            tornadoRuntimeSkybox = null;
        }
        RenderSettings.fog = tornadoOriginalFogEnabled;
        RenderSettings.fogMode = tornadoOriginalFogMode;
        RenderSettings.fogDensity = tornadoOriginalFogDensity;
        RenderSettings.fogColor = tornadoOriginalFogColor;
        foreach (GameObject damage in tornadoDamageProps) if (damage != null) damage.SetActive(true);
        if (tornadoResidentAnimator != null)
        {
            if (tornadoResidentPoseRoutine != null) StopCoroutine(tornadoResidentPoseRoutine);
            tornadoResidentPoseRoutine = StartCoroutine(ApplyTornadoResidentPose());
        }
    }

    private IEnumerator ApplyTornadoResidentPose()
    {
        // Stop the mismatched death clip from twisting the civilian's feet away from the
        // body. Lay the intact civilian rig face-up, then ground its whole mesh.
        yield return null;
        if (tornadoResidentAnimator == null)
            yield break;
        tornadoResidentAnimator.applyRootMotion = false;
        tornadoResidentAnimator.speed = 0f;
        tornadoResidentAnimator.enabled = false;
        if (tornadoResidentModelRoot != null)
            tornadoResidentModelRoot.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        GroundTornadoResidentModel();
        tornadoResidentPoseRoutine = null;
    }

    private void GroundTornadoResidentModel()
    {
        if (tornadoResidentModelRoot == null || tornadoResidentAnimator == null)
            return;

        Renderer[] renderers = tornadoResidentModelRoot.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        Bounds bounds = new Bounds(tornadoResidentModelRoot.position, Vector3.zero);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;
            if (!found)
            {
                bounds = renderers[i].bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        if (found)
        {
            float floorY = tornadoResidentAnimator.transform.root.position.y + 0.015f;
            tornadoResidentModelRoot.position += Vector3.up * (floorY - bounds.min.y);
        }
    }

    private void OnDestroy()
    {
        if (tornadoRuntimeSkybox != null)
        {
            RenderSettings.skybox = tornadoOriginalSkybox;
            if (Application.isPlaying) Destroy(tornadoRuntimeSkybox);
            else DestroyImmediate(tornadoRuntimeSkybox);
        }
        if (tornadoParticleTexture != null)
        {
            if (Application.isPlaying) Destroy(tornadoParticleTexture);
            else DestroyImmediate(tornadoParticleTexture);
        }
    }

    private void BuildStormWorld()
    {
        stormTrees.Clear();
        stormTreeBaseRotations.Clear();
        stormClouds.Clear();
        stormCloudBasePositions.Clear();
        stormWaveCrests.Clear();
        stormWaveBasePositions.Clear();
        stormPostEventObjects.Clear();
        stormRainSystems.Clear();
        stormIndoorWaterMesh = null;
        stormIndoorFoamMesh = null;
        stormIndoorWaterObject = null;
        stormIndoorFoamObject = null;
        stormIndoorWaterVertices = null;
        stormIndoorWaterArrival = null;
        stormIndoorWaterFilled = null;
        stormIndoorWaterTriangles.Clear();
        stormIndoorFoamVertices.Clear();
        stormIndoorFoamTriangles.Clear();
        stormIndoorWaterProgress = 0f;
        stormDoorInteraction = null;
        stormShelterPosition = new Vector3(0f, 0f, 3.5f);
        stormPowerPolePivot = null;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color32(111, 126, 137, 255);
        RenderSettings.fogDensity = 0.0065f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color32(116, 136, 153, 255);
        RenderSettings.ambientEquatorColor = new Color32(80, 93, 100, 255);
        RenderSettings.ambientGroundColor = new Color32(37, 47, 49, 255);
        RenderSettings.ambientIntensity = 0.78f;
        Shader proceduralSky = Shader.Find("Skybox/Procedural");
        if (proceduralSky != null)
        {
            Material overcast = new Material(proceduralSky) { name = "Fırtına Kapalı Gökyüzü" };
            if (overcast.HasProperty("_SkyTint")) overcast.SetColor("_SkyTint", new Color(0.57f, 0.64f, 0.69f));
            if (overcast.HasProperty("_GroundColor")) overcast.SetColor("_GroundColor", new Color(0.27f, 0.31f, 0.33f));
            if (overcast.HasProperty("_AtmosphereThickness")) overcast.SetFloat("_AtmosphereThickness", 1.35f);
            RenderSettings.skybox = overcast;
        }
        Light sun = GameObject.Find("Yumuşak Gün Işığı")?.GetComponent<Light>();
        if (sun != null)
        {
            sun.color = new Color(0.63f, 0.72f, 0.79f);
            sun.intensity = 0.48f;
            sun.transform.rotation = Quaternion.Euler(28f, -122f, 0f);
        }

        Material meadow = MakeMaterial("Fırtına Kıyı Çayırı", new Color32(70, 83, 71, 255));
        Material grass = MakeMaterial("Yağmurla Yatmış Otlar", new Color32(84, 99, 79, 255));
        Material asphalt = MakeMaterial("Fırtına Islak Asfalt", new Color32(42, 54, 62, 255));
        Material puddle = MakeMaterial("Yol Üstünde Yağmur Birikintileri", new Color32(52, 72, 79, 255));
        Material concrete = MakeMaterial("Yağmur Islatmış Beton", new Color32(102, 112, 112, 255));
        Material curb = MakeMaterial("Kaldırım Kenar Taşı", new Color32(135, 140, 133, 255));
        Material plaster = MakeMaterial("Sahil Evi Islak Sıva", new Color32(181, 174, 151, 255));
        Material plasterShade = MakeMaterial("Sahil Evi Koyu Sıva", new Color32(135, 137, 126, 255));
        Material stone = MakeMaterial("Sahil Evi Taş Temel", new Color32(88, 99, 98, 255));
        Material roof = MakeMaterial("Fırtına Evi Koyu Kiremit", new Color32(64, 77, 83, 255));
        Material timber = MakeMaterial("Dış Mekân Ceviz Ahşabı", new Color32(91, 67, 51, 255));
        Material blueTrim = MakeMaterial("Koyu Mavi Pencere Doğraması", new Color32(47, 72, 82, 255));
        Material glass = MakeMaterial("Fırtına Pencere Camı", new Color32(76, 111, 125, 255));
        Material shutter = MakeMaterial("Panjur Metal Kaplaması", new Color32(68, 91, 99, 255));
        Material green = MakeMaterial("Sığınak İç Alanı", new Color32(53, 111, 92, 255));
        Material yellow = MakeMaterial("Fırtına Yönlendirme İşareti", new Color32(230, 181, 80, 255));
        Material darkMetal = MakeMaterial("Elektrik Direği ve Metal", new Color32(43, 52, 55, 255));
        Material wire = MakeMaterial("Islak Elektrik Kablosu", new Color32(25, 34, 37, 255));
        Material sea = MakeMaterial("Fırtınalı Marmara Denizi", new Color32(37, 69, 82, 255));
        Material foam = MakeMaterial("Kıyı Köpüğü", new Color32(158, 177, 177, 255));
        Material treeBark = MakeMaterial("Rüzgârda Islak Ağaç Kabuğu", new Color32(69, 58, 47, 255));
        Material foliage = MakeMaterial("Yağmurlu Kıyı Ağacı Yaprakları", new Color32(54, 78, 66, 255));
        Material foliageLight = MakeMaterial("Kıyı Ağacı Yaprak Işıkları", new Color32(79, 103, 83, 255));
        Material emergency = MakeMaterial("Fırtına Acil Çağrı Turuncusu", new Color32(207, 74, 52, 255));
        if (asphalt.HasProperty("_Smoothness")) asphalt.SetFloat("_Smoothness", 0.82f);
        if (asphalt.HasProperty("_Metallic")) asphalt.SetFloat("_Metallic", 0.12f);
        if (puddle.HasProperty("_Smoothness")) puddle.SetFloat("_Smoothness", 0.98f);
        if (puddle.HasProperty("_Metallic")) puddle.SetFloat("_Metallic", 0.24f);
        if (glass.HasProperty("_Smoothness")) glass.SetFloat("_Smoothness", 0.92f);
        if (sea.HasProperty("_Smoothness")) sea.SetFloat("_Smoothness", 0.76f);

        CreateBox("Fırtına Kıyı Mahallesi Zemini", new Vector3(0f, -0.42f, 1f), new Vector3(78f, 0.84f, 60f), meadow);
        CreateBox("Sahil Caddesi Islak Asfalt", new Vector3(0f, 0.015f, -9f), new Vector3(68f, 0.08f, 7.4f), asphalt);
        CreateBox("Sahil Caddesi Güney Kaldırımı", new Vector3(0f, 0.08f, -13.25f), new Vector3(68f, 0.22f, 1.0f), concrete);
        CreateBox("Sahil Caddesi Kuzey Kaldırımı", new Vector3(0f, 0.08f, -4.75f), new Vector3(68f, 0.22f, 1.0f), concrete);
        CreateBox("Kaldırım Bordürü Güney", new Vector3(0f, 0.11f, -12.7f), new Vector3(68f, 0.25f, 0.16f), curb);
        CreateBox("Kaldırım Bordürü Kuzey", new Vector3(0f, 0.11f, -5.28f), new Vector3(68f, 0.25f, 0.16f), curb);
        for (int i = 0; i < 15; i++)
        {
            float x = -31.5f + i * 4.5f;
            CreateBox("Yıpranmış Yol Orta Çizgisi " + i, new Vector3(x, 0.066f, -9f),
                new Vector3(2.5f, 0.018f, 0.12f), yellow);
        }
        for (int i = 0; i < 24; i++)
        {
            float x = -31f + i * 2.7f + Mathf.Sin(i * 2.1f) * 0.55f;
            float z = -9f + Mathf.Sin(i * 1.73f) * 2.7f;
            GameObject puddlePatch = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puddlePatch.name = "Asfaltta Düzensiz Yağmur Birikintisi " + i;
            puddlePatch.transform.position = new Vector3(x, 0.060f, z);
            puddlePatch.transform.localScale = new Vector3(0.55f + (i % 4) * 0.22f, 0.012f, 0.22f + (i % 3) * 0.10f);
            puddlePatch.GetComponent<Renderer>().sharedMaterial = puddle;
            Collider puddleCollider = puddlePatch.GetComponent<Collider>();
            if (puddleCollider != null) puddleCollider.enabled = false;
        }
        for (int i = 0; i < 18; i++)
        {
            float x = -31f + i * 3.7f;
            CreateBox("Yağmur Suyu Drenaj Kanalı " + i, new Vector3(x, 0.195f, -5.42f),
                new Vector3(0.9f, 0.025f, 0.08f), darkMetal);
        }

        GameObject ocean = CreateBox("Fırtınalı Kıyı Denizi", new Vector3(0f, -0.08f, 23f), new Vector3(82f, 0.16f, 17f), sea);
        Collider seaCollider = ocean.GetComponent<Collider>();
        if (seaCollider != null) seaCollider.enabled = false;
        CreateBox("Dalgakıran Beton Kıyı", new Vector3(0f, 0.38f, 14.0f), new Vector3(82f, 0.8f, 1.2f), stone);
        for (int i = 0; i < 22; i++)
        {
            float x = -38f + i * 3.6f;
            GameObject crest = CreateBox("Köpüren Dalga Sırtı " + i,
                new Vector3(x, 0.04f + (i % 3) * 0.018f, 16.2f + Mathf.Sin(i * 1.7f) * 0.75f),
                new Vector3(2.2f + (i % 4) * 0.3f, 0.035f, 0.26f), foam);
            Collider crestCollider = crest.GetComponent<Collider>();
            if (crestCollider != null) crestCollider.enabled = false;
            stormWaveCrests.Add(crest.transform);
            stormWaveBasePositions.Add(crest.transform.position);
        }
        for (int i = 0; i < 20; i++)
        {
            float x = -36f + i * 3.8f;
            GameObject cloud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cloud.name = "Alçak Fırtına Bulutu Kütlesi " + i;
            cloud.transform.position = new Vector3(x, 22f + Mathf.Sin(i * 0.73f) * 2.3f, 4f + Mathf.Cos(i * 1.11f) * 13f);
            cloud.transform.localScale = new Vector3(18f, 5.2f + (i % 3), 14f);
            cloud.GetComponent<Renderer>().sharedMaterial = i % 4 == 0
                ? MakeMaterial("Açık Kurşuni Bulut", new Color32(133, 146, 153, 255))
                : MakeMaterial("Fırtına Kurşuni Bulut", new Color32(87, 103, 114, 255));
            stormClouds.Add(cloud.transform);
            stormCloudBasePositions.Add(cloud.transform.position);
            Collider cloudCollider = cloud.GetComponent<Collider>();
            if (cloudCollider != null) cloudCollider.enabled = false;
        }

        BuildStormHouse(plaster, plasterShade, stone, roof, timber, blueTrim, glass, shutter, green, yellow, emergency, darkMetal);
        BuildStormNeighborhood(grass, plaster, roof, treeBark, foliage, foliageLight);
        BuildStormUtilities(timber, darkMetal, wire, yellow);
        BuildStormWeatherEffects();
        BuildInvisibleMapBoundary("Fırtına", new Vector3(0f, 0f, 1f), 78f, 60f);
    }

    private void BuildInvisibleMapBoundary(string mapName, Vector3 groundCenter, float groundWidth, float groundDepth)
    {
        const float wallThickness = 1f;
        const float wallHeight = 30f;
        float wallCenterY = wallHeight * 0.5f - 0.5f;
        float halfWidth = groundWidth * 0.5f;
        float halfDepth = groundDepth * 0.5f;

        AddInvisibleBoundaryCollider(mapName + " Görünmez Sınır Batı", new Vector3(groundCenter.x - halfWidth, wallCenterY, groundCenter.z),
            new Vector3(wallThickness, wallHeight, groundDepth + wallThickness * 2f));
        AddInvisibleBoundaryCollider(mapName + " Görünmez Sınır Doğu", new Vector3(groundCenter.x + halfWidth, wallCenterY, groundCenter.z),
            new Vector3(wallThickness, wallHeight, groundDepth + wallThickness * 2f));
        AddInvisibleBoundaryCollider(mapName + " Görünmez Sınır Güney", new Vector3(groundCenter.x, wallCenterY, groundCenter.z - halfDepth),
            new Vector3(groundWidth, wallHeight, wallThickness));
        AddInvisibleBoundaryCollider(mapName + " Görünmez Sınır Kuzey", new Vector3(groundCenter.x, wallCenterY, groundCenter.z + halfDepth),
            new Vector3(groundWidth, wallHeight, wallThickness));
    }

    private void AddInvisibleBoundaryCollider(string objectName, Vector3 position, Vector3 size)
    {
        GameObject boundary = new GameObject(objectName);
        boundary.transform.position = position;
        BoxCollider collider = boundary.AddComponent<BoxCollider>();
        collider.size = size;
    }

    private void BuildStormHouse(Material wall, Material shade, Material foundation, Material roof, Material timber,
        Material trim, Material glass, Material shutter, Material safeFloor, Material accent, Material emergency, Material metal)
    {
        const float centerZ = 3f;
        const float halfWidth = 5f;
        const float frontZ = -2f;
        const float backZ = 8f;
        // Split the foundation around the entrance; a solid full-width foundation cube blocked the doorway.
        CreateBox("Sağlam Kıyı Evi Taş Temeli Sol", new Vector3(-3.45f, 0.36f, centerZ), new Vector3(3.1f, 0.72f, 10.5f), foundation);
        CreateBox("Sağlam Kıyı Evi Taş Temeli Sağ", new Vector3(3.45f, 0.36f, centerZ), new Vector3(3.1f, 0.72f, 10.5f), foundation);
        CreateBox("Sağlam Kıyı Evi Arka Temel Bağı", new Vector3(0f, 0.36f, 8.0f), new Vector3(3.8f, 0.72f, 0.5f), foundation);
        CreateBox("Evin İç Döşemesi", new Vector3(0f, 0.76f, centerZ), new Vector3(9.85f, 0.10f, 9.85f), MakeMaterial("Fırtına Evi İç Ahşap Döşeme", new Color32(135, 119, 91, 255)));
        GameObject entryRamp = CreateBox("Kapıya Çıkan Kaymaz Erişim Rampası", new Vector3(0f, 0.37f, -3.55f),
            new Vector3(3.72f, 0.16f, 3.65f), MakeMaterial("Islak Hava Kaymaz Rampa Betonu", new Color32(104, 111, 107, 255)));
        entryRamp.transform.rotation = Quaternion.Euler(-11.8f, 0f, 0f);
        CreateBox("Evin Arka Dış Duvarı", new Vector3(0f, 2.36f, backZ), new Vector3(10f, 3.2f, 0.32f), wall);
        CreateBox("Evin Sol Yan Duvarı", new Vector3(-halfWidth, 2.36f, centerZ), new Vector3(0.32f, 3.2f, 10f), shade);
        CreateBox("Evin Sağ Yan Duvarı", new Vector3(halfWidth, 2.36f, centerZ), new Vector3(0.32f, 3.2f, 10f), wall);
        // Split the side facade around real window openings so the glazing is not a flat prop on a wall.
        float windowBottom = 1.83f;
        float windowTop = 3.13f;
        float windowCenterY = (windowBottom + windowTop) * 0.5f;
        float windowHeight = windowTop - windowBottom;
        for (int side = -1; side <= 1; side += 2)
        {
            float windowX = side * 2.95f;
            float outerStripWidth = 1.125f;
            float innerStripWidth = 0.125f;
            float outerCenterX = side * 4.4375f;
            float innerCenterX = side * 1.9625f;
            CreateBox("Ön Cephe Pencere Dış Yan Duvar " + side,
                new Vector3(outerCenterX, 2.36f, frontZ), new Vector3(outerStripWidth, 3.2f, 0.32f), wall);
            CreateBox("Ön Cephe Pencere Kapı Yanı Duvar " + side,
                new Vector3(innerCenterX, 2.36f, frontZ), new Vector3(innerStripWidth, 3.2f, 0.32f), wall);
            CreateBox("Ön Cephe Pencere Alt Duvarı " + side,
                new Vector3(windowX, (0.76f + windowBottom) * 0.5f, frontZ),
                new Vector3(1.85f, windowBottom - 0.76f, 0.32f), wall);
            CreateBox("Ön Cephe Pencere Üst Duvarı " + side,
                new Vector3(windowX, (windowTop + 3.96f) * 0.5f, frontZ),
                new Vector3(1.85f, 3.96f - windowTop, 0.32f), wall);
        }
        CreateBox("Ön Kapı Yan Duvarı Sol", new Vector3(-1.45f, 2.36f, frontZ), new Vector3(0.9f, 3.2f, 0.32f), wall);
        CreateBox("Ön Kapı Yan Duvarı Sağ", new Vector3(1.45f, 2.36f, frontZ), new Vector3(0.9f, 3.2f, 0.32f), wall);
        CreateBox("Ön Kapı Üstü Hatıl", new Vector3(0f, 3.84f, frontZ), new Vector3(2.0f, 0.24f, 0.32f), wall);
        // Keep the threshold open: the plinth continues on both sides of the doorway only.
        CreateBox("Dış Cephe Su Basman Bandı Sol", new Vector3(-3.0f, 1.0f, frontZ - 0.19f), new Vector3(4.0f, 0.38f, 0.10f), foundation);
        CreateBox("Dış Cephe Su Basman Bandı Sağ", new Vector3(3.0f, 1.0f, frontZ - 0.19f), new Vector3(4.0f, 0.38f, 0.10f), foundation);
        CreateBox("Arka Dış Cephe Su Basman Bandı", new Vector3(0f, 1.0f, backZ + 0.19f), new Vector3(9.95f, 0.38f, 0.10f), foundation);
        CreateBox("Sol Dış Cephe Su Basman Bandı", new Vector3(-halfWidth - 0.19f, 1.0f, centerZ), new Vector3(0.10f, 0.38f, 9.9f), foundation);
        CreateBox("Sağ Dış Cephe Su Basman Bandı", new Vector3(halfWidth + 0.19f, 1.0f, centerZ), new Vector3(0.10f, 0.38f, 9.9f), foundation);

        GameObject leftRoof = CreateBox("Evin Kırma Çatı Sol Eğimi", new Vector3(-2.55f, 4.68f, centerZ), new Vector3(5.75f, 0.30f, 10.65f), roof);
        leftRoof.transform.rotation = Quaternion.Euler(0f, 0f, 16f);
        GameObject rightRoof = CreateBox("Evin Kırma Çatı Sağ Eğimi", new Vector3(2.55f, 4.68f, centerZ), new Vector3(5.75f, 0.30f, 10.65f), roof);
        rightRoof.transform.rotation = Quaternion.Euler(0f, 0f, -16f);
        CreateBox("Çatı Mahya Kapağı", new Vector3(0f, 5.40f, centerZ), new Vector3(0.42f, 0.22f, 10.9f), trim);
        CreateStormGable("Ön Çatı Üçgen Kapatması", frontZ, wall);
        CreateStormGable("Arka Çatı Üçgen Kapatması", backZ, wall);
        CreateBox("Ön Saçak Oluk", new Vector3(0f, 3.96f, -2.52f), new Vector3(10.2f, 0.14f, 0.16f), metal);
        CreateBox("Arka Saçak Oluk", new Vector3(0f, 3.96f, 8.52f), new Vector3(10.2f, 0.14f, 0.16f), metal);
        CreateBox("Yağmur İniş Borusu", new Vector3(4.79f, 2.0f, -1.72f), new Vector3(0.12f, 3.6f, 0.12f), metal);

        // The passage is about 1.88 m wide and 2.9 m high; the hinged door starts closed.
        CreateBox("Giriş Kapısı Kasa Sol", new Vector3(-1.0f, 2.27f, frontZ), new Vector3(0.12f, 3.0f, 0.16f), trim);
        CreateBox("Giriş Kapısı Kasa Sağ", new Vector3(1.0f, 2.27f, frontZ), new Vector3(0.12f, 3.0f, 0.16f), trim);
        CreateBox("Giriş Kapısı Üst Kasa", new Vector3(0f, 3.77f, frontZ), new Vector3(2.08f, 0.16f, 0.16f), trim);
        GameObject stormDoorPivot = new GameObject("Açılıp Kapanan Fırtına Giriş Kapısı Menteşesi");
        stormDoorPivot.transform.position = new Vector3(-0.935f, 0.81f, frontZ);
        GameObject stormDoorLeaf = CreateBox("Sağlam Ev Ahşap Giriş Kapısı", stormDoorPivot.transform.position + new Vector3(0.935f, 1.44f, 0f),
            new Vector3(1.87f, 2.88f, 0.12f), timber);
        stormDoorLeaf.transform.SetParent(stormDoorPivot.transform, true);
        CreateChildBox(stormDoorPivot.transform, "Fırtına Kapısı Etkileşim Kolu", new Vector3(1.67f, 1.52f, -0.08f),
            new Vector3(0.10f, 0.20f, 0.10f), accent);
        GameObject doorControl = new GameObject("Fırtına Kapısı Etkileşim Alanı");
        stormDoorPivot.transform.SetParent(doorControl.transform, true);
        BoxCollider doorTargetArea = doorControl.AddComponent<BoxCollider>();
        doorTargetArea.isTrigger = true;
        doorTargetArea.center = new Vector3(0f, 2.18f, frontZ);
        doorTargetArea.size = new Vector3(1.88f, 2.8f, 0.42f);
        stormDoorInteraction = doorControl.AddComponent<PrototypeInteractable>();
        stormDoorInteraction.Configure(PrototypeAction.ToggleStormDoor, "E ile kapıyı aç");
        stormDoorInteraction.ConfigureStormDoor(stormDoorPivot.transform, stormDoorLeaf.GetComponent<Collider>(), false);
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 2.95f;
            CreateBox("Ön Pencere Dış Çerçevesi " + side, new Vector3(x, 2.48f, frontZ - 0.19f), new Vector3(1.85f, 1.30f, 0.10f), trim);
            CreateBox("Ön Pencere Camı " + side, new Vector3(x, 2.48f, frontZ - 0.25f), new Vector3(1.56f, 1.03f, 0.045f), glass);
            CreateBox("Pencere Dikey Bölmesi " + side, new Vector3(x, 2.48f, frontZ - 0.29f), new Vector3(0.055f, 1.02f, 0.05f), metal);
            CreateBox("Pencere Yatay Bölmesi " + side, new Vector3(x, 2.48f, frontZ - 0.29f), new Vector3(1.54f, 0.055f, 0.05f), metal);
            CreateBox("Pencere Altlığı " + side, new Vector3(x, 1.77f, frontZ - 0.28f), new Vector3(2.03f, 0.12f, 0.35f), timber);
        }

        // The open roller sits stacked above the glass; the slats descend to cover the window.
        if (shutter.HasProperty("_Metallic")) shutter.SetFloat("_Metallic", 0.58f);
        if (shutter.HasProperty("_Smoothness")) shutter.SetFloat("_Smoothness", 0.68f);
        GameObject shutterAssembly = new GameObject("Fırtına Öncesi Açık Metal Panjur");
        shutterAssembly.transform.position = new Vector3(-2.95f, 2.48f, frontZ - 0.36f);
        CreateBox("Panjur Üst Rulo Muhafazası", new Vector3(-2.95f, 3.20f, frontZ - 0.36f),
            new Vector3(1.68f, 0.16f, 0.18f), metal);
        for (int side = -1; side <= 1; side += 2)
            CreateBox("Panjur Yan Kılavuz Rayı " + side, new Vector3(-2.95f + side * 0.80f, 2.48f, frontZ - 0.36f),
                new Vector3(0.07f, 1.20f, 0.14f), metal);
        Transform[] shutterSlats = new Transform[12];
        Vector3[] closedShutterPositions = new Vector3[12];
        for (int slat = 0; slat < 12; slat++)
        {
            GameObject slatObject = CreateChildBox(shutterAssembly.transform, "Panjur Kilitli Metal Lata " + (slat + 1),
                new Vector3(0f, 0.61f - slat * 0.012f, -0.045f), new Vector3(1.54f, 0.086f, 0.055f), shutter);
            shutterSlats[slat] = slatObject.transform;
            closedShutterPositions[slat] = new Vector3(0f, -0.515f + slat * 0.094f, -0.045f);
        }
        GameObject shutterCrank = CreateBox("Panjur El Kolu", new Vector3(-1.75f, 1.15f, frontZ - 0.42f), new Vector3(0.18f, 0.38f, 0.16f), metal);
        PrototypeInteractable shutterTask = shutterCrank.AddComponent<PrototypeInteractable>();
        shutterTask.Configure(PrototypeAction.CloseStormShutters, "Metal panjuru kapat");
        shutterTask.ConfigureStormShutter(shutterSlats, closedShutterPositions);

        GameObject outdoorFurniture = new GameObject("Sabitleme Bekleyen Balkon Sandalye ve Masa Takımı");
        outdoorFurniture.transform.position = new Vector3(-1.9f, 0f, -4.2f);
        CreateChildBox(outdoorFurniture.transform, "Hafif Balkon Masası", new Vector3(0f, 0.72f, 0f), new Vector3(1.35f, 0.14f, 0.85f), timber);
        for (int side = -1; side <= 1; side += 2)
        {
            CreateChildBox(outdoorFurniture.transform, "Balkon Masası Ayağı " + side, new Vector3(side * 0.48f, 0.36f, 0f), new Vector3(0.09f, 0.72f, 0.09f), metal);
            CreateChildBox(outdoorFurniture.transform, "Hafif Balkon Sandalyesi " + side, new Vector3(side * 1.0f, 0.35f, 0.14f), new Vector3(0.58f, 0.70f, 0.62f), MakeMaterial("Balkon Sandalyesi", new Color32(104, 116, 113, 255)));
            CreateChildBox(outdoorFurniture.transform, "Sandalye Sırtlığı " + side, new Vector3(side * 1.0f, 0.76f, 0.40f), new Vector3(0.58f, 0.66f, 0.10f), timber);
        }
        PrototypeInteractable furnitureTask = outdoorFurniture.AddComponent<PrototypeInteractable>();
        furnitureTask.Configure(PrototypeAction.SecureStormLooseItems, "Hafif balkon mobilyasını duvar dibindeki sabitleme yerine al");
        furnitureTask.ConfigureMove(outdoorFurniture.transform, new Vector3(-3.65f, 0f, -3.45f));
        scenario.AddStormPreparationTask(furnitureTask);
        scenario.AddStormPreparationTask(shutterTask);
        CreateBox("Balkon Mobilyası Sabitleme Halkası", new Vector3(-3.65f, 0.07f, -3.45f), new Vector3(1.6f, 0.04f, 0.12f), accent);

        // A broad, visible inner-house refuge gives the player a fair margin from windows.
        GameObject refugeRug = CreateBox("Pencerelerden Uzak İç Oda Güvenli Alanı", stormShelterPosition + new Vector3(0f, 0.035f, 0f),
            new Vector3(7.5f, 0.05f, 7.5f), safeFloor);
        Collider refugeCollider = refugeRug.GetComponent<Collider>();
        if (refugeCollider != null) refugeCollider.enabled = false;
        CreateBox("İç Oda Alanı İnce Kenar Şeridi", stormShelterPosition + new Vector3(0f, 0.068f, -3.68f), new Vector3(7.55f, 0.025f, 0.045f), accent);
        CreateBox("İç Oda Alanı Kenar Şeridi Sol", stormShelterPosition + new Vector3(-3.68f, 0.068f, 0f), new Vector3(0.045f, 0.025f, 7.55f), accent);

        // An in-world emergency phone lets the player report a downed line after the storm.
        Vector3 phonePosition = new Vector3(-1.55f, 1.18f, 5.35f);
        CreateBox("Fırtına Acil Telefon Sehpası Ayağı", phonePosition + new Vector3(0f, -0.60f, 0.14f), new Vector3(0.16f, 1.08f, 0.16f), timber);
        CreateBox("Fırtına Acil Telefon Sehpası Tabanı", phonePosition + new Vector3(0f, -1.14f, 0.14f), new Vector3(0.64f, 0.12f, 0.44f), timber);
        CreateBox("Fırtına Acil Telefon Duvar Montajı", phonePosition + new Vector3(-0.32f, 0f, 0.14f), new Vector3(0.78f, 0.96f, 0.24f), metal);
        GameObject phone = CreateBox("Fırtına Sonrası 112 Acil Telefonu", phonePosition, new Vector3(0.52f, 0.68f, 0.22f), MakeMaterial("Fırtına Telefonu Gövdesi", new Color32(42, 58, 61, 255)));
        CreateBox("Telefon Ekranı", phonePosition + new Vector3(0f, 0.18f, -0.13f), new Vector3(0.32f, 0.12f, 0.025f), MakeMaterial("Telefon Yeşil Ekran", new Color32(91, 184, 151, 255)));
        CreateBox("Telefon Acil Çağrı Tuşu", phonePosition + new Vector3(0f, -0.12f, -0.14f), new Vector3(0.17f, 0.13f, 0.035f), emergency);
        GameObject handsetPivot = new GameObject("Fırtına Telefonu Ahize Menteşesi");
        handsetPivot.transform.position = phonePosition + new Vector3(0f, 0.52f, -0.16f);
        GameObject handset = CreateBox("Fırtına Telefonu Ahizesi", handsetPivot.transform.position, new Vector3(0.46f, 0.09f, 0.10f), metal);
        handset.transform.SetParent(handsetPivot.transform, false);
        handset.transform.localPosition = Vector3.zero;
        handset.transform.localScale = new Vector3(0.46f, 0.09f, 0.10f);
        PrototypeInteractable call = phone.AddComponent<PrototypeInteractable>();
        call.Configure(PrototypeAction.CallForStormHelp, "112'yi ara; kopmuş hattı ve konumu bildir");
        call.ConfigureFloodPhoneVisual(handsetPivot.transform);
        foreach (Collider visual in new[] { handset.GetComponent<Collider>() })
            if (visual != null) visual.enabled = false;
        scenario.SetStormAfterTask(call);

        // Interior furniture makes the refuge read as a lived-in sturdy room, but stays clear of the route.
        CreateBox("İç Oda Masası", new Vector3(1.85f, 0.80f, 6.8f), new Vector3(2.0f, 0.12f, 0.9f), timber);
        for (int side = -1; side <= 1; side += 2)
            for (int depth = -1; depth <= 1; depth += 2)
                CreateBox("İç Oda Masası Ayağı " + side + " " + depth,
                    new Vector3(1.85f + side * 0.82f, 0.42f, 6.8f + depth * 0.33f), new Vector3(0.12f, 0.72f, 0.12f), timber);
        CreateBox("İç Oda Duvar Rafı", new Vector3(2.7f, 2.25f, 7.62f), new Vector3(2.1f, 0.12f, 0.42f), timber);
        CreateBox("İç Oda Battaniye ve Su Kutusu", new Vector3(2.65f, 0.48f, 6.72f), new Vector3(0.54f, 0.46f, 0.48f), MakeMaterial("Acil Durum Malzemesi", new Color32(148, 85, 48, 255)));
    }

    private void CreateStormGable(string objectName, float z, Material material)
    {
        const float halfWidth = 5f;
        const float eaveY = 3.96f;
        const float ridgeY = 5.34f;
        const float halfDepth = 0.16f;
        Mesh mesh = new Mesh { name = objectName + " Mesh" };
        mesh.vertices = new[]
        {
            new Vector3(-halfWidth, eaveY, -halfDepth), new Vector3(0f, ridgeY, -halfDepth), new Vector3(halfWidth, eaveY, -halfDepth),
            new Vector3(-halfWidth, eaveY, halfDepth), new Vector3(0f, ridgeY, halfDepth), new Vector3(halfWidth, eaveY, halfDepth)
        };
        mesh.triangles = new[]
        {
            0, 1, 2, 3, 5, 4,
            0, 3, 4, 0, 4, 1,
            1, 4, 5, 1, 5, 2,
            0, 5, 3, 0, 2, 5
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GameObject gable = new GameObject(objectName);
        gable.transform.position = new Vector3(0f, 0f, z);
        gable.AddComponent<MeshFilter>().sharedMesh = mesh;
        gable.AddComponent<MeshRenderer>().sharedMaterial = material;
        gable.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    private void BuildStormNeighborhood(Material grass, Material wall, Material roof, Material bark, Material leaves, Material leavesLight)
    {
        for (int i = 0; i < 26; i++)
        {
            int row = i / 13;
            int column = i % 13;
            float x = -29f + column * 4.8f + Mathf.Sin(i * 1.71f) * 0.7f;
            float z = row == 0 ? -22f + Mathf.Sin(i * 0.83f) * 2f : 11.5f + Mathf.Cos(i * 1.3f) * 2.5f;
            if (Mathf.Abs(x) < 8.5f && z > -16f && z < 9f)
                continue;
            if (row == 1 && z > 9f && i % 4 == 0)
                continue;
            BuildStormTree(new Vector3(x, 0f, z), i, bark, i % 3 == 0 ? leavesLight : leaves);
        }

        // Quiet coastal homes and stone walls establish a real street around the playable shelter.
        for (int i = 0; i < 7; i++)
        {
            float x = -28f + i * 9.2f;
            if (Mathf.Abs(x) < 9f) continue;
            float z = -0.5f + Mathf.Sin(i * 1.7f) * 1.5f;
            CreateBox("Sahil Sokağı Ev Temeli " + i, new Vector3(x, 0.22f, z), new Vector3(7.1f, 0.44f, 6.6f), MakeMaterial("Komşu Ev Taş Temeli", new Color32(92, 100, 98, 255)));
            CreateBox("Komşu Ev Gövdesi " + i, new Vector3(x, 1.9f, z), new Vector3(6.8f, 3.0f, 6.3f), i % 2 == 0 ? wall : MakeMaterial("Komşu Ev Soluk Adaçayı Sıvası", new Color32(151, 164, 151, 255)));
            GameObject roofPanel = CreateBox("Komşu Ev Çatı Paneli " + i, new Vector3(x, 3.75f, z), new Vector3(7.8f, 0.42f, 7.0f), roof);
            roofPanel.transform.rotation = Quaternion.Euler(0f, i % 2 == 0 ? 0f : 4f, i % 2 == 0 ? -8f : 8f);
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject window = CreateBox("Komşu Ev Penceresi " + i + " " + side,
                    new Vector3(x + side * 1.85f, 2.05f, z - 3.18f), new Vector3(1.08f, 1.1f, 0.045f), MakeMaterial("Uzak Ev Pencere Camı", new Color32(73, 105, 115, 255)));
                Collider windowCollider = window.GetComponent<Collider>();
                if (windowCollider != null) windowCollider.enabled = false;
            }
        }

        for (int i = 0; i < 36; i++)
        {
            float x = -35f + i * 2.0f;
            GameObject shrub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shrub.name = "Rüzgârda Yere Yakın Kıyı Çalısı " + i;
            shrub.transform.position = new Vector3(x, 0.28f + (i % 3) * 0.08f, -15.5f + Mathf.Sin(i * 1.41f) * 1.5f);
            shrub.transform.localScale = new Vector3(1.2f + (i % 4) * 0.16f, 0.45f, 0.72f);
            shrub.GetComponent<Renderer>().sharedMaterial = i % 2 == 0 ? grass : leaves;
            Collider c = shrub.GetComponent<Collider>();
            if (c != null) c.enabled = false;
        }
    }

    private void BuildStormTree(Vector3 position, int index, Material bark, Material foliage)
    {
        GameObject tree = new GameObject("Fırtınada Eğilen Kıyı Ağacı " + index);
        tree.transform.position = position;
        float lean = Mathf.Sin(index * 1.87f) * 5f;
        tree.transform.rotation = Quaternion.Euler(lean * 0.55f, index * 37f, lean);
        stormTrees.Add(tree.transform);
        stormTreeBaseRotations.Add(tree.transform.rotation);

        float height = 5.2f + (index % 4) * 0.6f;
        CapsuleCollider trunkHitbox = tree.AddComponent<CapsuleCollider>();
        trunkHitbox.center = new Vector3(0f, height * 0.46f, 0f);
        trunkHitbox.radius = 0.22f;
        trunkHitbox.height = height * 0.92f;

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Islak Ağaç Gövdesi";
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localPosition = new Vector3(0f, height * 0.46f, 0f);
        trunk.transform.localScale = new Vector3(0.30f, height * 0.48f, 0.30f);
        trunk.GetComponent<Renderer>().sharedMaterial = bark;
        Collider trunkCollider = trunk.GetComponent<Collider>();
        if (trunkCollider != null) trunkCollider.enabled = false;
        for (int cluster = 0; cluster < 5; cluster++)
        {
            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "Rüzgârla Sıkışan Yaprak Kümesi";
            crown.transform.SetParent(tree.transform, false);
            crown.transform.localPosition = new Vector3(Mathf.Sin(cluster * 4.3f + index) * 0.62f,
                height * 0.72f + Mathf.Cos(cluster * 2.7f) * 0.45f, Mathf.Cos(cluster * 3.7f + index) * 0.62f);
            crown.transform.localScale = new Vector3(1.65f, 1.28f, 1.5f);
            crown.GetComponent<Renderer>().sharedMaterial = foliage;
            Collider crownCollider = crown.GetComponent<Collider>();
            if (crownCollider != null) crownCollider.enabled = false;
        }
        for (int branch = 0; branch < 3; branch++)
        {
            GameObject limb = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            limb.name = "Ağacın Kısa Yan Dalı";
            limb.transform.SetParent(tree.transform, false);
            limb.transform.localPosition = new Vector3((branch - 1) * 0.33f, height * (0.55f + branch * 0.055f), Mathf.Sin(branch * 3f) * 0.16f);
            limb.transform.localRotation = Quaternion.Euler(0f, 0f, (branch - 1) * 25f + 48f);
            limb.transform.localScale = new Vector3(0.12f, 1.05f, 0.12f);
            limb.GetComponent<Renderer>().sharedMaterial = bark;
            Collider branchCollider = limb.GetComponent<Collider>();
            if (branchCollider != null) branchCollider.enabled = false;
        }
    }

    private void BuildStormUtilities(Material timber, Material metal, Material wire, Material caution)
    {
        float[] streetLightPositions = { -27f, -13.5f, 13.5f, 27f };
        for (int i = 0; i < streetLightPositions.Length; i++)
        {
            float x = streetLightPositions[i];
            GameObject lampPole = CreateBox("Kıyı Caddesi Sokak Lambası Direği " + i,
                new Vector3(x, 3.3f, -5.0f), new Vector3(0.16f, 6.6f, 0.16f), metal);
            CreateBox("Kıyı Caddesi Sokak Lambası Kol " + i,
                new Vector3(x + 0.58f, 6.28f, -5.0f), new Vector3(1.2f, 0.12f, 0.12f), metal);
            CreateBox("Sokak Lambası Koruyucu Başlık " + i,
                new Vector3(x + 1.05f, 6.1f, -5.0f), new Vector3(0.55f, 0.18f, 0.32f), caution);
            Collider poleCollider = lampPole.GetComponent<Collider>();
            if (poleCollider != null) poleCollider.enabled = false;
        }

        CreateBox("Elektrik Direği Ahşap Ana Gövde", new Vector3(16f, 4.7f, -5.1f), new Vector3(0.28f, 9.4f, 0.28f), timber);
        CreateBox("Direk Üst Travers", new Vector3(16f, 9.15f, -5.1f), new Vector3(3.2f, 0.20f, 0.22f), timber);
        CreateBox("Elektrik İzolatörü Sol", new Vector3(14.9f, 9.35f, -5.1f), new Vector3(0.18f, 0.56f, 0.18f), metal);
        CreateBox("Elektrik İzolatörü Orta", new Vector3(16f, 9.35f, -5.1f), new Vector3(0.18f, 0.56f, 0.18f), metal);
        CreateBox("Elektrik İzolatörü Sağ", new Vector3(17.1f, 9.35f, -5.1f), new Vector3(0.18f, 0.56f, 0.18f), metal);
        CreateBox("Yüksek Gerilim Hattı Sol", new Vector3(3f, 8.9f, -5.1f), new Vector3(23f, 0.055f, 0.055f), wire);
        CreateBox("Yüksek Gerilim Hattı Sağ", new Vector3(24f, 8.9f, -5.1f), new Vector3(16f, 0.055f, 0.055f), wire);
        stormPowerPolePivot = new GameObject("Fırtınada Eğilmiş Elektrik Direği Pivotu").transform;
        stormPowerPolePivot.position = new Vector3(25.5f, 0f, -4.9f);
        GameObject fallenPole = CreateBox("Rüzgârda Eğilen Uzak Elektrik Direği", stormPowerPolePivot.position + new Vector3(0f, 4.2f, 0f),
            new Vector3(0.24f, 8.4f, 0.24f), timber);
        fallenPole.transform.SetParent(stormPowerPolePivot, true);
        GameObject crossbar = CreateBox("Uzak Direk Travers", stormPowerPolePivot.position + new Vector3(0f, 8.4f, 0f),
            new Vector3(2.7f, 0.18f, 0.2f), timber);
        crossbar.transform.SetParent(stormPowerPolePivot, true);
        for (int i = 0; i < 3; i++)
        {
            GameObject line = CreateBox("Elektrik Hattı Kabloları " + i,
                new Vector3(20.8f, 8.7f + i * 0.12f, -4.9f + (i - 1) * 0.35f), new Vector3(12f, 0.045f, 0.045f), wire);
            line.transform.rotation = Quaternion.Euler(0f, i * 2f, 0f);
        }
        GameObject hazard = CreateBox("Yerde Kopmuş Elektrik Hattı Tehlikesi", new Vector3(25f, 0.08f, -3.1f),
            new Vector3(8.5f, 0.05f, 0.05f), wire);
        hazard.transform.rotation = Quaternion.Euler(0f, 19f, 0f);
        stormPostEventObjects.Add(hazard);
        hazard.SetActive(false);
        for (int i = 0; i < 4; i++)
        {
            GameObject marker = CreateBox("Kopmuş Hat Çevresi Uyarı Kazığı " + i,
                new Vector3(22.2f + (i % 2) * 5.2f, 0.48f, -4.2f + (i / 2) * 2.3f), new Vector3(0.14f, 0.9f, 0.14f), caution);
            marker.transform.rotation = Quaternion.Euler(0f, i * 29f, 10f * (i % 2 == 0 ? 1 : -1));
            stormPostEventObjects.Add(marker);
            marker.SetActive(false);
        }
    }

    private void BuildStormWeatherEffects()
    {
        // Rain is emitted around the house, not above its interior. Wind pushes each band
        // away from the walls so drops cannot drift through the open entrance.
        Material rainMaterial = CreateStormParticleMaterial("Fırtına Yağmur Damlası Materyali", new Color(0.75f, 0.86f, 0.93f, 0.68f));
        CreateStormRainEmitter("Ön Sokak Yağmuru", new Vector3(0f, 0f, -12.5f), new Vector3(22f, 0.12f, 15f), 215f, 5f, -3.5f, rainMaterial);
        CreateStormRainEmitter("Sol Kıyı Yağmuru", new Vector3(-21f, 0f, 1f), new Vector3(20f, 0.12f, 43f), 560f, -2.8f, 0.8f, rainMaterial);
        CreateStormRainEmitter("Sağ Kıyı Yağmuru", new Vector3(21f, 0f, 1f), new Vector3(20f, 0.12f, 43f), 560f, 7.5f, 0.8f, rainMaterial);
        CreateStormRainEmitter("Arka Kıyı Yağmuru", new Vector3(0f, 0f, 16f), new Vector3(22f, 0.12f, 12f), 170f, 0f, 3.5f, rainMaterial);

        Material stormWaterMaterial = MakeMaterial("Kapıdan İçeri Akan Fırtına Suyu", new Color32(46, 126, 148, 205));
        ConfigureStormWaterMaterial(stormWaterMaterial);
        Material stormWaterFrontMaterial = MakeMaterial("Fırtına Suyu Akış Sınırı", new Color32(153, 218, 222, 225));
        ConfigureStormWaterMaterial(stormWaterFrontMaterial);
        BuildStormIndoorWater(stormWaterMaterial, stormWaterFrontMaterial);

        GameObject debrisObject = new GameObject("Rüzgârda Sürüklenen Yaprak ve Küçük Dallar");
        stormWindDebris = debrisObject.AddComponent<ParticleSystem>();
        stormWindDebris.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule debrisMain = stormWindDebris.main;
        debrisMain.playOnAwake = false;
        debrisMain.loop = true;
        debrisMain.duration = 7f;
        debrisMain.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 3.2f);
        debrisMain.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
        debrisMain.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);
        debrisMain.startColor = new ParticleSystem.MinMaxGradient(new Color32(117, 103, 71, 235), new Color32(66, 86, 68, 240));
        debrisMain.maxParticles = 380;
        debrisMain.simulationSpace = ParticleSystemSimulationSpace.World;
        debrisMain.gravityModifier = 0.06f;
        debrisMain.startRotation3D = true;
        debrisMain.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        debrisMain.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        debrisMain.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        ParticleSystem.EmissionModule debrisEmission = stormWindDebris.emission;
        debrisEmission.rateOverTime = 48f;
        ParticleSystem.ShapeModule debrisShape = stormWindDebris.shape;
        debrisShape.shapeType = ParticleSystemShapeType.Box;
        debrisShape.scale = new Vector3(58f, 0.12f, 35f);
        debrisShape.position = new Vector3(0f, 2.2f, 0f);
        ParticleSystem.VelocityOverLifetimeModule debrisVelocity = stormWindDebris.velocityOverLifetime;
        debrisVelocity.enabled = true;
        debrisVelocity.x = new ParticleSystem.MinMaxCurve(8f);
        debrisVelocity.y = new ParticleSystem.MinMaxCurve(1.4f);
        debrisVelocity.z = new ParticleSystem.MinMaxCurve(1.1f);
        ParticleSystem.RotationOverLifetimeModule debrisRotation = stormWindDebris.rotationOverLifetime;
        debrisRotation.enabled = true;
        debrisRotation.z = new ParticleSystem.MinMaxCurve(2.2f, 5.5f);
        ParticleSystemRenderer debrisRenderer = stormWindDebris.GetComponent<ParticleSystemRenderer>();
        debrisRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        debrisRenderer.mesh = CreateStormLeafMesh();
        debrisRenderer.sharedMaterial = CreateStormParticleMaterial("Yağmurda Sürüklenen Yaprak Materyali", new Color(0.68f, 0.57f, 0.35f, 0.95f));
        debrisRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        debrisRenderer.receiveShadows = false;
        stormWindDebris.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        GameObject lightningObject = new GameObject("Uzak Şimşek Işığı");
        lightningObject.transform.position = new Vector3(2f, 15f, 11f);
        stormLightning = lightningObject.AddComponent<Light>();
        stormLightning.type = LightType.Directional;
        stormLightning.color = new Color(0.72f, 0.82f, 1f);
        stormLightning.intensity = 0f;
        // Let roofs and walls block the exterior flash from reaching the house interior.
        stormLightning.shadows = LightShadows.Hard;
    }

    private ParticleSystem CreateStormRainEmitter(string objectName, Vector3 center, Vector3 bounds,
        float emissionRate, float windX, float windZ, Material material)
    {
        GameObject rainObject = new GameObject(objectName);
        rainObject.transform.position = center;
        ParticleSystem rain = rainObject.AddComponent<ParticleSystem>();
        rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = rain.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.7f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.075f);
        main.startColor = new Color(0.76f, 0.86f, 0.92f, 0.6f);
        main.maxParticles = Mathf.Max(600, Mathf.CeilToInt(emissionRate * 1.8f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;
        ParticleSystem.EmissionModule emission = rain.emission;
        emission.rateOverTime = emissionRate;
        ParticleSystem.ShapeModule shape = rain.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = bounds;
        shape.position = new Vector3(0f, 18f, 0f);
        ParticleSystem.VelocityOverLifetimeModule velocity = rain.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(windX);
        velocity.y = new ParticleSystem.MinMaxCurve(-20f);
        velocity.z = new ParticleSystem.MinMaxCurve(windZ);
        ParticleSystem.CollisionModule rainCollision = rain.collision;
        rainCollision.enabled = true;
        rainCollision.type = ParticleSystemCollisionType.World;
        rainCollision.quality = ParticleSystemCollisionQuality.Low;
        rainCollision.lifetimeLoss = 1f;
        rainCollision.sendCollisionMessages = false;
        ParticleSystemRenderer renderer = rain.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 5.8f;
        renderer.velocityScale = 0.12f;
        renderer.sharedMaterial = material;
        rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        stormRainSystems.Add(rain);
        return rain;
    }

    private Material CreateStormParticleMaterial(string name, Color tint)
    {
        // Particle shaders found only by name can be stripped from player builds.
        // Reuse the already referenced URP/Lit shader from the storm scene instead.
        Shader shader = null;
        if (materials.TryGetValue("Fırtına Kıyı Çayırı", out Material stormGround) && stormGround != null)
            shader = stormGround.shader;
        if (shader == null)
            return MakeMaterial(name, tint);
        Material material = new Material(shader) { name = name, color = tint };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
        if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
        material.SetOverrideTag("RenderType", "Transparent");
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
        return material;
    }

    private void ConfigureStormWaterMaterial(Material material)
    {
        if (material == null) return;
        material.SetOverrideTag("RenderType", "Transparent");
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.92f);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
    }

    private void BuildStormIndoorWater(Material waterMaterial, Material foamMaterial)
    {
        const float minX = -4.75f;
        const float minZ = -1.75f;
        const float cellSize = 0.25f;
        stormIndoorWaterCellsX = 38;
        stormIndoorWaterCellsZ = 38;
        int verticesX = stormIndoorWaterCellsX + 1;
        int verticesZ = stormIndoorWaterCellsZ + 1;
        stormIndoorWaterVertices = new Vector3[verticesX * verticesZ];
        stormIndoorWaterArrival = new float[stormIndoorWaterCellsX * stormIndoorWaterCellsZ];
        stormIndoorWaterFilled = new bool[stormIndoorWaterArrival.Length];

        for (int z = 0; z < stormIndoorWaterCellsZ; z++)
        {
            for (int x = 0; x < stormIndoorWaterCellsX; x++)
            {
                float worldX = minX + (x + 0.5f) * cellSize;
                float worldZ = minZ + (z + 0.5f) * cellSize;
                float distance = Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(0f, minZ));
                float basinVariation = (Mathf.PerlinNoise((worldX + 13f) * 0.42f, (worldZ + 29f) * 0.39f) - 0.5f) * 0.78f;
                float flowingVariation = Mathf.Sin(worldX * 1.12f + worldZ * 0.57f) * 0.18f
                    + Mathf.Sin(worldZ * 1.48f - worldX * 0.81f) * 0.14f;
                stormIndoorWaterArrival[z * stormIndoorWaterCellsX + x] =
                    Mathf.Max(0f, (distance - 0.35f + basinVariation + flowingVariation) / 2.35f);
            }
        }

        stormIndoorWaterObject = new GameObject("Evin Zeminine Yayılan Fırtına Suyu");
        MeshFilter waterFilter = stormIndoorWaterObject.AddComponent<MeshFilter>();
        MeshRenderer waterRenderer = stormIndoorWaterObject.AddComponent<MeshRenderer>();
        stormIndoorWaterMesh = new Mesh { name = "Düzensiz Dalgalı İç Mekân Su Yüzeyi" };
        stormIndoorWaterMesh.MarkDynamic();
        waterFilter.sharedMesh = stormIndoorWaterMesh;
        waterRenderer.sharedMaterial = waterMaterial;
        waterRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        waterRenderer.receiveShadows = false;
        stormIndoorWaterObject.SetActive(false);

        stormIndoorFoamObject = new GameObject("Su Akışının Kesikli Köpüklü Kenarları");
        MeshFilter foamFilter = stormIndoorFoamObject.AddComponent<MeshFilter>();
        MeshRenderer foamRenderer = stormIndoorFoamObject.AddComponent<MeshRenderer>();
        stormIndoorFoamMesh = new Mesh { name = "Dağınık İnce Su Köpüğü" };
        stormIndoorFoamMesh.MarkDynamic();
        foamFilter.sharedMesh = stormIndoorFoamMesh;
        foamRenderer.sharedMaterial = foamMaterial;
        foamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        foamRenderer.receiveShadows = false;
        stormIndoorFoamObject.SetActive(false);
    }

    private void AddStormFoamQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int first = stormIndoorFoamVertices.Count;
        stormIndoorFoamVertices.Add(a);
        stormIndoorFoamVertices.Add(b);
        stormIndoorFoamVertices.Add(c);
        stormIndoorFoamVertices.Add(d);
        stormIndoorFoamTriangles.Add(first);
        stormIndoorFoamTriangles.Add(first + 1);
        stormIndoorFoamTriangles.Add(first + 2);
        stormIndoorFoamTriangles.Add(first + 2);
        stormIndoorFoamTriangles.Add(first + 1);
        stormIndoorFoamTriangles.Add(first + 3);
    }

    private void UpdateStormIndoorWater(bool exposed, float deltaTime)
    {
        if (stormIndoorWaterMesh == null || stormIndoorWaterVertices == null)
            return;

        stormIndoorWaterProgress = Mathf.MoveTowards(stormIndoorWaterProgress, exposed ? 5.7f : 0f,
            deltaTime * (exposed ? 1.45f : 0.72f));
        if (!exposed && stormIndoorWaterProgress <= 0.001f)
        {
            if (stormIndoorWaterObject.activeSelf) stormIndoorWaterObject.SetActive(false);
            if (stormIndoorFoamObject.activeSelf) stormIndoorFoamObject.SetActive(false);
            return;
        }

        const float minX = -4.75f;
        const float minZ = -1.75f;
        const float cellSize = 0.25f;
        int verticesX = stormIndoorWaterCellsX + 1;
        float waterLevel = 0.824f + Mathf.Clamp01((stormIndoorWaterProgress - 3.4f) / 2.3f) * 0.085f;
        for (int z = 0; z < verticesX; z++)
        {
            for (int x = 0; x < verticesX; x++)
            {
                float worldX = minX + x * cellSize;
                float worldZ = minZ + z * cellSize;
                float ripple = Mathf.Sin(Time.time * 2.2f + worldX * 1.35f + worldZ * 0.82f) * 0.006f
                    + Mathf.Sin(Time.time * 1.45f - worldX * 0.67f + worldZ * 1.14f) * 0.004f;
                stormIndoorWaterVertices[z * verticesX + x] = new Vector3(worldX, waterLevel + ripple, worldZ);
            }
        }

        stormIndoorWaterTriangles.Clear();
        stormIndoorFoamVertices.Clear();
        stormIndoorFoamTriangles.Clear();
        for (int cell = 0; cell < stormIndoorWaterArrival.Length; cell++)
            stormIndoorWaterFilled[cell] = stormIndoorWaterProgress > 0.03f
                && stormIndoorWaterArrival[cell] <= stormIndoorWaterProgress;

        int totalWetCells = 0;
        for (int z = 0; z < stormIndoorWaterCellsZ; z++)
        {
            for (int x = 0; x < stormIndoorWaterCellsX; x++)
            {
                int cell = z * stormIndoorWaterCellsX + x;
                if (!stormIndoorWaterFilled[cell]) continue;

                totalWetCells++;
                int lowerLeft = z * verticesX + x;
                int upperLeft = (z + 1) * verticesX + x;
                int lowerRight = lowerLeft + 1;
                int upperRight = upperLeft + 1;
                stormIndoorWaterTriangles.Add(lowerLeft);
                stormIndoorWaterTriangles.Add(upperLeft);
                stormIndoorWaterTriangles.Add(lowerRight);
                stormIndoorWaterTriangles.Add(lowerRight);
                stormIndoorWaterTriangles.Add(upperLeft);
                stormIndoorWaterTriangles.Add(upperRight);

                float x0 = minX + x * cellSize;
                float x1 = x0 + cellSize;
                float z0 = minZ + z * cellSize;
                float z1 = z0 + cellSize;
                float foamY = waterLevel + 0.012f;
                const float foamWidth = 0.052f;
                bool skipFoam = (x * 17 + z * 31) % 4 == 0;
                if (!skipFoam && (z == 0 || !stormIndoorWaterFilled[cell - stormIndoorWaterCellsX]))
                    AddStormFoamQuad(new Vector3(x0, foamY, z0), new Vector3(x0, foamY, z0 + foamWidth),
                        new Vector3(x1, foamY, z0), new Vector3(x1, foamY, z0 + foamWidth));
                if (!skipFoam && (z == stormIndoorWaterCellsZ - 1 || !stormIndoorWaterFilled[cell + stormIndoorWaterCellsX]))
                    AddStormFoamQuad(new Vector3(x0, foamY, z1 - foamWidth), new Vector3(x0, foamY, z1),
                        new Vector3(x1, foamY, z1 - foamWidth), new Vector3(x1, foamY, z1));
                if (!skipFoam && (x == 0 || !stormIndoorWaterFilled[cell - 1]))
                    AddStormFoamQuad(new Vector3(x0, foamY, z0), new Vector3(x0, foamY, z1),
                        new Vector3(x0 + foamWidth, foamY, z0), new Vector3(x0 + foamWidth, foamY, z1));
                if (!skipFoam && (x == stormIndoorWaterCellsX - 1 || !stormIndoorWaterFilled[cell + 1]))
                    AddStormFoamQuad(new Vector3(x1 - foamWidth, foamY, z0), new Vector3(x1 - foamWidth, foamY, z1),
                        new Vector3(x1, foamY, z0), new Vector3(x1, foamY, z1));
            }
        }

        bool visible = totalWetCells > 0;
        if (stormIndoorWaterObject.activeSelf != visible)
            stormIndoorWaterObject.SetActive(visible);
        if (stormIndoorFoamObject.activeSelf != visible)
            stormIndoorFoamObject.SetActive(visible);
        // Foam topology changes with the moving shoreline; clear old triangle indices
        // before assigning this frame's shorter or longer vertex list.
        stormIndoorWaterMesh.Clear(false);
        stormIndoorFoamMesh.Clear(false);
        stormIndoorWaterMesh.vertices = stormIndoorWaterVertices;
        stormIndoorWaterMesh.SetTriangles(stormIndoorWaterTriangles, 0, false);
        stormIndoorWaterMesh.RecalculateNormals();
        stormIndoorWaterMesh.RecalculateBounds();
        stormIndoorFoamMesh.SetVertices(stormIndoorFoamVertices);
        stormIndoorFoamMesh.SetTriangles(stormIndoorFoamTriangles, 0, false);
        stormIndoorFoamMesh.RecalculateNormals();
        stormIndoorFoamMesh.RecalculateBounds();
    }

    private Mesh CreateStormLeafMesh()
    {
        Mesh leaf = new Mesh { name = "Sivrilen Organik Yaprak Parçacığı" };
        Vector3[] vertices =
        {
            new Vector3(-0.50f, 0f, 0f),
            new Vector3(-0.27f, 0.16f, 0f),
            new Vector3(0f, 0.23f, 0f),
            new Vector3(0.28f, 0.14f, 0f),
            new Vector3(0.50f, 0f, 0f),
            new Vector3(0.25f, -0.14f, 0f),
            new Vector3(0f, -0.22f, 0f),
            new Vector3(-0.27f, -0.15f, 0f)
        };
        int[] front = { 0, 1, 7, 1, 2, 7, 2, 6, 7, 2, 3, 6, 3, 5, 6, 3, 4, 5 };
        int[] triangles = new int[front.Length * 2];
        for (int i = 0; i < front.Length; i += 3)
        {
            triangles[i] = front[i];
            triangles[i + 1] = front[i + 1];
            triangles[i + 2] = front[i + 2];
            int back = front.Length + i;
            triangles[back] = front[i + 2];
            triangles[back + 1] = front[i + 1];
            triangles[back + 2] = front[i];
        }
        leaf.vertices = vertices;
        leaf.triangles = triangles;
        leaf.RecalculateNormals();
        leaf.RecalculateBounds();
        return leaf;
    }

    private void BuildWildfireTree(Vector3 position, int variation, Material bark, Material needles, float height)
    {
        wildfireTreeCanopies.Add(position + Vector3.up * (height * 0.68f));
        GameObject tree = new GameObject("Çam Ağacı - Orman Yangını Alanı " + variation);
        tree.transform.position = position;
        tree.transform.rotation = Quaternion.Euler(0f, variation * 41f, 0f);
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Çam Gövdesi";
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localPosition = new Vector3(0f, height * 0.42f, 0f);
        trunk.transform.localScale = new Vector3(0.25f, height * 0.45f, 0.25f);
        trunk.GetComponent<Renderer>().sharedMaterial = bark;
        for (int tier = 0; tier < 4; tier++)
        {
            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crown.name = "Katmanlı Çam Dalları " + tier;
            crown.transform.SetParent(tree.transform, false);
            crown.transform.localPosition = new Vector3(0f, height * (0.43f + tier * 0.145f), 0f);
            float width = 2.0f - tier * 0.34f + (variation % 3) * 0.08f;
            crown.transform.localScale = new Vector3(width, height * 0.17f, width);
            crown.GetComponent<Renderer>().sharedMaterial = needles;
            Collider crownCollider = crown.GetComponent<Collider>();
            if (crownCollider != null) crownCollider.enabled = false;

            // Compound foliage colliders keep the character from walking through the visible crown.
            SphereCollider foliageCollider = tree.AddComponent<SphereCollider>();
            foliageCollider.center = new Vector3(0f, height * (0.43f + tier * 0.145f), 0f);
            foliageCollider.radius = width * 0.48f;
        }
        Rigidbody body = tree.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        CapsuleCollider collision = tree.AddComponent<CapsuleCollider>();
        collision.center = new Vector3(0f, height * 0.45f, 0f);
        collision.height = height * 0.9f;
        collision.radius = 0.16f;
    }

    private void BuildWildfireFront(Material forest, Material grass, Material flameMaterial)
    {
        // Particle flames and sparks form a broad, irregular front without primitive flame-shaped colliders.
        for (int layer = 0; layer < 4; layer++)
        {
            GameObject pulse = new GameObject("Orman Yangını Alev Cephesi Dalgası " + (layer + 1));
            pulse.transform.position = new Vector3(0f, 0f, -13f + layer * 4.4f);
            pulse.SetActive(false);
            wildfireFlameLayers.Add(pulse);
            for (int lightIndex = 0; lightIndex < 3; lightIndex++)
            {
                GameObject glowObject = new GameObject("Yangın Parıltısı " + lightIndex);
                glowObject.transform.SetParent(pulse.transform, false);
                glowObject.transform.localPosition = new Vector3(-9f + lightIndex * 8f, 1.3f, 0f);
                Light glow = glowObject.AddComponent<Light>();
                glow.type = LightType.Point;
                glow.color = new Color(1f, 0.22f, 0.045f);
                glow.intensity = 2.5f;
                glow.range = 9f;
                glow.shadows = LightShadows.None;
                wildfireFlameLights.Add(glow);
            }

            GameObject flameObject = new GameObject("Havada Dalgalanan Alev Parçacıkları");
            flameObject.transform.SetParent(pulse.transform, false);
            ParticleSystem flames = flameObject.AddComponent<ParticleSystem>();
            var flameMain = flames.main;
            flameMain.playOnAwake = true;
            flameMain.loop = true;
            flameMain.duration = 5f;
            flameMain.startLifetime = new ParticleSystem.MinMaxCurve(1.05f, 1.9f);
            flameMain.startSpeed = 0f;
            flameMain.startSize3D = true;
            flameMain.startSizeX = new ParticleSystem.MinMaxCurve(0.42f, 0.84f);
            flameMain.startSizeY = new ParticleSystem.MinMaxCurve(1.15f, 2.35f);
            flameMain.startSizeZ = new ParticleSystem.MinMaxCurve(0.16f, 0.34f);
            flameMain.startColor = Color.white;
            flameMain.gravityModifier = 0f;
            flameMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            flameMain.maxParticles = 420;
            var flameEmission = flames.emission;
            flameEmission.rateOverTime = 64f;
            var flameShape = flames.shape;
            flameShape.shapeType = ParticleSystemShapeType.Box;
            flameShape.scale = new Vector3(23f, 0.24f, 1.15f);
            flameShape.position = new Vector3(-3.5f, 0.26f, 0f);
            var flameVelocity = flames.velocityOverLifetime;
            flameVelocity.enabled = true;
            flameVelocity.space = ParticleSystemSimulationSpace.Local;
            flameVelocity.x = new ParticleSystem.MinMaxCurve(-0.32f, 0.32f);
            flameVelocity.y = new ParticleSystem.MinMaxCurve(1.45f, 2.6f);
            flameVelocity.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
            var flameSize = flames.sizeOverLifetime;
            flameSize.enabled = true;
            flameSize.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 0.28f), new Keyframe(0.22f, 1f),
                    new Keyframe(0.62f, 0.74f), new Keyframe(1f, 0.04f)));
            var flameColor = flames.colorOverLifetime;
            flameColor.enabled = true;
            Gradient flameFade = new Gradient();
            flameFade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.79f, 0.43f), 0.55f),
                    new GradientColorKey(new Color(0.48f, 0.12f, 0.035f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.98f, 0.12f),
                    new GradientAlphaKey(0.72f, 0.7f), new GradientAlphaKey(0f, 1f) });
            flameColor.color = new ParticleSystem.MinMaxGradient(flameFade);
            var flameNoise = flames.noise;
            flameNoise.enabled = true;
            flameNoise.strength = new ParticleSystem.MinMaxCurve(0.18f, 0.46f);
            flameNoise.frequency = 0.72f;
            flameNoise.scrollSpeed = 0.8f;
            flameNoise.damping = true;
            ParticleSystemRenderer flameRenderer = flames.GetComponent<ParticleSystemRenderer>();
            flameRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            flameRenderer.sharedMaterial = flameMaterial;
            flameRenderer.sortMode = ParticleSystemSortMode.Distance;
            flameRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            flameRenderer.receiveShadows = false;

            GameObject sparksObject = new GameObject("Yukarı Savrulan Korlar");
            sparksObject.transform.SetParent(pulse.transform, false);
            ParticleSystem sparks = sparksObject.AddComponent<ParticleSystem>();
            var sparkMain = sparks.main;
            sparkMain.playOnAwake = true;
            sparkMain.loop = true;
            sparkMain.duration = 4f;
            sparkMain.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, 1.65f);
            sparkMain.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 4.3f);
            sparkMain.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.07f);
            sparkMain.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.32f, 0.055f), new Color(1f, 0.78f, 0.24f));
            sparkMain.gravityModifier = -0.14f;
            sparkMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            sparkMain.maxParticles = 120;
            var sparkEmission = sparks.emission;
            sparkEmission.rateOverTime = 17f;
            var sparkShape = sparks.shape;
            sparkShape.shapeType = ParticleSystemShapeType.Box;
            sparkShape.scale = new Vector3(23f, 0.2f, 1.1f);
            sparkShape.position = new Vector3(-3.5f, 0.3f, 0f);
            var sparkVelocity = sparks.velocityOverLifetime;
            sparkVelocity.enabled = true;
            sparkVelocity.x = new ParticleSystem.MinMaxCurve(-0.9f, 0.9f);
            sparkVelocity.y = new ParticleSystem.MinMaxCurve(1.7f, 4.4f);
            sparkVelocity.z = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);
            var sparkColor = sparks.colorOverLifetime;
            sparkColor.enabled = true;
            Gradient sparkFade = new Gradient();
            sparkFade.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.8f, 0.34f), 0f), new GradientColorKey(new Color(0.85f, 0.18f, 0.035f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.35f), new GradientAlphaKey(0f, 1f) });
            sparkColor.color = new ParticleSystem.MinMaxGradient(sparkFade);
            ParticleSystemRenderer sparkRenderer = sparks.GetComponent<ParticleSystemRenderer>();
            sparkRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            sparkRenderer.sharedMaterial = flameMaterial;
            sparkRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sparkRenderer.receiveShadows = false;

            for (int i = 0; i < 22; i++)
            {
                float x = -15f + i * 1.05f;
                GameObject stump = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stump.name = "Yangın Öncesi Kuru Çalı " + i;
                stump.transform.SetParent(pulse.transform, false);
                stump.transform.localPosition = new Vector3(x, 0.28f + (i % 3) * 0.08f, Mathf.Sin(i * 2.3f) * 0.9f);
                stump.transform.localScale = new Vector3(0.38f, 0.28f, 0.4f);
                stump.GetComponent<Renderer>().sharedMaterial = i % 2 == 0 ? forest : grass;
                Collider collider = stump.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
            }
        }
    }

    private Texture2D CreateWildfireFlameTexture()
    {
        const int width = 128;
        const int height = 192;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true)
        {
            name = "Organik Alev Dili Flipbook Maskesi",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            anisoLevel = 2
        };
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            float center = 0.5f + Mathf.Sin(v * 8.5f + 0.7f) * 0.075f;
            float edgeNoise = Mathf.PerlinNoise(2.6f, v * 9f + 1.8f);
            float halfWidth = Mathf.Max(0.012f, 0.47f * Mathf.Pow(1f - v, 0.68f)
                * Mathf.Lerp(0.8f, 1.08f, edgeNoise));
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                float distance = Mathf.Abs(u - center) / halfWidth;
                float edge = 1f - Mathf.SmoothStep(0.78f, 1.08f, distance);
                float turbulence = Mathf.PerlinNoise(u * 8.5f + 3.2f, v * 15f + 4.7f);
                float alpha = edge * Mathf.Lerp(0.48f, 1f, turbulence);

                float core = Mathf.Clamp01((1f - distance) * 1.7f) * (1f - v * 0.72f);
                Color outer = Color.Lerp(new Color(0.42f, 0.035f, 0.008f), new Color(1f, 0.16f, 0.015f), 1f - v);
                Color inner = Color.Lerp(new Color(1f, 0.32f, 0.025f), new Color(1f, 0.91f, 0.48f), core);
                Color color = Color.Lerp(outer, inner, core * 0.88f);
                pixels[y * width + x] = new Color(color.r, color.g, color.b, alpha);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(true, true);
        return texture;
    }

    private void BuildWildfireSmoke()
    {
        if (wildfireTreeCanopies.Count == 0)
            return;

        Texture2D smokeTexture = CreateWildfireSmokeTexture();
        wildfireSmokeMaterial = CreateWildfireParticleMaterial(smokeTexture);
        if (wildfireSmokeMaterial == null)
        {
            if (smokeTexture != null)
                Destroy(smokeTexture);
            Debug.LogWarning("Duman shader'ı build içinde bulunamadı; yangın dumanı kapatıldı.");
            return;
        }

        UnityEngine.Random.State previousRandomState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(23981);
        const int smokeSources = 12;
        for (int sourceIndex = 0; sourceIndex < smokeSources; sourceIndex++)
        {
            Vector3 canopy = wildfireTreeCanopies[UnityEngine.Random.Range(0, wildfireTreeCanopies.Count)];
            Vector3 offset = new Vector3(UnityEngine.Random.Range(-0.9f, 0.9f),
                UnityEngine.Random.Range(-0.65f, 0.25f), UnityEngine.Random.Range(-0.9f, 0.9f));
            GameObject source = new GameObject("Ağaç Tacında Yükselen Duman Kaynağı " + sourceIndex);
            source.transform.position = canopy + offset;
            source.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-8f, 8f),
                UnityEngine.Random.Range(-25f, 25f), UnityEngine.Random.Range(-8f, 8f));

            ParticleSystem particles = source.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 8f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4.5f, 8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 1.15f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.1f, 2.7f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.20f, 0.21f, 0.20f, 0.7f), new Color(0.42f, 0.41f, 0.38f, 0.86f));
            main.gravityModifier = -0.035f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 72;

            var emission = particles.emission;
            emission.rateOverTime = UnityEngine.Random.Range(1.7f, 3.2f);
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = UnityEngine.Random.Range(14f, 24f);
            shape.radius = UnityEngine.Random.Range(0.18f, 0.38f);
            shape.rotation = new Vector3(-8f, 0f, 0f);

            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.3f, 1f), new Keyframe(1f, 1.75f)));
            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(new Color(0.22f, 0.23f, 0.22f), 0f),
                    new GradientColorKey(new Color(0.34f, 0.33f, 0.31f), 0.48f),
                    new GradientColorKey(new Color(0.49f, 0.48f, 0.45f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.76f, 0.16f),
                    new GradientAlphaKey(0.58f, 0.62f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);
            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.28f, 0.52f);
            noise.frequency = 0.32f;
            noise.scrollSpeed = 0.18f;
            noise.damping = true;

            ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sharedMaterial = wildfireSmokeMaterial;
            particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            particles.Play();
        }
        UnityEngine.Random.state = previousRandomState;
    }

    private Texture2D CreateWildfireSmokeTexture()
    {
        const int resolution = 192;
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true)
        {
            name = "Düzensiz Kenarlı Yumuşak Orman Dumanı",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            anisoLevel = 2
        };
        Color[] pixels = new Color[resolution * resolution];
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float u = x / (float)(resolution - 1);
                float v = y / (float)(resolution - 1);
                float broadNoise = Mathf.PerlinNoise(u * 4.2f + 13.7f, v * 4.2f + 8.3f);
                float detailNoise = Mathf.PerlinNoise(u * 10.5f + 4.1f, v * 10.5f + 23.6f);
                float fineNoise = Mathf.PerlinNoise(u * 22f + 39.2f, v * 22f + 2.8f);
                float noise = broadNoise * 0.55f + detailNoise * 0.30f + fineNoise * 0.15f;
                float dx = (u - 0.5f) * 1.75f;
                float dy = (v - 0.49f) * 1.9f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float irregularEdge = 0.48f + noise * 0.30f;
                float edgeProgress = Mathf.InverseLerp(irregularEdge * 0.52f, irregularEdge, distance);
                float cloud = 1f - Mathf.SmoothStep(0f, 1f, edgeProgress);
                float alpha = cloud * Mathf.Lerp(0.35f, 0.88f, noise);
                pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(true, true);
        return texture;
    }

    private Material CreateWildfireParticleMaterial(Texture2D texture, string materialName = "Duman - Yumuşak Gürültülü Hacim Katmanları")
    {
        // Match the forest's referenced shader so the generated smoke material survives builds.
        Shader shader = null;
        if (materials.TryGetValue("Yangın Tatbikatı Orman Zemini", out Material forestMaterial))
            shader = forestMaterial != null ? forestMaterial.shader : null;
        if (shader == null)
            return null;

        Material material = new Material(shader) { name = materialName };
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        material.SetOverrideTag("RenderType", "Transparent");
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_SrcBlendAlpha")) material.SetInt("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
        if (material.HasProperty("_DstBlendAlpha")) material.SetInt("_DstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
        if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 0f);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.DisableKeyword("_ALPHAMODULATE_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
        return material;
    }

    public void BeginStormVisual()
    {
        for (int i = 0; i < stormRainSystems.Count; i++)
        {
            ParticleSystem rain = stormRainSystems[i];
            if (rain == null) continue;
            rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            rain.Play(true);
        }
        if (stormWindDebris != null)
        {
            stormWindDebris.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            stormWindDebris.Play(true);
        }
    }

    public void UpdateStormVisual(float elapsed, bool playerInShelter)
    {
        float gust = 0.45f + Mathf.Max(0f, Mathf.Sin(elapsed * 1.55f)) * 0.55f;
        for (int i = 0; i < stormTrees.Count; i++)
        {
            Transform tree = stormTrees[i];
            if (tree == null) continue;
            float sway = gust * (2.0f + (i % 4) * 0.45f);
            tree.rotation = stormTreeBaseRotations[i]
                * Quaternion.Euler(Mathf.Sin(elapsed * 2.15f + i * 0.91f) * sway * 0.6f,
                    0f, Mathf.Sin(elapsed * 1.7f + i * 1.37f) * sway);
        }
        for (int i = 0; i < stormClouds.Count; i++)
        {
            Transform cloud = stormClouds[i];
            if (cloud == null) continue;
            Vector3 position = stormCloudBasePositions[i];
            position.x += Mathf.Sin(elapsed * 0.065f + i * 0.31f) * 1.35f;
            cloud.position = position;
        }
        for (int i = 0; i < stormWaveCrests.Count; i++)
        {
            Transform wave = stormWaveCrests[i];
            if (wave == null) continue;
            Vector3 position = stormWaveBasePositions[i];
            position.z += Mathf.Sin(elapsed * 2.2f + i * 0.73f) * 0.48f;
            position.y += Mathf.Abs(Mathf.Sin(elapsed * 2.8f + i * 0.91f)) * 0.16f;
            wave.position = position;
        }

        if (stormPowerPolePivot != null)
        {
            float fall = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(13.5f, 16f, elapsed));
            float sway = Mathf.Sin(elapsed * 2.1f) * (1.8f + gust * 3.1f);
            stormPowerPolePivot.localRotation = Quaternion.Euler(0f, 0f, sway + fall * 56f);
        }

        bool postEventVisible = elapsed >= 16f;
        for (int i = 0; i < stormPostEventObjects.Count; i++)
        {
            GameObject item = stormPostEventObjects[i];
            if (item != null && item.activeSelf != postEventVisible)
                item.SetActive(postEventVisible);
        }

        if (stormLightning != null)
        {
            float flash = Mathf.Repeat(elapsed + 3.9f, 8.7f);
            float intensity = flash < 0.075f
                ? Mathf.Lerp(3.4f, 0f, flash / 0.075f)
                : flash > 0.14f && flash < 0.185f
                    ? Mathf.Lerp(1.25f, 0f, (flash - 0.14f) / 0.045f)
                    : 0f;
            // A windowless refuge blocks the outside lightning flash.
            stormLightning.intensity = playerInShelter ? 0f : intensity;
        }
    }

    public void SetStormDoorExposureVisual(bool exposed)
    {
        UpdateStormIndoorWater(exposed, Time.deltaTime);
    }

    public void EndStormVisual()
    {
        PrototypeAudio.SetStormRain(false, false);
        for (int i = 0; i < stormRainSystems.Count; i++)
            if (stormRainSystems[i] != null)
                stormRainSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (stormWindDebris != null)
            stormWindDebris.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (stormLightning != null)
            stormLightning.intensity = 0f;
    }

    public void BeginWildfireVisual()
    {
        for (int i = 0; i < wildfireFlameLayers.Count; i++)
            wildfireFlameLayers[i].SetActive(i == 0);
    }

    public void UpdateWildfireVisual(float elapsed)
    {
        int activeLayers = Mathf.Clamp(Mathf.FloorToInt(elapsed / 4.8f) + 1, 1, wildfireFlameLayers.Count);
        for (int i = 0; i < wildfireFlameLayers.Count; i++)
            if (wildfireFlameLayers[i] != null && wildfireFlameLayers[i].activeSelf != (i < activeLayers))
                wildfireFlameLayers[i].SetActive(i < activeLayers);
        for (int i = 0; i < wildfireFlameLights.Count; i++)
        {
            Light flameLight = wildfireFlameLights[i];
            if (flameLight == null || !flameLight.gameObject.activeInHierarchy) continue;
            flameLight.intensity = 2.15f + Mathf.PerlinNoise(Time.time * 3.7f, i * 5.1f) * 1.25f;
        }
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
        const int segments = 20;
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
            frontPanel.AddComponent<PrototypeLandslideDebrisImpact>();
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
            roofPanel.AddComponent<PrototypeLandslideDebrisImpact>();
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
            tree.AddComponent<PrototypeLandslideDebrisImpact>();
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
        Material fissure = MakeMaterial("Zemindeki Koyu Fissürler", new Color32(35, 29, 24, 255));
        Material crackedSoil = MakeMaterial("Çatlamış Zeminde Açığa Çıkan Toprak", new Color32(119, 87, 59, 255));
        CreateLandslideSlopePatch("Çatlakların Çevresindeki Açığa Çıkmış Toprak", new Vector2(crackX, crackZ),
            1.24f, 0.84f, crackedSoil, 754, 0.018f);
        float[,] crackSegments =
        {
            { -0.66f, -0.37f, 0.55f, 0.085f, -18f }, { -0.28f, -0.22f, 0.62f, 0.090f, 22f },
            { 0.10f, 0.03f, 0.58f, 0.095f, -20f }, { 0.46f, 0.20f, 0.56f, 0.082f, 25f },
            { 0.72f, 0.40f, 0.38f, 0.074f, -16f }, { -0.28f, -0.22f, 0.38f, 0.052f, 67f },
            { 0.46f, 0.20f, 0.34f, 0.048f, -58f }
        };
        for (int i = 0; i < crackSegments.GetLength(0); i++)
        {
            float x = crackX + crackSegments[i, 0];
            float z = crackZ + crackSegments[i, 1];
            GameObject fissureSegment = CreateBox("Geniş Dallanan Zemin Çatlağı " + i,
                new Vector3(x, LandslideSlopeHeight(x, z) + 0.045f, z),
                new Vector3(crackSegments[i, 2], 0.04f, crackSegments[i, 3]), fissure);
            fissureSegment.transform.rotation = Quaternion.Euler(0f, crackSegments[i, 4], 0f);
            Collider fissureCollider = fissureSegment.GetComponent<Collider>();
            if (fissureCollider != null) fissureCollider.enabled = false;
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
        Material wetSoil = MakeMaterial("Sızıntı Çevresi Islak Koyu Toprak", new Color32(70, 59, 47, 255));
        Material seepWater = MakeMaterial("Parlak Soğuk Yamaç Suyu", new Color32(69, 142, 160, 255));
        if (seepWater.HasProperty("_Smoothness")) seepWater.SetFloat("_Smoothness", 0.82f);
        CreateLandslideSlopePatch("Sızıntının Islak Toprağı", new Vector2(seepageX, seepageZ), 0.64f, 0.42f, wetSoil, 755, 0.04f);
        for (int i = 0; i < 4; i++)
        {
            float puddleX = seepageX - 0.3f + i * 0.18f;
            float puddleZ = seepageZ - 0.3f + i * 0.16f;
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            water.name = "Yamaç Eteğindeki Parlak Su Birikintisi " + i;
            water.transform.position = new Vector3(puddleX, LandslideSlopeHeight(puddleX, puddleZ) + 0.045f, puddleZ);
            water.transform.localScale = new Vector3(0.42f - i * 0.035f, 0.025f, 0.19f + (i % 2) * 0.045f);
            water.GetComponent<Renderer>().sharedMaterial = seepWater;
            water.transform.rotation = Quaternion.Euler(0f, i * 22f, 0f);
            Collider waterCollider = water.GetComponent<Collider>();
            if (waterCollider != null) waterCollider.enabled = false;
        }
        GameObject waterTrail = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        waterTrail.name = "Sızıntıdan Akan İnce Su İzi";
        waterTrail.transform.position = new Vector3(3.55f, LandslideSlopeHeight(3.55f, -3.65f) + 0.04f, -3.65f);
        waterTrail.transform.localScale = new Vector3(0.12f, 0.023f, 0.62f);
        waterTrail.transform.rotation = Quaternion.Euler(0f, 12f, 0f);
        waterTrail.GetComponent<Renderer>().sharedMaterial = seepWater;
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
        if (beaconRenderer != null && beaconRenderer.sharedMaterial != null && beaconRenderer.sharedMaterial.HasProperty("_EmissionColor"))
        {
            beaconRenderer.sharedMaterial.EnableKeyword("_EMISSION");
            beaconRenderer.sharedMaterial.SetColor("_EmissionColor", new Color(0.2f, 0.9f, 0.55f) * 1.2f);
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
        landslideDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
        if (materials.TryGetValue("Akışkan Koyu Çamur", out Material mudMaterial)
            && mudMaterial != null && mudMaterial.shader != null)
        {
            // Use the same URP shader already used by the landslide materials. Particle-only
            // shaders can be stripped from player builds and then render bright pink.
            Material particleMaterial = new Material(mudMaterial.shader);
            particleMaterial.name = "Heyelan Tozu Şeffaf Parçacık Malzemesi";
            Color dustTint = new Color32(156, 130, 104, 210);
            if (particleMaterial.HasProperty("_BaseColor")) particleMaterial.SetColor("_BaseColor", dustTint);
            if (particleMaterial.HasProperty("_Color")) particleMaterial.SetColor("_Color", dustTint);
            particleMaterial.SetOverrideTag("RenderType", "Transparent");
            if (particleMaterial.HasProperty("_Surface")) particleMaterial.SetFloat("_Surface", 1f);
            if (particleMaterial.HasProperty("_Blend")) particleMaterial.SetFloat("_Blend", 0f);
            if (particleMaterial.HasProperty("_SrcBlend"))
                particleMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (particleMaterial.HasProperty("_DstBlend"))
                particleMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (particleMaterial.HasProperty("_ZWrite")) particleMaterial.SetInt("_ZWrite", 0);
            particleMaterial.DisableKeyword("_ALPHATEST_ON");
            particleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            particleMaterial.EnableKeyword("_ALPHABLEND_ON");
            particleMaterial.renderQueue = 3000;
            renderer.sharedMaterial = particleMaterial;
        }
        else
            Debug.LogWarning("Heyelan parçacıkları için kullanılabilir URP materyali bulunamadı.");
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    public void BeginLandslideVisual()
    {
        lastLandslideDustBurst = -1;
        for (int i = 0; i < landslideDebris.Count; i++)
        {
            LandslideDebrisBody item = landslideDebris[i];
            item.Released = false;
            if (!item.Body.isKinematic)
            {
                item.Body.linearVelocity = Vector3.zero;
                item.Body.angularVelocity = Vector3.zero;
            }
            item.Body.isKinematic = true;
            item.Body.useGravity = false;
            item.Body.position = item.StartPosition;
            item.Body.rotation = item.StartRotation;
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
        Material sideWindowGlass = Resources.Load<Material>("SelPencereCami");
        if (sideWindowGlass == null)
            sideWindowGlass = MakeMaterial("Sel Evi Yan Pencere Camı", new Color32(105, 166, 190, 72));
        sideWindowGlass.SetOverrideTag("RenderType", "Transparent");
        if (sideWindowGlass.HasProperty("_Surface"))
            sideWindowGlass.SetFloat("_Surface", 1f);
        if (sideWindowGlass.HasProperty("_Blend")) sideWindowGlass.SetFloat("_Blend", 0f);
        if (sideWindowGlass.HasProperty("_SrcBlend"))
            sideWindowGlass.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (sideWindowGlass.HasProperty("_DstBlend"))
            sideWindowGlass.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (sideWindowGlass.HasProperty("_SrcBlendAlpha"))
            sideWindowGlass.SetInt("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
        if (sideWindowGlass.HasProperty("_DstBlendAlpha"))
            sideWindowGlass.SetInt("_DstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (sideWindowGlass.HasProperty("_ZWrite")) sideWindowGlass.SetInt("_ZWrite", 0);
        if (sideWindowGlass.HasProperty("_AlphaClip")) sideWindowGlass.SetFloat("_AlphaClip", 0f);
        sideWindowGlass.DisableKeyword("_ALPHATEST_ON");
        sideWindowGlass.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        sideWindowGlass.DisableKeyword("_ALPHAMODULATE_ON");
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
        // The leaf is shut during the flood scenario, so it must block the player even while crouching.
        if (leafCollider != null) leafCollider.enabled = true;
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
        ring.sharedMaterial = MakeMaterial("Güvenli Bölge Işığı", new Color32(56, 207, 179, 255));
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
        arrow.sharedMaterial = ring.sharedMaterial;
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

    public void SetLandslideDirectionArrow(bool visible, Vector3 targetPosition)
    {
        if (ui != null)
            ui.SetLandslideDirectionArrow(visible, targetPosition);
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




