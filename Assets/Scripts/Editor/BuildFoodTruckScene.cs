using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RollingKitchen.EditorTools
{
    /// <summary>Membangun layar Food Truck (first-person, UI statis) sesuai sketsa, dan layar Rekap.</summary>
    public static class BuildFoodTruckScene
    {
        private const string ScenePath = "Assets/Scenes/FoodTruck.unity";
        private const string RekapPath = "Assets/Scenes/Rekap.unity";
        private const string RecipeDir = "Assets/Data/Recipes";
        private const string ItemPrefabPath = "Assets/Prefabs/ShelfItem.prefab";

        // Koordinat tata letak = piksel gambar latar 480 x 270 (asal kiri-atas), dikali 4 ke kanvas 1920 x 1080.
        private const string BackgroundPath = "Assets/Art/Backgrounds/bg-food-truck.png";
        private const float Sx = 4f;
        private const float Sy = 4f;

        // nama, harga jual, alat, detik masak, bahan
        private static readonly (string name, int price, ToolType tool, float secs, string[] items)[] BaseRecipes =
        {
            ("Nasi Goreng", 55, ToolType.Penggorengan, 6f, new[] { "Beras", "Telur", "Bumbu" }),
            ("Ayam Goreng", 80, ToolType.Penggorengan, 7f, new[] { "Ayam", "Minyak" }),
            ("Ikan Goreng", 60, ToolType.Penggorengan, 6f, new[] { "Ikan", "Bumbu" }),
            ("Sup Sayur", 35, ToolType.Panci, 5f, new[] { "Sayur", "Wortel" }),
            ("Salad Segar", 45, ToolType.Talenan, 4f, new[] { "Sayur", "Tomat", "Wortel" }),
        };

        [MenuItem("Rolling Kitchen/Build Food Truck")]
        public static void Run()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogError("FoodTruck.unity belum ada. Jalankan Rolling Kitchen > Setup Scenes dulu.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            BuildPasarScene.CreateIngredients();
            BuildPasarScene.CreateSquareSprite();
            CreateBaseRecipes();
            CreateItemPrefab();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var square = AssetDatabase.LoadAssetAtPath<Sprite>(BuildPasarScene.SquarePath);
            var itemPrefab = AssetDatabase.LoadAssetAtPath<Button>(ItemPrefabPath);
            var recipes = LoadRecipes();
            var background = ArtLayout.ImportSprite(BackgroundPath, 25);
            if (square == null || itemPrefab == null || recipes.Length == 0 || background == null)
            {
                Debug.LogError("Gagal memuat sprite, prefab ShelfItem, resep, atau latar Food Truck.");
                return;
            }

            foreach (var c in Object.FindObjectsByType<FoodTruckController>())
                if (c != null) Object.DestroyImmediate(c.gameObject);
            var oldCanvas = GameObject.Find("Canvas");
            if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);

            if (Object.FindAnyObjectByType<GameManager>() == null)
                new GameObject("GameManager (uji)").AddComponent<GameManager>();

            BuildPasarScene.SetupCamera();
            BuildPasarScene.BuildEventSystem();
            BuildUi(square, background, itemPrefab, recipes);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Rolling Kitchen: Food Truck dibangun. Play di scene ini memberi stok uji otomatis.");
        }

        [MenuItem("Rolling Kitchen/Build Rekap")]
        public static void RunRekap()
        {
            if (!File.Exists(RekapPath))
            {
                Debug.LogError("Rekap.unity belum ada. Jalankan Rolling Kitchen > Setup Scenes dulu.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.OpenScene(RekapPath, OpenSceneMode.Single);
            foreach (var v in Object.FindObjectsByType<RekapView>())
                if (v != null) Object.DestroyImmediate(v.GetComponentInParent<Canvas>().gameObject);

            if (Object.FindAnyObjectByType<GameManager>() == null)
                new GameObject("GameManager (uji)").AddComponent<GameManager>();
            BuildPasarScene.SetupCamera();
            BuildPasarScene.BuildEventSystem();

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var root = new GameObject("Rekap", typeof(RectTransform), typeof(RekapView));
            root.transform.SetParent(canvasGo.transform, false);
            BuildPasarScene.Stretch((RectTransform)root.transform);

            var title = BuildPasarScene.MakeText(root.transform, "Judul", 56, TextAlignmentOptions.Center, 0f);
            BuildPasarScene.Anchor(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(1000, 90));
            var body = BuildPasarScene.MakeText(root.transform, "Isi", 36, TextAlignmentOptions.Center, 0f);
            BuildPasarScene.Anchor(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1200, 520));

            var btn = new GameObject("Lanjut", typeof(RectTransform), typeof(Image), typeof(Button));
            btn.transform.SetParent(root.transform, false);
            btn.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.9f);
            BuildPasarScene.Anchor((RectTransform)btn.transform, new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(460, 80));
            var label = BuildPasarScene.MakeText(btn.transform, "Label", 32, TextAlignmentOptions.Center, 0f);
            BuildPasarScene.Stretch(label.rectTransform);

            var view = root.GetComponent<RekapView>();
            var so = new SerializedObject(view);
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("bodyText").objectReferenceValue = body;
            so.FindProperty("continueLabel").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(btn.GetComponent<Button>().onClick, view.Continue);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Rolling Kitchen: Rekap dibangun.");
        }

        // ---------- Aset ----------

        private static void CreateBaseRecipes()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(RecipeDir)) AssetDatabase.CreateFolder("Assets/Data", "Recipes");

            foreach (var def in BaseRecipes)
            {
                string path = $"{RecipeDir}/{def.name}.asset";
                if (AssetDatabase.LoadAssetAtPath<RecipeData>(path) != null) continue;

                var r = ScriptableObject.CreateInstance<RecipeData>();
                r.displayName = def.name;
                r.sellPrice = def.price;
                r.tool = def.tool;
                r.cookSeconds = def.secs;
                r.isSpecial = false;
                var entries = new RecipeData.Entry[def.items.Length];
                for (int i = 0; i < entries.Length; i++)
                {
                    entries[i].ingredient = AssetDatabase.LoadAssetAtPath<IngredientData>($"{BuildPasarScene.DataDir}/{def.items[i]}.asset");
                    entries[i].amount = 1;
                }
                r.ingredients = entries;
                AssetDatabase.CreateAsset(r, path);
            }

            // Resep Spesial contoh: pastikan alat masaknya masuk akal (aset lama dibuat sebelum ada field tool).
            SetTool("Capcay Laut Spesial", ToolType.Panci);
            AssetDatabase.SaveAssets();
        }

        private static void SetTool(string name, ToolType tool)
        {
            var r = AssetDatabase.LoadAssetAtPath<RecipeData>($"{RecipeDir}/{name}.asset");
            if (r == null || r.tool == tool) return;
            r.tool = tool;
            EditorUtility.SetDirty(r);
        }

        private static RecipeData[] LoadRecipes()
        {
            var list = new List<RecipeData>();
            foreach (var guid in AssetDatabase.FindAssets("t:RecipeData", new[] { RecipeDir }))
            {
                var r = AssetDatabase.LoadAssetAtPath<RecipeData>(AssetDatabase.GUIDToAssetPath(guid));
                if (r != null) list.Add(r);
            }
            return list.ToArray();
        }

        private static Button CreateItemPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Button>(ItemPrefabPath);
            if (existing != null) return existing;

            var root = new GameObject("ShelfItem", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            root.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.85f);
            var le = root.GetComponent<LayoutElement>();
            le.minWidth = 70f;
            le.preferredWidth = 110f;
            le.preferredHeight = 70f;
            var label = BuildPasarScene.MakeText(root.transform, "Label", 20, TextAlignmentOptions.Center, 0f);
            label.color = Color.black;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10;
            label.fontSizeMax = 22;
            BuildPasarScene.Stretch(label.rectTransform);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ItemPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<Button>();
        }

        // ---------- UI ----------

        private static void BuildUi(Sprite square, Sprite background, Button itemPrefab, RecipeData[] recipes)
        {
            var canvasGo = BuildPasarScene.CreateCanvasWithHud("Q/W/E/R: QTE   Spasi: skill koki   Esc: jeda", out _);
            var root = canvasGo.transform;

            // Food Truck sudah punya rak sendiri: bar stok HUD tidak perlu.
            var hud = canvasGo.GetComponentInChildren<HudView>();
            var hso = new SerializedObject(hud);
            hso.FindProperty("showStock").boolValue = false;
            hso.ApplyModifiedPropertiesWithoutUndo();

            // Latar belakang truk (pixel art 480x270, direntangkan tepat 4x).
            var bg = Panel(root, square, "Latar", 0, 0, 480, 270, Color.white, false);
            bg.GetComponent<Image>().sprite = background;
            bg.transform.SetAsFirstSibling();

            // Rak bahan: 3 bukaan lemari kiri (Sayur / Daging+Ikan / Lainnya).
            var shelves = new Transform[3];
            var shelfRects = new[] { new Vector4(17, 36, 155, 71), new Vector4(17, 78, 155, 109), new Vector4(17, 116, 155, 147) };
            for (int i = 0; i < 3; i++)
            {
                var s = Panel(root, square, $"Rak {i + 1}", shelfRects[i].x, shelfRects[i].y, shelfRects[i].z, shelfRects[i].w, new Color(0f, 0f, 0f, 0f), false);
                var h = s.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 4f;
                h.padding = new RectOffset(4, 4, 4, 4);
                h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = h.childControlHeight = true;
                h.childForceExpandWidth = false;
                h.childForceExpandHeight = true;
                shelves[i] = s.transform;
            }

            // Pelanggan di jendela (kaca x172-402, y30-190).
            var slots = new[]
            {
                MakeCustomer(root, square, 180, 54, "Pelanggan 1"),
                MakeCustomer(root, square, 257, 46, "Pelanggan 2"),
                MakeCustomer(root, square, 334, 54, "Pelanggan 3"),
            };

            // Tiket pesanan di kertas kanan atas; celemek = Kain pembersih.
            var tickets = Panel(root, square, "Tiket", 407, 18, 478, 100, new Color(1f, 0.97f, 0.82f, 0.93f), false);
            var ticketText = BuildPasarScene.MakeText(tickets.transform, "Teks", 18, TextAlignmentOptions.TopLeft, 0f);
            ticketText.color = Color.black;
            ticketText.enableAutoSizing = true;
            ticketText.fontSizeMin = 12;
            ticketText.fontSizeMax = 18;
            BuildPasarScene.Stretch(ticketText.rectTransform);
            ticketText.rectTransform.offsetMin = new Vector2(8, 8);
            ticketText.rectTransform.offsetMax = new Vector2(-8, -8);
            var cloth = Panel(root, square, "Kain", 412, 104, 454, 153, new Color(1f, 1f, 1f, 0f), true);
            Caption(cloth.transform, "Kain (bersihkan)");

            // Alat masak di meja: dua kompor + talenan di atas laci.
            var toolSlots = new[]
            {
                MakeTool(root, square, ToolType.Penggorengan, 158, 205, 236, 256),
                MakeTool(root, square, ToolType.Panci, 242, 205, 313, 256),
                MakeTool(root, square, ToolType.Talenan, 316, 203, 367, 256),
            };

            // Wadah hidangan: meja baja kanan bawah.
            var wadah = Panel(root, square, "Wadah", 370, 206, 478, 256, new Color(0f, 0f, 0f, 0.25f), false);
            var wh = wadah.AddComponent<HorizontalLayoutGroup>();
            wh.spacing = 4f;
            wh.padding = new RectOffset(6, 6, 6, 6);
            wh.childControlWidth = wh.childControlHeight = true;
            wh.childForceExpandWidth = false;
            wh.childForceExpandHeight = true;
            var wadahLabel = BuildPasarScene.MakeText(root, "Label Wadah", 18, TextAlignmentOptions.BottomLeft, 0f);
            Place(wadahLabel.rectTransform, 370, 196, 478, 205);
            wadahLabel.text = "Wadah: klik hidangan, lalu pelanggan";

            // Skill koki: meja kiri bawah (tombol dibuat FoodTruckController).
            var skillArea = Panel(root, square, "Skill", 6, 212, 152, 252, new Color(0f, 0f, 0f, 0f), false);

            // Teks status.
            var timer = BuildPasarScene.MakeText(root, "Waktu", 30, TextAlignmentOptions.Center, 0f);
            Place(timer.rectTransform, 230, 36, 346, 46);
            var combo = BuildPasarScene.MakeText(root, "Combo", 30, TextAlignmentOptions.Center, 0f);
            combo.color = new Color(1f, 0.9f, 0.3f);
            Place(combo.rectTransform, 172, 170, 402, 180);
            var message = BuildPasarScene.MakeText(root, "Pesan", 26, TextAlignmentOptions.Center, 0f);
            Place(message.rectTransform, 140, 186, 440, 197);
            var mbg = Panel(root, square, "Pesan BG", 140, 186, 440, 197, new Color(0f, 0f, 0f, 0.45f), false);
            mbg.transform.SetSiblingIndex(message.transform.GetSiblingIndex());

            // Ringkasan akhir servis.
            var summary = Panel(root, square, "Ringkasan", 130, 50, 350, 200, new Color(0.05f, 0.05f, 0.08f, 0.97f), false);
            var sText = BuildPasarScene.MakeText(summary.transform, "Teks", 34, TextAlignmentOptions.Center, 0f);
            BuildPasarScene.Stretch(sText.rectTransform);
            sText.rectTransform.offsetMax = new Vector2(0, -20);
            sText.rectTransform.offsetMin = new Vector2(0, 120);
            var cont = Panel(summary.transform, square, "Lanjut", 0, 0, 10, 10, new Color(0.2f, 0.6f, 0.9f), true);
            BuildPasarScene.Anchor((RectTransform)cont.transform, new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(340, 70));
            var cLabel = BuildPasarScene.MakeText(cont.transform, "Label", 30, TextAlignmentOptions.Center, 0f);
            cLabel.text = "Lanjut";
            BuildPasarScene.Stretch(cLabel.rectTransform);

            // Controller.
            var ctrlGo = new GameObject("FoodTruckController", typeof(FoodTruckController));
            var so = new SerializedObject(ctrlGo.GetComponent<FoodTruckController>());

            var ts = so.FindProperty("tools");
            ts.arraySize = toolSlots.Length;
            for (int i = 0; i < toolSlots.Length; i++)
            {
                var p = ts.GetArrayElementAtIndex(i);
                p.FindPropertyRelative("tool").enumValueIndex = (int)toolSlots[i].tool;
                p.FindPropertyRelative("button").objectReferenceValue = toolSlots[i].button;
                p.FindPropertyRelative("status").objectReferenceValue = toolSlots[i].status;
                p.FindPropertyRelative("progress").objectReferenceValue = toolSlots[i].progress;
                p.FindPropertyRelative("highlight").objectReferenceValue = toolSlots[i].highlight;
                p.FindPropertyRelative("qtePrompt").objectReferenceValue = toolSlots[i].qtePrompt;
                p.FindPropertyRelative("qteText").objectReferenceValue = toolSlots[i].qteText;
                p.FindPropertyRelative("qteButton").objectReferenceValue = toolSlots[i].qteButton;
            }
            var cs = so.FindProperty("customers");
            cs.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++)
            {
                var p = cs.GetArrayElementAtIndex(i);
                p.FindPropertyRelative("root").objectReferenceValue = slots[i].root;
                p.FindPropertyRelative("button").objectReferenceValue = slots[i].button;
                p.FindPropertyRelative("nameText").objectReferenceValue = slots[i].nameText;
                p.FindPropertyRelative("orderText").objectReferenceValue = slots[i].orderText;
                p.FindPropertyRelative("patienceFill").objectReferenceValue = slots[i].patienceFill;
            }
            var sh = so.FindProperty("shelfParents");
            sh.arraySize = 3;
            for (int i = 0; i < 3; i++) sh.GetArrayElementAtIndex(i).objectReferenceValue = shelves[i];

            so.FindProperty("dishParent").objectReferenceValue = wadah.transform;
            so.FindProperty("listItemPrefab").objectReferenceValue = itemPrefab;
            so.FindProperty("clothButton").objectReferenceValue = cloth.GetComponent<Button>();
            so.FindProperty("ticketText").objectReferenceValue = ticketText;
            so.FindProperty("timerText").objectReferenceValue = timer;
            so.FindProperty("messageText").objectReferenceValue = message;
            so.FindProperty("comboText").objectReferenceValue = combo;
            so.FindProperty("summaryPanel").objectReferenceValue = summary;
            so.FindProperty("summaryText").objectReferenceValue = sText;
            so.FindProperty("continueButton").objectReferenceValue = cont.GetComponent<Button>();
            so.FindProperty("skillArea").objectReferenceValue = skillArea.transform;
            var rp = so.FindProperty("allRecipes");
            rp.arraySize = recipes.Length;
            for (int i = 0; i < recipes.Length; i++) rp.GetArrayElementAtIndex(i).objectReferenceValue = recipes[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static FoodTruckController.CustomerSlot MakeCustomer(Transform root, Sprite square, float x, float y, string name)
        {
            var slot = new FoodTruckController.CustomerSlot();
            // Satuan = piksel latar (x4 di layar); slot 64 x 134.
            var go = Panel(root, square, name, x, y, x + 64, y + 134, new Color(0, 0, 0, 0), false);
            slot.root = go;

            // Papan info di atas kepala: bar kesabaran + nama/tipe + pesanan.
            var info = Panel(go.transform, square, "Info", 0, 0, 64, 34, new Color(0.05f, 0.05f, 0.08f, 0.75f), false);
            var barBg = Panel(info.transform, square, "Kesabaran BG", 2, 2, 62, 5, new Color(0.15f, 0.15f, 0.15f, 0.9f), false);
            var fill = Panel(barBg.transform, square, "Kesabaran", 0, 0, 60, 3, new Color(0.3f, 0.85f, 0.4f), false);
            BuildPasarScene.Stretch((RectTransform)fill.transform);
            var img = fill.GetComponent<Image>();
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            slot.patienceFill = img;

            slot.nameText = BuildPasarScene.MakeText(info.transform, "Nama", 20, TextAlignmentOptions.Center, 0f);
            slot.nameText.enableAutoSizing = true;
            slot.nameText.fontSizeMin = 12;
            slot.nameText.fontSizeMax = 20;
            Place(slot.nameText.rectTransform, 1, 7, 63, 21);
            slot.orderText = BuildPasarScene.MakeText(info.transform, "Pesanan", 20, TextAlignmentOptions.Center, 0f);
            slot.orderText.color = new Color(1f, 0.95f, 0.5f);
            slot.orderText.enableAutoSizing = true;
            slot.orderText.fontSizeMin = 12;
            slot.orderText.fontSizeMax = 20;
            Place(slot.orderText.rectTransform, 1, 22, 63, 33);

            // Kepala + badan (klik badan untuk menyajikan); warna badan = tipe pelanggan.
            Panel(go.transform, square, "Kepala", 22, 40, 42, 60, new Color(0.9f, 0.75f, 0.6f), false);
            var body = Panel(go.transform, square, "Badan", 16, 61, 48, 134, new Color(0.35f, 0.5f, 0.8f), true);
            slot.button = body.GetComponent<Button>();
            return slot;
        }

        private static FoodTruckController.ToolSlot MakeTool(Transform root, Sprite square, ToolType tool, float x1, float y1, float x2, float y2)
        {
            var slot = new FoodTruckController.ToolSlot { tool = tool };
            // Tombol transparan di atas gambar alat (Image alpha 0 tetap menerima klik).
            var go = Panel(root, square, tool.ToString(), x1, y1, x2, y2, new Color(1f, 1f, 1f, 0f), true);
            slot.button = go.GetComponent<Button>();

            var hl = Panel(go.transform, square, "Sorotan", 0, 0, 10, 10, new Color(1f, 0.9f, 0.2f, 0.3f), false);
            BuildPasarScene.Stretch((RectTransform)hl.transform);
            hl.GetComponent<Image>().raycastTarget = false;
            slot.highlight = hl.GetComponent<Image>();

            var prog = Panel(go.transform, square, "Progres", 0, 0, 10, 10, new Color(0.3f, 0.85f, 0.4f, 0.8f), false);
            var pr = (RectTransform)prog.transform;
            pr.anchorMin = new Vector2(0, 0);
            pr.anchorMax = new Vector2(1, 0);
            pr.pivot = new Vector2(0.5f, 0);
            pr.offsetMin = new Vector2(0, 0);
            pr.offsetMax = new Vector2(0, 14);
            var pi = prog.GetComponent<Image>();
            pi.type = Image.Type.Filled;
            pi.fillMethod = Image.FillMethod.Horizontal;
            pi.fillAmount = 0f;
            pi.raycastTarget = false;
            slot.progress = pi;

            // Status di pita gelap bawah alat agar terbaca di atas gambar.
            var strip = Panel(go.transform, square, "Pita Status", 0, 0, 10, 10, new Color(0f, 0f, 0f, 0.6f), false);
            var srt = (RectTransform)strip.transform;
            srt.anchorMin = new Vector2(0, 0);
            srt.anchorMax = new Vector2(1, 0);
            srt.pivot = new Vector2(0.5f, 0);
            srt.offsetMin = new Vector2(0, 14);
            srt.offsetMax = new Vector2(0, 70);
            slot.status = BuildPasarScene.MakeText(strip.transform, "Status", 18, TextAlignmentOptions.Center, 0f);
            slot.status.enableAutoSizing = true;
            slot.status.fontSizeMin = 12;
            slot.status.fontSizeMax = 18;
            BuildPasarScene.Stretch(slot.status.rectTransform);

            // Prompt QTE di atas alat (tombol, bisa diklik sebagai pengganti keyboard).
            var qte = Panel(go.transform, square, "QTE", 0, 0, 10, 10, new Color(0.95f, 0.3f, 0.2f), true);
            BuildPasarScene.Anchor((RectTransform)qte.transform, new Vector2(0.5f, 1), new Vector2(0, 20), new Vector2(200, 54));
            ((RectTransform)qte.transform).pivot = new Vector2(0.5f, 0);
            var qt = BuildPasarScene.MakeText(qte.transform, "Teks", 24, TextAlignmentOptions.Center, 0f);
            BuildPasarScene.Stretch(qt.rectTransform);
            slot.qtePrompt = qte;
            slot.qteText = qt;
            slot.qteButton = qte.GetComponent<Button>();
            return slot;
        }

        // ---------- Util ----------

        // Label kecil di pita gelap bagian bawah sebuah tombol transparan.
        private static void Caption(Transform parent, string text)
        {
            var go = new GameObject("Keterangan", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            go.GetComponent<Image>().raycastTarget = false;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0, 30);
            var t = BuildPasarScene.MakeText(go.transform, "Teks", 18, TextAlignmentOptions.Center, 0f);
            t.text = text;
            BuildPasarScene.Stretch(t.rectTransform);
        }

        // Panel berwarna di posisi sketsa (px kiri-atas ke kanan-bawah); opsional dijadikan tombol.
        private static GameObject Panel(Transform parent, Sprite square, string name, float x1, float y1, float x2, float y2, Color color, bool button)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = square;
            img.color = color;
            img.raycastTarget = button;
            if (button) go.AddComponent<Button>();
            Place((RectTransform)go.transform, x1, y1, x2, y2);
            return go;
        }

        private static void Place(RectTransform rt, float x1, float y1, float x2, float y2)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x1 * Sx, -y1 * Sy);
            rt.sizeDelta = new Vector2((x2 - x1) * Sx, (y2 - y1) * Sy);
        }
    }
}
