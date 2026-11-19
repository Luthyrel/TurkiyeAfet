using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class PrototypeMapAuthoringWindow : EditorWindow
{
    private const string PrototypeScenePath = "Assets/Scenes/DepremPrototip.unity";
    private const string PreviewRootName = "Oyun Haritası - Düzenleme Önizlemesi";
    private const string PlacementRootName = "Sahneye Yerleştirilen Harita Varlıkları";
    private const string EarthquakeTerrainRootName = "Deprem Haritası";
    private const string SelectedDisasterKey = "TurkiyeAfet.MapAuthoring.SelectedDisaster";
    private const string PreviewDisasterKey = "TurkiyeAfet.MapAuthoring.PreviewDisaster";
    private static readonly string[] DisasterLabels = { "Deprem", "Sel", "Heyelan", "Orman yangını", "Fırtına", "Hortum" };
    private static bool isGeneratingPreview;
    private Vector2 scrollPosition;

    [MenuItem("Türkiye Afet/Harita Düzenleme Penceresi")]
    private static void OpenWindow()
    {
        PrototypeMapAuthoringWindow window = GetWindow<PrototypeMapAuthoringWindow>();
        window.titleContent = new GUIContent("Harita Düzenleme");
        window.minSize = new Vector2(330f, 360f);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.LabelField("Haritayı Scene Üzerinden Düzenle", EditorStyles.boldLabel);

        if (SceneManager.GetActiveScene().path != PrototypeScenePath)
        {
            EditorGUILayout.HelpBox("Önce Assets/Scenes/DepremPrototip sahnesini aç.", MessageType.Warning);
            if (GUILayout.Button("Deprem prototip sahnesini aç"))
                EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Single);
        }
        else
        {
            GameObject preview = GameObject.Find(PreviewRootName);
            int selectedDisaster = Mathf.Clamp(EditorPrefs.GetInt(SelectedDisasterKey, 0), 0, DisasterLabels.Length - 1);
            int previewDisaster = preview != null ? EditorPrefs.GetInt(PreviewDisasterKey, -1) : -1;
            if (preview != null)
            {
                PrototypeMapPreviewInfo previewInfo = preview.GetComponent<PrototypeMapPreviewInfo>();
                if (previewInfo != null)
                    previewDisaster = (int)previewInfo.DisasterType;
            }
            selectedDisaster = EditorGUILayout.Popup("Önizlenecek afet", selectedDisaster, DisasterLabels);
            EditorPrefs.SetInt(SelectedDisasterKey, selectedDisaster);
            bool samePreviewIsReady = preview != null && previewDisaster == selectedDisaster;

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                using (new EditorGUI.DisabledScope(isGeneratingPreview || samePreviewIsReady))
                {
                    string buttonLabel = preview == null
                        ? "Harita önizlemesini oluştur"
                        : "Önizlemeyi seçilen afetle değiştir";
                    if (GUILayout.Button(buttonLabel, GUILayout.Height(32f)))
                        GeneratePreview(selectedDisaster);
                }

                using (new EditorGUI.DisabledScope(isGeneratingPreview || preview == null))
                {
                    if (GUILayout.Button("Değişiklikleri Play Mode ve Build için kaydet", GUILayout.Height(32f)))
                    {
                        int disasterToSave = previewDisaster >= 0 ? previewDisaster : selectedDisaster;
                        SavePreviewForPlayModeAndBuild(preview, (GameManager.DisasterType)disasterToSave);
                    }
                }

                if (preview != null && GUILayout.Button("Önizlemeyi kaldır"))
                    RemovePreview();

                EditorGUILayout.Space(6f);
                if (GUILayout.Button("Asset yerleşim alanını seç / oluştur"))
                    SelectOrCreatePlacementRoot();

                using (new EditorGUI.DisabledScope(FindPlacementRoot() == null || Selection.gameObjects.Length == 0))
                {
                    if (GUILayout.Button("Seçili asseti yerleşim alanına al"))
                        ParentSelectionToPlacementRoot();
                }

                if (GUILayout.Button("Scene görünümünü haritaya odakla"))
                    FocusMapInSceneView();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "1. Afeti seçip önizlemeyi oluştur.\n" +
                "2. Asseti Project'ten Scene'e sürükle, konumlandır ve yerleşim alanına al.\n" +
                "3. Önizlemeyi düzenle, ardından Play Mode ve Build için kaydet düğmesine bas.\n\n" +
                "Kaydedilen önizleme, aynı afet seçildiğinde Play Mode ve build sırasında kullanılır.",
                MessageType.Info);

            GameObject placementRoot = FindPlacementRoot();
            EditorGUILayout.LabelField("Önizleme", preview == null
                ? "Yok"
                : (samePreviewIsReady ? DisasterLabels[selectedDisaster] + " hazır" : "Başka afetin haritası açık"));
            EditorGUILayout.LabelField("Asset yerleşim alanı", placementRoot != null ? "Hazır" : "Henüz oluşturulmadı");
        }

        EditorGUILayout.EndScrollView();
    }

    private static void GeneratePreview(int disasterIndex)
    {
        if (EditorApplication.isPlaying || isGeneratingPreview)
        {
            return;
        }

        PrototypeBootstrap bootstrap = Object.FindAnyObjectByType<PrototypeBootstrap>();
        if (bootstrap == null)
        {
            EditorUtility.DisplayDialog("Harita düzenleme", "Açık sahnede PrototypeBootstrap bulunamadı.", "Tamam");
            return;
        }

        GameObject oldPreview = GameObject.Find(PreviewRootName);
        if (oldPreview != null && !EditorUtility.DisplayDialog(
                "Önizlemeyi değiştir",
                "Mevcut önizlemeyi değiştirmek, üzerindeki konum ve materyal düzenlemelerini kaldırır. Devam etmeden önce Play Mode ve Build için kaydet.",
                "Değiştir",
                "İptal"))
            return;

        isGeneratingPreview = true;
        bool generated;
        try
        {
            SetEarthquakeTerrainActive(disasterIndex == (int)GameManager.DisasterType.Earthquake, true);
            generated = bootstrap.GenerateMapEditingPreviewForEditor((GameManager.DisasterType)disasterIndex);
        }
        finally
        {
            isGeneratingPreview = false;
        }

        if (!generated)
            return;

        EditorPrefs.SetInt(PreviewDisasterKey, disasterIndex);
        GameObject placementRoot = FindPlacementRoot();
        if (placementRoot != null)
            Selection.activeGameObject = placementRoot;
        SceneView.lastActiveSceneView?.Repaint();
    }

    private static void SavePreviewForPlayModeAndBuild(GameObject preview, GameManager.DisasterType disasterType)
    {
        if (preview == null)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path))
        {
            EditorUtility.DisplayDialog("Harita düzenleme", "Önce sahneyi kaydet, ardından tekrar dene.", "Tamam");
            return;
        }

        preview.tag = "Untagged";
        PrototypeMapPreviewInfo previewInfo = preview.GetComponent<PrototypeMapPreviewInfo>();
        if (previewInfo == null)
            previewInfo = Undo.AddComponent<PrototypeMapPreviewInfo>(preview);
        previewInfo.DisasterType = disasterType;
        EditorUtility.SetDirty(previewInfo);
        EditorUtility.SetDirty(preview);

        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool sceneFound = false;
        for (int i = 0; i < buildScenes.Count; i++)
        {
            if (buildScenes[i].path != scene.path)
                continue;

            if (!buildScenes[i].enabled)
                buildScenes[i] = new EditorBuildSettingsScene(scene.path, true);
            sceneFound = true;
            break;
        }

        if (!sceneFound)
            buildScenes.Add(new EditorBuildSettingsScene(scene.path, true));
        EditorBuildSettings.scenes = buildScenes.ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene);
        if (!saved)
        {
            EditorUtility.DisplayDialog("Harita kaydedilemedi", "Sahne diske kaydedilemedi. Console'u kontrol et.", "Tamam");
            return;
        }

        EditorUtility.DisplayDialog(
            "Harita kaydedildi",
            disasterType + " önizlemesi Play Mode ve build için kaydedildi. Yeni build alırken bu sahnenin build listesinde olduğundan emin ol.",
            "Tamam");
    }

    private static void RemovePreview()
    {
        GameObject preview = GameObject.Find(PreviewRootName);
        if (preview != null)
        {
            Undo.DestroyObjectImmediate(preview);
            EditorPrefs.DeleteKey(PreviewDisasterKey);
            SetEarthquakeTerrainActive(false, true);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }

    private static void SetEarthquakeTerrainActive(bool active, bool moveUnderEarthquakeRoot)
    {
        GameObject terrain = FindInActiveScene("Terrain");
        GameObject terrainRoot = FindInActiveScene(EarthquakeTerrainRootName);
        if (terrain == null && terrainRoot == null)
            return;

        if (terrainRoot == null && moveUnderEarthquakeRoot)
        {
            terrainRoot = new GameObject(EarthquakeTerrainRootName);
            Undo.RegisterCreatedObjectUndo(terrainRoot, "Deprem harita grubunu oluştur");
        }

        if (terrainRoot == null)
            return;

        if (terrain != null && terrain.transform.parent != terrainRoot.transform)
            Undo.SetTransformParent(terrain.transform, terrainRoot.transform, "Terrain'i deprem haritasına ekle");

        terrainRoot.SetActive(active);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static GameObject FindInActiveScene(string objectName)
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

    private static GameObject FindPlacementRoot()
    {
        return GameObject.Find(PlacementRootName);
    }

    private static void SelectOrCreatePlacementRoot()
    {
        if (EditorApplication.isPlaying)
            return;

        GameObject root = FindPlacementRoot();
        if (root == null)
        {
            root = new GameObject(PlacementRootName);
            Undo.RegisterCreatedObjectUndo(root, "Harita yerleşim kökü oluştur");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
    }

    private static void ParentSelectionToPlacementRoot()
    {
        GameObject root = FindPlacementRoot();
        if (root == null)
            return;

        foreach (GameObject selected in Selection.gameObjects)
        {
            if (selected == null || selected == root || selected.transform.IsChildOf(root.transform))
                continue;

            Undo.SetTransformParent(selected.transform, root.transform, "Harita nesnesini kalıcı köke al");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static void FocusMapInSceneView()
    {
        SceneView view = SceneView.lastActiveSceneView;
        if (view == null)
            view = GetWindow<SceneView>();

        view.pivot = new Vector3(-5.5f, 0f, 1f);
        view.size = 28f;
        view.orthographic = false;
        view.rotation = Quaternion.Euler(48f, 35f, 0f);
        view.Repaint();
    }
}
