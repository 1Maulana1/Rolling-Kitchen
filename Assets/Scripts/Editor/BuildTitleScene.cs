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
    /// Membangun layar judul (latar pixel art + Mulai/Keluar + pilihan 3 slot) dan memastikan
    /// GameManager asli ada di sana. Juga menyediakan menu pengisi ItemDatabase untuk sistem simpan.
    /// </summary>
    public static class BuildTitleScene
    {
        private const string ScenePath = "Assets/Scenes/TitleScreen.unity";
        private const string BackgroundPath = "Assets/Art/Backgrounds/bg-title-screen.png";
        private const string DatabasePath = "Assets/Resources/ItemDatabase.asset";
        private const string WorldName = "Title World";
        private static readonly ArtLayout Art = new ArtLayout(480, 270, 25);

        [MenuItem("Rolling Kitchen/Build Title")]
        public static void Run()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogError("TitleScreen.unity belum ada. Jalankan Rolling Kitchen > Setup Scenes dulu.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            UpdateItemDatabase();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var background = ArtLayout.ImportSprite(BackgroundPath, Art.Ppu);
            if (background == null) return;

            // Buang hasil build lama.
            foreach (var v in Object.FindObjectsByType<TitleView>())
                if (v != null) Object.DestroyImmediate(v.GetComponentInParent<Canvas>().gameObject);
            var old = GameObject.Find(WorldName);
            if (old != null) Object.DestroyImmediate(old);

            // GameManager asli permainan (hidup lintas scene). Di scene lain hanya ada versi "(uji)".
            if (Object.FindAnyObjectByType<GameManager>() == null) new GameObject("GameManager").AddComponent<GameManager>();

            BuildPasarScene.SetupCamera();
            Camera.main.backgroundColor = new Color(0.1f, 0.08f, 0.18f);
            BuildPasarScene.BuildEventSystem();

            var world = new GameObject(WorldName);
            Art.Picture(world.transform, "Latar Judul", background, Vector2.zero, -10);

            BuildCanvas();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Rolling Kitchen: Title dibangun. Mulai -> pilih slot -> Lanjut / Main Baru.");
        }

        /// <summary>Isi Resources/ItemDatabase dengan semua IngredientData dan RecipeData di proyek.</summary>
        [MenuItem("Rolling Kitchen/Update Item Database")]
        public static void UpdateItemDatabase()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<ItemDatabase>();
                AssetDatabase.CreateAsset(db, DatabasePath);
            }
            db.ingredients = LoadAll<IngredientData>();
            db.recipes = LoadAll<RecipeData>();
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log($"Rolling Kitchen: ItemDatabase diperbarui ({db.ingredients.Length} bahan, {db.recipes.Length} resep).");
        }

        private static T[] LoadAll<T>() where T : Object
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            var list = new T[guids.Length];
            for (int i = 0; i < guids.Length; i++) list[i] = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
            return list;
        }

        // ---------- UI ----------

        private static void BuildCanvas()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var view = new GameObject("Title UI", typeof(RectTransform), typeof(TitleView));
            view.transform.SetParent(canvasGo.transform, false);
            BuildPasarScene.Stretch((RectTransform)view.transform);
            var tv = view.GetComponent<TitleView>();

            // Menu utama: kolom tombol di bawah logo (logo ada di kanan atas gambar latar).
            var menu = new GameObject("Menu", typeof(RectTransform), typeof(VerticalLayoutGroup));
            menu.transform.SetParent(view.transform, false);
            var mrt = (RectTransform)menu.transform;
            mrt.anchorMin = mrt.anchorMax = new Vector2(0.73f, 0.66f);
            mrt.pivot = new Vector2(0.5f, 1f);
            mrt.sizeDelta = new Vector2(380, 260);
            var mv = menu.GetComponent<VerticalLayoutGroup>();
            mv.spacing = 18f;
            mv.childControlWidth = mv.childControlHeight = true;
            mv.childForceExpandWidth = true;
            mv.childForceExpandHeight = false;
            var start = MakeButton(menu.transform, "Mulai", new Color(0.86f, 0.3f, 0.18f), 76f, out _);
            var options = MakeButton(menu.transform, "Options", new Color(0.22f, 0.2f, 0.32f, 0.92f), 64f, out _);
            var quit = MakeButton(menu.transform, "Keluar", new Color(0.22f, 0.2f, 0.32f, 0.92f), 64f, out _);
            UnityEventTools.AddPersistentListener(options.onClick, tv.OpenOptions);

            // Panel slot.
            var panel = new GameObject("Pilih Slot", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(view.transform, false);
            panel.GetComponent<Image>().color = new Color(0.09f, 0.07f, 0.13f, 1f); // opak: di color space linear, alpha <1 tampak jauh lebih tembus
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(980, 700);
            var pv = panel.GetComponent<VerticalLayoutGroup>();
            pv.padding = new RectOffset(32, 32, 24, 28);
            pv.spacing = 16f;
            pv.childControlWidth = pv.childControlHeight = true;
            pv.childForceExpandWidth = true;
            pv.childForceExpandHeight = false;

            var title = BuildPasarScene.MakeText(panel.transform, "Judul", 42, TextAlignmentOptions.Center, 0f);
            title.text = "Pilih Slot";
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;

            var so = new SerializedObject(tv);
            var rows = so.FindProperty("slots");
            rows.arraySize = SaveSystem.SlotCount;
            var continueButtons = new Button[SaveSystem.SlotCount];
            var newButtons = new Button[SaveSystem.SlotCount];
            for (int i = 0; i < SaveSystem.SlotCount; i++)
            {
                var row = new GameObject($"Slot {i + 1}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                row.transform.SetParent(panel.transform, false);
                row.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.07f);
                row.GetComponent<LayoutElement>().preferredHeight = 130f;
                var h = row.GetComponent<HorizontalLayoutGroup>();
                h.padding = new RectOffset(24, 20, 16, 16);
                h.spacing = 16f;
                h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = h.childControlHeight = true;
                h.childForceExpandWidth = false;
                h.childForceExpandHeight = true;

                var info = BuildPasarScene.MakeText(row.transform, "Info", 28, TextAlignmentOptions.Left, 0f);
                info.text = $"Slot {i + 1}";
                var infoLe = info.gameObject.AddComponent<LayoutElement>();
                infoLe.flexibleWidth = 1f;

                continueButtons[i] = MakeButton(row.transform, "Lanjut", new Color(0.2f, 0.58f, 0.32f), 0f, out _);
                continueButtons[i].GetComponent<LayoutElement>().preferredWidth = 170f;
                newButtons[i] = MakeButton(row.transform, "Main Baru", new Color(0.86f, 0.3f, 0.18f), 0f, out var newLabel);
                newButtons[i].GetComponent<LayoutElement>().preferredWidth = 210f;

                var el = rows.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("info").objectReferenceValue = info;
                el.FindPropertyRelative("continueButton").objectReferenceValue = continueButtons[i];
                el.FindPropertyRelative("newButton").objectReferenceValue = newButtons[i];
                el.FindPropertyRelative("newLabel").objectReferenceValue = newLabel;
            }
            var back = MakeButton(panel.transform, "Kembali", new Color(0.35f, 0.33f, 0.42f), 60f, out _);

            so.FindProperty("menuPanel").objectReferenceValue = menu;
            so.FindProperty("slotPanel").objectReferenceValue = panel;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(start.onClick, tv.OpenSlots);
            UnityEventTools.AddPersistentListener(quit.onClick, tv.Quit);
            UnityEventTools.AddPersistentListener(back.onClick, tv.CloseSlots);
            for (int i = 0; i < SaveSystem.SlotCount; i++)
            {
                UnityEventTools.AddIntPersistentListener(continueButtons[i].onClick, tv.ContinueSlot, i);
                UnityEventTools.AddIntPersistentListener(newButtons[i].onClick, tv.NewGameInSlot, i);
            }
        }

        private static Button MakeButton(Transform parent, string text, Color color, float height, out TextMeshProUGUI label)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            if (height > 0f) go.GetComponent<LayoutElement>().preferredHeight = height;
            label = BuildPasarScene.MakeText(go.transform, "Label", 30, TextAlignmentOptions.Center, 0f);
            label.text = text;
            BuildPasarScene.Stretch(label.rectTransform);
            return go.GetComponent<Button>();
        }
    }
}
