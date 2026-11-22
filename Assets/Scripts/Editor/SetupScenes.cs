using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RollingKitchen.EditorTools
{
    /// <summary>Membuat 5 scene kosong dan mendaftarkannya ke Build Settings.</summary>
    public static class SetupScenes
    {
        private static readonly string[] SceneNames = { "TitleScreen", "Rumah", "Pasar", "FoodTruck", "Rekap" };
        private const string Folder = "Assets/Scenes";

        [MenuItem("Rolling Kitchen/Setup Scenes")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "Scenes");

            var paths = new List<string>();
            foreach (var name in SceneNames)
            {
                string path = $"{Folder}/{name}.unity";
                paths.Add(path);

                // Jangan timpa scene yang sudah ada.
                if (File.Exists(path)) continue;

                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                CreateCamera();
                EditorSceneManager.SaveScene(scene, path);
            }

            UpdateBuildSettings(paths);
            AssetDatabase.Refresh();
            Debug.Log("Rolling Kitchen: 5 scene siap dan terdaftar di Build Settings.");
        }

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
            go.transform.position = new Vector3(0f, 0f, -10f);
        }

        // Scene kita ditaruh di depan (TitleScreen = index 0); scene lain yang sudah ada tetap dipertahankan.
        private static void UpdateBuildSettings(List<string> ours)
        {
            var list = new List<EditorBuildSettingsScene>();
            foreach (var p in ours) list.Add(new EditorBuildSettingsScene(p, true));
            foreach (var s in EditorBuildSettings.scenes)
                if (!ours.Contains(s.path)) list.Add(s);
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
