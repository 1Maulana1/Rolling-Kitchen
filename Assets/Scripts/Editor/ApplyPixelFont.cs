using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace RollingKitchen.EditorTools
{
    /// <summary>
    /// Membuat font asset TMP dari Press Start 2P, menjadikannya font default TMP,
    /// lalu mengganti font LiberationSans di semua scene dan prefab game.
    /// Aman dijalankan ulang: asset yang sudah ada dipakai lagi, skala diperbarui.
    /// </summary>
    public static class ApplyPixelFont
    {
        private const string TtfPath = "Assets/Fonts/PressStart2P/PressStart2P-Regular.ttf";
        private const string AssetPath = "Assets/Fonts/PressStart2P/PressStart2P SDF.asset";
        private const string LiberationPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private static readonly string[] Scenes =
        {
            "Assets/Scenes/TitleScreen.unity", "Assets/Scenes/Rumah.unity", "Assets/Scenes/Pasar.unity",
            "Assets/Scenes/FoodTruck.unity", "Assets/Scenes/Rekap.unity",
        };

        // Tiap huruf Press Start 2P selebar 1 em (LiberationSans rata-rata ~0,55 em).
        // Skala ini mengecilkan semua teks yang memakai font ini tanpa mengubah fontSize
        // di scene/kode, supaya tata letak lama tidak meluap. Ubah di sini lalu jalankan ulang menu.
        private const float FaceScale = 0.6f;

        private const string Ascii =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~“”‘’…–—·×";

        [MenuItem("Rolling Kitchen/Terapkan Font Press Start 2P")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var font = GetOrCreateFontAsset();
            if (font == null) return;
            var liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationPath);

            SetDefaultFont(font);

            int changed = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                int n = Swap(root.GetComponentsInChildren<TMP_Text>(true), font, liberation);
                if (n > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                changed += n;
            }

            foreach (var path in Scenes)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var texts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
                int n = Swap(texts, font, liberation);
                if (n > 0) EditorSceneManager.SaveScene(scene);
                changed += n;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Rolling Kitchen: font Press Start 2P diterapkan ke {changed} teks (skala {FaceScale}).");
        }

        private static TMP_FontAsset GetOrCreateFontAsset()
        {
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
            if (fa == null)
            {
                var ttf = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
                if (ttf == null)
                {
                    Debug.LogError($"Rolling Kitchen: file font tidak ditemukan di {TtfPath}.");
                    return null;
                }
                // SDF dengan sampling besar: tepi kotak tetap tajam di ukuran berapa pun (UI ikut CanvasScaler).
                fa = TMP_FontAsset.CreateFontAsset(ttf, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024,
                                                   AtlasPopulationMode.Dynamic, true);
                fa.name = "PressStart2P SDF";
                AssetDatabase.CreateAsset(fa, AssetPath);
                fa.material.name = fa.name + " Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
                fa.atlasTexture.name = fa.name + " Atlas";
                AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
            }

            fa.TryAddCharacters(Ascii, out _);

            // Glyph yang tidak ada di Press Start 2P (mis. ▲ ▼) diambil dari LiberationSans.
            var liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationPath);
            if (liberation != null)
                fa.fallbackFontAssetTable = new List<TMP_FontAsset> { liberation };

            var so = new SerializedObject(fa);
            so.FindProperty("m_FaceInfo.m_Scale").floatValue = FaceScale;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }

        private static void SetDefaultFont(TMP_FontAsset font)
        {
            var settings = Resources.Load<TMP_Settings>("TMP Settings");
            if (settings == null)
            {
                Debug.LogWarning("Rolling Kitchen: TMP Settings tidak ditemukan; font default tidak diubah.");
                return;
            }
            var so = new SerializedObject(settings);
            so.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        private static int Swap(IEnumerable<TMP_Text> texts, TMP_FontAsset font, TMP_FontAsset liberation)
        {
            int n = 0;
            foreach (var t in texts)
            {
                if (t.font != null && t.font != liberation) continue; // font lain sengaja dipilih, biarkan
                t.font = font;
                t.fontSharedMaterial = font.material;
                EditorUtility.SetDirty(t);
                n++;
            }
            return n;
        }
    }
}
