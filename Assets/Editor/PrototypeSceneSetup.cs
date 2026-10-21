using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TurkiyeAfet.EditorTools
{
    public static class PrototypeSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/DepremPrototip.unity";
        private const string PreviousSampleScene = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Türkiye Afet/Prototip Sahnesini Oluştur")]
        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Türkiye Afet • Başlangıç");
            root.AddComponent<PrototypeBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (existing.path != ScenePath && existing.path != PreviousSampleScene)
                    scenes.Add(existing);
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PreviousSampleScene) != null)
                scenes.Add(new EditorBuildSettingsScene(PreviousSampleScene, true));

            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Deprem prototip sahnesi oluşturuldu ve Windows derleme listesinde ilk sıraya alındı: " + ScenePath);
        }
    }
}
