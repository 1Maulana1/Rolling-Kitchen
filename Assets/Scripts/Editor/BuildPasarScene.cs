using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RollingKitchen.EditorTools
{
    /// <summary>
    /// Membangun Pasar placeholder top-down: bahan contoh, 4 zona pedagang, pemain, UI beli.
    /// Aset yang sudah ada tidak ditimpa; objek scene hasil build lama dibuang dan dibangun ulang.
    /// </summary>
    public static class BuildPasarScene
    {
        private const string ScenePath = "Assets/Scenes/Pasar.unity";
        internal const string DataDir = "Assets/Data/Ingredients";
        internal const string RowPrefabPath = "Assets/Prefabs/MarketRow.prefab";
        internal const string SquarePath = "Assets/Sprites/Placeholder_Square.png";
        private const string WorldName = "Pasar World";

        // Latar pixel art 960x400 (versi polos: lapak slot tanpa dagangan); 32 px = 1 unit -> dunia 30 x 12.5.
        internal const string BackgroundPath = "Assets/Art/Backgrounds/bg-pasar-tradisional-polos.png";
        private const string GoodsDir = "Assets/Art/Pasar";
        private static readonly ArtLayout Art = new ArtLayout(960, 400, 32);

        private static readonly (string name, int price, IngredientCategory cat)[] Samples =
        {
            ("Sayur", 8, IngredientCategory.Sayur), ("Wortel", 7, IngredientCategory.Sayur),
            ("Bayam", 5, IngredientCategory.Sayur), ("Tomat", 9, IngredientCategory.Sayur),
            ("Ayam", 25, IngredientCategory.Daging), ("Sapi", 40, IngredientCategory.Daging),
            ("Sosis", 18, IngredientCategory.Daging),
            ("Ikan", 22, IngredientCategory.Ikan), ("Udang", 30, IngredientCategory.Ikan),
            ("Cumi", 28, IngredientCategory.Ikan),
            ("Beras", 10, IngredientCategory.Lainnya), ("Telur", 12, IngredientCategory.Lainnya),
            ("Bumbu", 6, IngredientCategory.Lainnya), ("Minyak", 15, IngredientCategory.Lainnya),
        };

        // ----- Denah dari gambar latar (piksel, asal kiri-atas) -----
        // Lapak selebar 112 px; baris A di y=90, baris B di y=222. Empat lapak slot (dagangan berganti tiap hari):
        private const int StallW = 112;
        private static readonly Vector2Int[] SlotStalls =
        {
            new Vector2Int(134, 90), new Vector2Int(694, 90), new Vector2Int(134, 222), new Vector2Int(694, 222),
        };
        // Potongan sprite dagangan relatif ke asal lapak (hasil ekspor generator: offset 5,52 ukuran 102x32).
        private static readonly Vector2 GoodsCenter = new Vector2(5 + 51, 52 + 16);
        private static readonly string[] GoodsFiles = { "dagangan-sayur", "dagangan-daging", "dagangan-ikan", "dagangan-bumbu" };

        // Rintangan (x1,y1,x2,y2): dinding ruko, dua baris lapak kiri/kanan, gerobak, lapak lesehan, kendaraan.
        private static readonly (string name, Vector4 r)[] Solids =
        {
            ("Ruko", new Vector4(0, 0, 960, 86)),
            ("Lapak A kiri", new Vector4(0, 86, 366, 186)), ("Lapak A kanan", new Vector4(574, 86, 960, 186)),
            ("Lapak B kiri", new Vector4(0, 242, 366, 318)), ("Lapak B kanan", new Vector4(574, 242, 960, 318)),
            ("Lesehan sayur", new Vector4(392, 96, 474, 134)), ("Lesehan ikan", new Vector4(482, 104, 564, 142)),
            ("Gerobak bakso", new Vector4(388, 226, 448, 278)), ("Bangku bakso", new Vector4(458, 232, 504, 266)),
            ("Gerobak es teh", new Vector4(514, 238, 562, 280)), ("Gerobak sate", new Vector4(390, 300, 444, 344)),
            ("Gerobak dorong", new Vector4(300, 364, 372, 392)), ("Pikap", new Vector4(596, 338, 738, 392)),
            ("Motor kiri", new Vector4(50, 372, 172, 400)), ("Motor kanan", new Vector4(720, 372, 802, 400)),
        };

        [MenuItem("Rolling Kitchen/Build Pasar UI")]
        public static void Run()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogError("Pasar.unity belum ada. Jalankan Rolling Kitchen > Setup Scenes dulu.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            CreateIngredients();
            CreateSquareSprite();
            CreateRowPrefab();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Membuka scene meng-unload aset yang baru dibuat; muat ulang dari disk agar referensinya valid.
            var catalog = CreateIngredients();
            var rowPrefab = AssetDatabase.LoadAssetAtPath<MarketRowView>(RowPrefabPath);
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
            var background = ArtLayout.ImportSprite(BackgroundPath, Art.Ppu);
            var goods = new Sprite[GoodsFiles.Length];
            for (int i = 0; i < goods.Length; i++) goods[i] = ArtLayout.ImportSprite($"{GoodsDir}/{GoodsFiles[i]}.png", Art.Ppu);
            if (rowPrefab == null || square == null || background == null || System.Array.Exists(goods, g => g == null)
                || System.Array.Exists(catalog, c => c == null))
            {
                Debug.LogError("Gagal memuat bahan, prefab MarketRow, sprite placeholder, latar, atau gambar dagangan.");
                return;
            }

            // Buang hasil build sebelumnya.
            foreach (var v in Object.FindObjectsByType<MarketView>())
            {
                if (v == null) continue;
                var c = v.GetComponentInParent<Canvas>();
                if (c != null) Object.DestroyImmediate(c.gameObject);
            }
            var oldWorld = GameObject.Find(WorldName);
            if (oldWorld != null) Object.DestroyImmediate(oldWorld);

            // GameManager uji agar scene bisa dimainkan langsung (duplikat dihancurkan oleh singleton).
            if (Object.FindAnyObjectByType<GameManager>() == null)
                new GameObject("GameManager (uji)").AddComponent<GameManager>();

            SetupCamera();
            BuildEventSystem();
            var player = BuildWorld(square, background, goods);
            SetupFollowCamera(player.transform, Art.WorldSize);
            BuildCanvas(catalog, rowPrefab, player);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Rolling Kitchen: Pasar dibangun. Tekan Play: WASD gerak, E bicara dengan pedagang.");
        }

        // ---------- Aset ----------

        internal static IngredientData[] CreateIngredients()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(DataDir)) AssetDatabase.CreateFolder("Assets/Data", "Ingredients");

            var list = new IngredientData[Samples.Length];
            for (int i = 0; i < Samples.Length; i++)
            {
                string path = $"{DataDir}/{Samples[i].name}.asset";
                var so = AssetDatabase.LoadAssetAtPath<IngredientData>(path);
                if (so == null)
                {
                    so = ScriptableObject.CreateInstance<IngredientData>();
                    so.displayName = Samples[i].name;
                    so.buyPrice = Samples[i].price;
                    so.category = Samples[i].cat;
                    AssetDatabase.CreateAsset(so, path);
                }
                else if (so.category != Samples[i].cat)
                {
                    so.category = Samples[i].cat; // bahan contoh lama dibuat sebelum ada kategori
                    EditorUtility.SetDirty(so);
                }
                list[i] = so;
            }
            AssetDatabase.SaveAssets();
            return list;
        }

        internal static void CreateSquareSprite()
        {
            if (File.Exists(SquarePath)) return;
            if (!AssetDatabase.IsValidFolder("Assets/Sprites")) AssetDatabase.CreateFolder("Assets", "Sprites");

            var tex = new Texture2D(4, 4);
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            File.WriteAllBytes(SquarePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(SquarePath);

            var imp = (TextureImporter)AssetImporter.GetAtPath(SquarePath);
            imp.textureType = TextureImporterType.Sprite;
            imp.spritePixelsPerUnit = 4f; // 4 piksel = 1 unit -> sprite 1x1
            imp.filterMode = FilterMode.Point;
            imp.SaveAndReimport();
        }

        internal static MarketRowView CreateRowPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MarketRowView>(RowPrefabPath);
            if (existing != null) return existing;

            var root = new GameObject("MarketRow", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            root.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            root.GetComponent<LayoutElement>().preferredHeight = 48f;
            var h = root.GetComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(12, 12, 6, 6);
            h.spacing = 12f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childForceExpandWidth = false;
            h.childControlWidth = true;
            h.childControlHeight = true;

            var nameT = MakeText(root.transform, "Nama", 24, TextAlignmentOptions.Left, 220f);
            var priceT = MakeText(root.transform, "Harga", 24, TextAlignmentOptions.Right, 180f);
            var stockT = MakeText(root.transform, "Stok", 24, TextAlignmentOptions.Center, 80f);

            var btnGo = new GameObject("Beli", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnGo.transform.SetParent(root.transform, false);
            btnGo.GetComponent<Image>().color = new Color(0.85f, 0.2f, 0.2f);
            btnGo.GetComponent<LayoutElement>().preferredWidth = 90f;
            var btnLabel = MakeText(btnGo.transform, "Label", 22, TextAlignmentOptions.Center, 0f);
            btnLabel.text = "Beli";
            Stretch(btnLabel.rectTransform);

            var row = root.AddComponent<MarketRowView>();
            var so = new SerializedObject(row);
            so.FindProperty("nameText").objectReferenceValue = nameT;
            so.FindProperty("priceText").objectReferenceValue = priceT;
            so.FindProperty("stockText").objectReferenceValue = stockT;
            so.FindProperty("buyButton").objectReferenceValue = btnGo.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, RowPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<MarketRowView>();
        }

        // ---------- Dunia ----------

        internal static void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = true;
            cam.orthographicSize = 5.4f; // 19.2 x 10.8 unit = seluruh denah dalam satu layar
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.78f, 0.72f, 0.6f);
        }

        /// <summary>Kamera ortografik setinggi gambar latar yang mengikuti pemain (batas = ukuran latar).</summary>
        internal static void SetupFollowCamera(Transform target, Vector2 worldSize)
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographicSize = worldSize.y * 0.5f;
            var follow = cam.GetComponent<CameraFollow2D>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow2D>();
            var so = new SerializedObject(follow);
            so.FindProperty("target").objectReferenceValue = target;
            so.FindProperty("bounds").rectValue = new Rect(-worldSize * 0.5f, worldSize);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static PlayerController2D BuildWorld(Sprite square, Sprite background, Sprite[] goods)
        {
            var world = new GameObject(WorldName);
            Art.Picture(world.transform, "Latar Pasar", background, Vector2.zero, -10);

            // Rintangan tak terlihat mengikuti gambar, plus tepi peta.
            var solids = new GameObject("Rintangan");
            solids.transform.SetParent(world.transform, false);
            foreach (var (name, r) in Solids) Art.Solid(solids.transform, name, r.x, r.y, r.z, r.w);
            Art.Solid(solids.transform, "Tepi bawah", 0, 400, 960, 420);
            Art.Solid(solids.transform, "Tepi kiri", -20, 0, 0, 400);
            Art.Solid(solids.transform, "Tepi kanan", 960, 0, 980, 400);

            for (int i = 0; i < SlotStalls.Length; i++) BuildZone(world.transform, i, SlotStalls[i], goods);

            // Pemain (MC) masuk dari lorong bawah.
            return MakePlayer(world.transform, square, Art.ToWorld(480, 384), new Vector2(0.55f, 0.85f));
        }

        /// <summary>Pemain placeholder: kotak biru dengan collider di kaki agar bisa lewat di depan benda.</summary>
        internal static PlayerController2D MakePlayer(Transform parent, Sprite square, Vector2 pos, Vector2 size)
        {
            var mc = MakeBlock(parent, square, "MC", pos, size, new Color(0.2f, 0.45f, 0.95f), 0f, 5, false);
            var col = mc.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.35f); // satuan lokal (sudah dikali scale)
            col.offset = new Vector2(0f, -0.325f);
            var rb = mc.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            return mc.AddComponent<PlayerController2D>();
        }

        private static void BuildZone(Transform parent, int index, Vector2Int stall, Sprite[] goods)
        {
            var zoneGo = new GameObject($"Lapak Slot {index + 1}");
            zoneGo.transform.SetParent(parent, false);
            zoneGo.transform.position = Art.ToWorld(stall.x + StallW * 0.5f, stall.y + 100);
            var zone = zoneGo.AddComponent<PasarZone>();

            // Dagangan di meja (berganti mengikuti kategori hari ini).
            var goodsGo = Art.Picture(zoneGo.transform, "Dagangan", goods[(int)Market.CategoryForZone(index, 1)],
                Art.ToWorld(stall.x + GoodsCenter.x, stall.y + GoodsCenter.y), -9);

            var so = new SerializedObject(zone);
            so.FindProperty("zoneIndex").intValue = index;
            so.FindProperty("goodsRenderer").objectReferenceValue = goodsGo.GetComponent<SpriteRenderer>();
            var arr = so.FindProperty("goodsByCategory");
            arr.arraySize = goods.Length;
            for (int i = 0; i < goods.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = goods[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            // Titik bicara dengan pedagang = depan meja (pedagangnya sendiri sudah tergambar di latar).
            var vendor = zoneGo.AddComponent<Vendor>();
            var vso = new SerializedObject(vendor);
            vso.FindProperty("zone").objectReferenceValue = zone;
            vso.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static GameObject MakeBlock(Transform parent, Sprite sprite, string name, Vector2 pos, Vector2 size, Color color, float rotZ, int order, bool collider)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, 0, rotZ);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            if (collider) go.AddComponent<BoxCollider2D>(); // ukuran mengikuti sprite 1x1 x scale
            return go;
        }

        internal static TextMeshPro MakeWorldLabel(Transform parent, Vector2 standSize)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshPro>();
            t.text = "Pedagang";
            t.fontSize = 3.5f;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.black;
            t.sortingOrder = 10;
            // Anak dari objek ber-scale (w,h): bagi ukuran dengan scale agar rect-nya 1.6 unit lebar.
            go.transform.localScale = new Vector3(1f / standSize.x, 1f / standSize.y, 1f);
            ((RectTransform)go.transform).sizeDelta = new Vector2(Mathf.Min(standSize.x, standSize.y) * 0.95f, 1f);
            if (standSize.y > standSize.x) go.transform.localRotation = Quaternion.identity;
            return t;
        }

        internal static void AddWall(Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Wall");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.AddComponent<BoxCollider2D>().size = size;
        }

        // ---------- UI ----------

        internal static void BuildEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        private static void BuildCanvas(IngredientData[] catalog, MarketRowView rowPrefab, PlayerController2D player)
        {
            var canvasGo = CreateCanvasWithHud("WASD / Panah: gerak   E: bicara   Esc: tutup", out var prompt);

            // Panel beli (tersembunyi sampai pemain menekan E di dekat pedagang).
            var uiGo = new GameObject("Pasar UI", typeof(RectTransform), typeof(MarketView));
            uiGo.transform.SetParent(canvasGo.transform, false);
            Stretch((RectTransform)uiGo.transform);
            BuildMarketPanel(canvasGo, uiGo, catalog, rowPrefab, player, prompt);
        }

        /// <summary>Canvas + HUD (hari/fase/uang) + petunjuk + prompt interaksi; dipakai semua scene top-down.</summary>
        internal static GameObject CreateCanvasWithHud(string hintText, out TextMeshProUGUI prompt)
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // HUD: hari, fase, uang (HUD direntangkan penuh agar jangkar pojok anak benar).
            var hud = new GameObject("HUD", typeof(RectTransform), typeof(HudView));
            hud.transform.SetParent(canvasGo.transform, false);
            Stretch((RectTransform)hud.transform);
            var dayT = MakeText(hud.transform, "Hari", 32, TextAlignmentOptions.TopLeft, 0f);
            Anchor(dayT.rectTransform, new Vector2(0, 1), new Vector2(24, -20), new Vector2(360, 50));
            var phaseT = MakeText(hud.transform, "Fase", 28, TextAlignmentOptions.TopLeft, 0f);
            Anchor(phaseT.rectTransform, new Vector2(0, 1), new Vector2(24, -64), new Vector2(360, 44));
            var moneyT = MakeText(hud.transform, "Uang", 32, TextAlignmentOptions.TopRight, 0f);
            Anchor(moneyT.rectTransform, new Vector2(1, 1), new Vector2(-24, -20), new Vector2(360, 50));
            var hudSo = new SerializedObject(hud.GetComponent<HudView>());
            hudSo.FindProperty("dayText").objectReferenceValue = dayT;
            hudSo.FindProperty("phaseText").objectReferenceValue = phaseT;
            hudSo.FindProperty("moneyText").objectReferenceValue = moneyT;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            // Petunjuk dan prompt interaksi.
            var hint = MakeText(canvasGo.transform, "Petunjuk", 22, TextAlignmentOptions.BottomLeft, 0f);
            hint.text = hintText;
            Anchor(hint.rectTransform, new Vector2(0, 0), new Vector2(24, 20), new Vector2(700, 40));
            prompt = MakeText(canvasGo.transform, "Prompt", 30, TextAlignmentOptions.Center, 0f);
            Anchor(prompt.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 90), new Vector2(800, 50));
            prompt.gameObject.SetActive(false);
            return canvasGo;
        }

        private static void BuildMarketPanel(GameObject canvasGo, GameObject uiGo, IngredientData[] catalog, MarketRowView rowPrefab,
            PlayerController2D player, TextMeshProUGUI prompt)
        {
            var panel = new GameObject("Panel Beli", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(uiGo.transform, false);
            panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.92f);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(700, 420);
            var v = panel.GetComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(12, 12, 12, 12);
            v.spacing = 6f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var title = MakeText(panel.transform, "Judul", 34, TextAlignmentOptions.Center, 0f);
            title.text = "Pedagang";
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;

            var close = new GameObject("Tutup", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            close.transform.SetParent(panel.transform, false);
            close.GetComponent<LayoutElement>().ignoreLayout = true;
            close.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.55f);
            Anchor((RectTransform)close.transform, new Vector2(1, 1), new Vector2(-12, -12), new Vector2(44, 44));
            var closeLabel = MakeText(close.transform, "Label", 26, TextAlignmentOptions.Center, 0f);
            closeLabel.text = "X";
            Stretch(closeLabel.rectTransform);

            // Tombol selesai belanja.
            var finish = new GameObject("Selesai Belanja", typeof(RectTransform), typeof(Image), typeof(Button));
            finish.transform.SetParent(canvasGo.transform, false);
            finish.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.9f);
            Anchor((RectTransform)finish.transform, new Vector2(1, 0), new Vector2(-24, 24), new Vector2(300, 60));
            var fl = MakeText(finish.transform, "Label", 26, TextAlignmentOptions.Center, 0f);
            fl.text = "Selesai Belanja";
            Stretch(fl.rectTransform);

            var view = uiGo.GetComponent<MarketView>();
            var vso = new SerializedObject(view);
            var cat = vso.FindProperty("catalog");
            cat.arraySize = catalog.Length;
            for (int i = 0; i < catalog.Length; i++) cat.GetArrayElementAtIndex(i).objectReferenceValue = catalog[i];
            vso.FindProperty("rowPrefab").objectReferenceValue = rowPrefab;
            vso.FindProperty("rowParent").objectReferenceValue = panel.transform;
            vso.FindProperty("panelRoot").objectReferenceValue = panel;
            vso.FindProperty("titleText").objectReferenceValue = title;
            vso.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(close.GetComponent<Button>().onClick, view.Close);
            UnityEventTools.AddPersistentListener(finish.GetComponent<Button>().onClick, view.FinishShopping);

            var pso = new SerializedObject(player);
            pso.FindProperty("promptText").objectReferenceValue = prompt;
            pso.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static TextMeshProUGUI MakeText(Transform parent, string name, float size, TextAlignmentOptions align, float width)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            t.raycastTarget = false;
            if (width > 0f) go.AddComponent<LayoutElement>().preferredWidth = width;
            return t;
        }

        internal static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        internal static void Anchor(RectTransform rt, Vector2 corner, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = corner;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}
