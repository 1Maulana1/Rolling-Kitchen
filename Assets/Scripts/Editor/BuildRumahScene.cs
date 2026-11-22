using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RollingKitchen.EditorTools
{
    /// <summary>
    /// Membangun scene Rumah placeholder (top-down): pintu ke Pasar dan Food Truck, dapur untuk
    /// Eksperimen Dapur, dan resep spesial contoh. Aset yang ada tidak ditimpa; objek scene lama dibangun ulang.
    /// </summary>
    public static class BuildRumahScene
    {
        private const string ScenePath = "Assets/Scenes/Rumah.unity";
        private const string RecipeDir = "Assets/Data/Recipes";
        private const string ItemPrefabPath = "Assets/Prefabs/ExperimentItem.prefab";
        private const string WorldName = "Rumah World";

        // Latar pixel art 480x270; 25 px = 1 unit -> dunia 19.2 x 10.8 (pas satu layar, kamera diam).
        private const string BackgroundPath = "Assets/Art/Backgrounds/bg-rumah-v2.png";
        private static readonly ArtLayout Art = new ArtLayout(480, 270, 25);

        // Rintangan dari gambar (x1,y1,x2,y2 piksel, asal kiri-atas).
        private static readonly (string name, Vector4 r)[] Solids =
        {
            // Dinding luar
            ("Dinding atas", new Vector4(0, 0, 480, 5)), ("Dinding bawah", new Vector4(0, 255, 480, 270)),
            ("Dinding kiri", new Vector4(0, 0, 14, 270)), ("Dinding kanan", new Vector4(466, 0, 480, 270)),
            // Sekat kamar atas dan dinding tengah (celah = pintu dapur x92-117, kamar mandi x236-259, kamar x362-387)
            ("Sekat dapur|kamar mandi", new Vector4(200, 0, 206, 124)), ("Sekat kamar mandi|kamar", new Vector4(290, 0, 296, 124)),
            ("Dinding tengah 1", new Vector4(14, 112, 92, 124)), ("Dinding tengah 2", new Vector4(117, 112, 236, 124)),
            ("Dinding tengah 3", new Vector4(259, 112, 362, 124)), ("Dinding tengah 4", new Vector4(387, 112, 466, 124)),
            // Dapur
            ("Kulkas", new Vector4(14, 5, 38, 45)), ("Meja dapur", new Vector4(38, 5, 200, 43)),
            ("Meja makan dapur", new Vector4(60, 62, 151, 92)), ("Tempat sampah", new Vector4(183, 90, 198, 110)),
            // Kamar mandi
            ("Wastafel", new Vector4(206, 5, 234, 38)), ("Bak mandi", new Vector4(238, 5, 290, 53)),
            ("Kloset", new Vector4(206, 84, 229, 101)), ("Sekat kloset", new Vector4(235, 90, 239, 112)),
            ("Pot kamar mandi", new Vector4(275, 88, 290, 110)),
            // Kamar tidur
            ("Lemari", new Vector4(296, 5, 341, 46)), ("Meja lampu", new Vector4(379, 22, 398, 41)),
            ("Kasur", new Vector4(397, 5, 466, 98)), ("Meja belajar", new Vector4(299, 73, 326, 97)),
            // Ruang tamu
            ("Rak buku", new Vector4(17, 124, 82, 138)), ("Rak TV", new Vector4(282, 124, 353, 137)),
            ("Buku resep", new Vector4(184, 128, 208, 152)), ("Lampu", new Vector4(424, 128, 446, 153)),
            ("Meja makan", new Vector4(90, 156, 181, 214)), ("Meja kopi", new Vector4(341, 171, 390, 197)),
            ("Sofa", new Vector4(313, 213, 418, 246)),
            ("Pot kiri", new Vector4(17, 228, 39, 251)), ("Pot tengah", new Vector4(209, 230, 231, 253)),
            ("Pot kanan", new Vector4(443, 228, 465, 251)),
        };

        // nama resep, harga jual, bahan (nama aset di Assets/Data/Ingredients)
        private static readonly (string name, int price, string[] items)[] SpecialRecipes =
        {
            ("Nasi Goreng Spesial", 45, new[] { "Beras", "Telur", "Bumbu" }),
            ("Ayam Goreng Spesial", 60, new[] { "Ayam", "Bumbu", "Minyak" }),
            ("Capcay Laut Spesial", 70, new[] { "Sayur", "Wortel", "Udang" }),
        };

        [MenuItem("Rolling Kitchen/Build Rumah")]
        public static void Run()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogError("Rumah.unity belum ada. Jalankan Rolling Kitchen > Setup Scenes dulu.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            BuildPasarScene.CreateIngredients();
            BuildPasarScene.CreateSquareSprite();
            CreateItemPrefab();
            CreateRecipes();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Muat ulang aset dari disk setelah scene terbuka (aset baru di-unload saat pindah scene).
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(BuildPasarScene.SquarePath);
            var itemPrefab = AssetDatabase.LoadAssetAtPath<Button>(ItemPrefabPath);
            var recipes = CreateRecipes();
            var background = ArtLayout.ImportSprite(BackgroundPath, Art.Ppu);
            if (square == null || itemPrefab == null || background == null || System.Array.Exists(recipes, r => r == null))
            {
                Debug.LogError("Gagal memuat sprite, prefab ExperimentItem, latar Rumah, atau resep contoh.");
                return;
            }

            // Buang hasil build sebelumnya.
            foreach (var v in Object.FindObjectsByType<ExperimentView>())
            {
                if (v == null) continue;
                var c = v.GetComponentInParent<Canvas>();
                if (c != null) Object.DestroyImmediate(c.gameObject);
            }
            var old = GameObject.Find(WorldName);
            if (old != null) Object.DestroyImmediate(old);

            if (Object.FindAnyObjectByType<GameManager>() == null)
                new GameObject("GameManager (uji)").AddComponent<GameManager>();

            BuildPasarScene.SetupCamera();
            Camera.main.backgroundColor = new Color(0.44f, 0.35f, 0.3f);
            var oldFollow = Camera.main.GetComponent<CameraFollow2D>();
            if (oldFollow != null) Object.DestroyImmediate(oldFollow);
            BuildPasarScene.BuildEventSystem();

            var player = BuildWorld(square, background, out var station);
            BuildCanvas(player, station, itemPrefab, recipes);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Rolling Kitchen: Rumah dibangun. Pintu kiri = Pasar, kanan = Food Truck, oranye = Dapur.");
        }

        // ---------- Aset ----------

        private static RecipeData[] CreateRecipes()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(RecipeDir)) AssetDatabase.CreateFolder("Assets/Data", "Recipes");

            var list = new RecipeData[SpecialRecipes.Length];
            for (int i = 0; i < SpecialRecipes.Length; i++)
            {
                string path = $"{RecipeDir}/{SpecialRecipes[i].name}.asset";
                var r = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
                if (r == null)
                {
                    r = ScriptableObject.CreateInstance<RecipeData>();
                    r.displayName = SpecialRecipes[i].name;
                    r.sellPrice = SpecialRecipes[i].price;
                    r.isSpecial = true;
                    var entries = new RecipeData.Entry[SpecialRecipes[i].items.Length];
                    for (int k = 0; k < entries.Length; k++)
                    {
                        entries[k].ingredient = AssetDatabase.LoadAssetAtPath<IngredientData>($"{BuildPasarScene.DataDir}/{SpecialRecipes[i].items[k]}.asset");
                        entries[k].amount = 1;
                    }
                    r.ingredients = entries;
                    AssetDatabase.CreateAsset(r, path);
                }
                list[i] = r;
            }
            AssetDatabase.SaveAssets();
            return list;
        }

        private static Button CreateItemPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Button>(ItemPrefabPath);
            if (existing != null) return existing;

            var root = new GameObject("ExperimentItem", typeof(RectTransform), typeof(Image), typeof(Button));
            root.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
            var label = BuildPasarScene.MakeText(root.transform, "Label", 22, TextAlignmentOptions.Left, 0f);
            BuildPasarScene.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(12, 0);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ItemPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<Button>();
        }

        // ---------- Dunia ----------

        private static PlayerController2D BuildWorld(Sprite square, Sprite background, out KitchenStation station)
        {
            var world = new GameObject(WorldName);
            Art.Picture(world.transform, "Latar Rumah", background, Vector2.zero, -10);

            var solids = new GameObject("Rintangan");
            solids.transform.SetParent(world.transform, false);
            foreach (var (name, r) in Solids) Art.Solid(solids.transform, name, r.x, r.y, r.z, r.w);

            // Pintu ke Pasar (kiri): hanya Pagi. Pintu ke Food Truck (kanan): Siang-Sore dan Malam.
            // Titik interaksinya di ambang pintu (bagian dalam dinding), gambar pintunya sudah ada di latar.
            MakeDoor(world.transform, "Pintu Pasar", Art.ToWorld(16, 178), GameScene.Pasar, "Pergi ke Pasar", true, false, false);
            MakeDoor(world.transform, "Pintu Food Truck", Art.ToWorld(464, 178), GameScene.FoodTruck, "Ke Food Truck", false, true, true);

            // Dapur (area eksperimen) = kompor di meja dapur.
            var kitchen = new GameObject("Dapur (Kompor)");
            kitchen.transform.SetParent(world.transform, false);
            kitchen.transform.position = Art.ToWorld(115, 40);
            station = kitchen.AddComponent<KitchenStation>();

            // Pemain mulai di ruang tamu, dekat pintu Pasar.
            return BuildPasarScene.MakePlayer(world.transform, square, Art.ToWorld(60, 190), new Vector2(0.64f, 0.88f));
        }

        private static void MakeDoor(Transform parent, string name, Vector2 pos, GameScene target, string label,
            bool pagi, bool siangSore, bool malam)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var door = go.AddComponent<Door>();
            var so = new SerializedObject(door);
            so.FindProperty("target").enumValueIndex = (int)target;
            so.FindProperty("label").stringValue = label;
            so.FindProperty("openInPagi").boolValue = pagi;
            so.FindProperty("openInSiangSore").boolValue = siangSore;
            so.FindProperty("openInMalam").boolValue = malam;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- UI ----------

        private static void BuildCanvas(PlayerController2D player, KitchenStation station, Button itemPrefab, RecipeData[] recipes)
        {
            var canvasGo = BuildPasarScene.CreateCanvasWithHud("WASD / Panah: gerak   E: interaksi   Esc: tutup", out var prompt);

            var uiGo = new GameObject("Eksperimen UI", typeof(RectTransform), typeof(ExperimentView));
            uiGo.transform.SetParent(canvasGo.transform, false);
            BuildPasarScene.Stretch((RectTransform)uiGo.transform);

            var panel = new GameObject("Panel Eksperimen", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(uiGo.transform, false);
            panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.94f);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(780, 720);
            var v = panel.GetComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(16, 16, 16, 16);
            v.spacing = 8f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var title = BuildPasarScene.MakeText(panel.transform, "Judul", 34, TextAlignmentOptions.Center, 0f);
            title.text = "Eksperimen Dapur";
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;

            // Daftar bahan (2 kolom).
            var list = new GameObject("Daftar Bahan", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
            list.transform.SetParent(panel.transform, false);
            var grid = list.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(360, 44);
            grid.spacing = new Vector2(8, 6);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            list.GetComponent<LayoutElement>().preferredHeight = 400f;

            var selected = BuildPasarScene.MakeText(panel.transform, "Terpilih", 24, TextAlignmentOptions.Left, 0f);
            selected.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
            var result = BuildPasarScene.MakeText(panel.transform, "Hasil", 24, TextAlignmentOptions.Left, 0f);
            result.gameObject.AddComponent<LayoutElement>().preferredHeight = 70f;

            // Baris tombol: Masak + Tutup.
            var buttons = new GameObject("Tombol", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            buttons.transform.SetParent(panel.transform, false);
            buttons.GetComponent<LayoutElement>().preferredHeight = 56f;
            var h = buttons.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 16f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = true;

            var cook = MakeButton(buttons.transform, "Masak", new Color(0.85f, 0.35f, 0.2f), out _);
            var close = MakeButton(buttons.transform, "Tutup", new Color(0.4f, 0.4f, 0.45f), out var closeLabel);

            var view = uiGo.GetComponent<ExperimentView>();
            var vso = new SerializedObject(view);
            vso.FindProperty("panelRoot").objectReferenceValue = panel;
            vso.FindProperty("itemParent").objectReferenceValue = list.transform;
            vso.FindProperty("itemButtonPrefab").objectReferenceValue = itemPrefab;
            vso.FindProperty("selectedText").objectReferenceValue = selected;
            vso.FindProperty("resultText").objectReferenceValue = result;
            vso.FindProperty("cookButton").objectReferenceValue = cook;
            vso.FindProperty("closeLabel").objectReferenceValue = closeLabel;
            var rp = vso.FindProperty("specialRecipes");
            rp.arraySize = recipes.Length;
            for (int i = 0; i < recipes.Length; i++) rp.GetArrayElementAtIndex(i).objectReferenceValue = recipes[i];
            vso.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(cook.onClick, view.Cook);
            UnityEventTools.AddPersistentListener(close.onClick, view.Close);

            var sso = new SerializedObject(station);
            sso.FindProperty("view").objectReferenceValue = view;
            sso.ApplyModifiedPropertiesWithoutUndo();

            var pso = new SerializedObject(player);
            pso.FindProperty("promptText").objectReferenceValue = prompt;
            pso.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button MakeButton(Transform parent, string text, Color color, out TextMeshProUGUI label)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            label = BuildPasarScene.MakeText(go.transform, "Label", 26, TextAlignmentOptions.Center, 0f);
            label.text = text;
            BuildPasarScene.Stretch(label.rectTransform);
            return go.GetComponent<Button>();
        }
    }
}
