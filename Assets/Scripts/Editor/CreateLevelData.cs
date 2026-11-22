using System.IO;
using UnityEditor;
using UnityEngine;

namespace RollingKitchen.EditorTools
{
    /// <summary>Membuat Assets/Resources/Levels.asset berisi pengaturan demo 7 hari. Tidak menimpa yang sudah ada.</summary>
    public static class CreateLevelData
    {
        private const string Path = "Assets/Resources/Levels.asset";

        // target, eksperimen, pelanggan maks, kesabaran, jeda spawn
        // Semua angka SEMENTARA untuk playtest; ubah langsung di asset Levels.
        private static readonly (int target, bool exp, int max, float patience, float spawn)[] Demo =
        {
            (60,  false, 2, 40f, 12f),
            (80,  false, 2, 38f, 11f),
            (100, true,  3, 36f, 10f),
            (120, true,  3, 34f, 9f),
            (140, true,  3, 32f, 9f),
            (160, true,  3, 30f, 8f),
            (180, true,  3, 28f, 8f),
        };

        [MenuItem("Rolling Kitchen/Create Level Data")]
        public static void Run()
        {
            if (File.Exists(Path))
            {
                Debug.LogWarning("Levels.asset sudah ada; tidak ditimpa. Edit langsung di Inspector.");
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<LevelSet>(Path);
                return;
            }
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");

            var set = ScriptableObject.CreateInstance<LevelSet>();
            set.startingMoney = 100;
            set.finalTarget = 700;
            set.maxFailedDays = 2;
            set.days = new DayConfig[Demo.Length];
            for (int i = 0; i < Demo.Length; i++)
            {
                var d = Demo[i];
                set.days[i] = new DayConfig
                {
                    dailyTarget = d.target,
                    allowExperiment = d.exp,
                    maxCustomers = d.max,
                    patienceSeconds = d.patience,
                    spawnInterval = d.spawn,
                };
            }
            AssetDatabase.CreateAsset(set, Path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = set;
            Debug.Log("Rolling Kitchen: Levels.asset (demo 7 hari) dibuat di Assets/Resources.");
        }
    }
}
