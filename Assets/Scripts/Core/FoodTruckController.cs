using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RollingKitchen
{
    /// <summary>
    /// Layar Food Truck (first-person, dimainkan dengan mouse + QTE keyboard).
    /// Alur: ambil bahan dari rak -> masukkan ke alat -> alat memasak (QTE) -> hidangan ke wadah -> sajikan ke pelanggan.
    /// Siang-Sore = mode ringan (pelanggan sedikit, sabar lama, 1 QTE). Malam = mode penuh.
    /// </summary>
    public class FoodTruckController : MonoBehaviour
    {
        public enum ToolState { Idle, Filling, Cooking, Done, Dirty }

        [Serializable]
        public class ToolSlot
        {
            public ToolType tool;
            public Button button;
            public TMP_Text status;
            public Image progress;
            public Image highlight;
            public GameObject qtePrompt;
            public TMP_Text qteText;
            public Button qteButton;

            [NonSerialized] public ToolState state;
            [NonSerialized] public List<IngredientData> contents;
            [NonSerialized] public RecipeData recipe;
            [NonSerialized] public float time;
            [NonSerialized] public float quality;   // 0..1 dari QTE yang berhasil
            [NonSerialized] public int nextQte;
            [NonSerialized] public float qteTimeLeft;
            [NonSerialized] public Key qteKey;
            [NonSerialized] public int failStreak;  // QTE gagal beruntun untuk hidangan ini (2 = gosong)
        }

        [Serializable]
        public class CustomerSlot
        {
            public GameObject root;
            public Button button;
            public TMP_Text nameText;
            public TMP_Text orderText;
            public Image patienceFill;

            [NonSerialized] public bool active;
            [NonSerialized] public CustomerType type;
            [NonSerialized] public RecipeData order;
            [NonSerialized] public float patience;
            [NonSerialized] public float patienceMax;
        }

        private class Dish
        {
            public RecipeData recipe;
            public float quality;
        }

        [Header("Tampilan")]
        [SerializeField] private ToolSlot[] tools;
        [SerializeField] private CustomerSlot[] customers;
        [SerializeField] private Transform[] shelfParents = new Transform[3];
        [SerializeField] private Transform dishParent;
        [SerializeField] private Button listItemPrefab;
        [SerializeField] private Button clothButton;
        [SerializeField] private TMP_Text ticketText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private GameObject summaryPanel;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private Button continueButton;
        [Tooltip("Area ikon skill koki (tombol dibuat dari kode).")]
        [SerializeField] private RectTransform skillArea;

        [Header("Data")]
        [Tooltip("Semua resep (dasar + spesial). Spesial hanya muncul bila sudah terbuka.")]
        [SerializeField] private RecipeData[] allRecipes;

        [Header("Penyetelan")]
        [SerializeField] private float nightSeconds = 100f;
        [SerializeField] private float daySeconds = 60f;
        [SerializeField] private float qteWindow = 1.5f;
        [SerializeField] private int maxDishes = 4;
        [SerializeField] private float skillSeconds = 8f;
        [Tooltip("Tip = harga x (bagian ini x sisa kesabaran + 3% per combo, maks 6) x pengali tipe.")]
        [SerializeField] private float speedTip = 0.15f;
        [SerializeField] private float comboTip = 0.03f;

        // ---- Skill koki: aktif sekali per servis selama skillSeconds ----
        private class Skill
        {
            public string name, desc;
            public bool unlocked, used;
            public float timeLeft;
            public Button button;
            public TMP_Text label;
            public bool Active => timeLeft > 0f;
        }
        private Skill quickHands, calm;

        private static readonly Key[] QteKeys = { Key.Q, Key.W, Key.E, Key.R };
        private static readonly string[] Names = { "Budi", "Sari", "Andi", "Rina", "Dewi", "Joko", "Maya", "Tono" };

        private GameManager gm;
        private readonly List<RecipeData> menu = new List<RecipeData>();
        private readonly List<Dish> dishes = new List<Dish>();
        private readonly List<GameObject> dishButtons = new List<GameObject>();
        private ToolSlot selectedTool;
        private int selectedDish = -1;

        private bool night;
        private int maxCustomers, qteCount;
        private float timeLeft, spawnTimer, spawnInterval, patienceBase, messageTimer;
        private int combo, served, lost, revenue;
        private bool finished;

        // Petunjuk eksperimen dari pelanggan: maks satu per servis, tampil sesudah pesan bayaran.
        private bool hintGiven;
        private string lastHint, queuedMessage;
        private float queuedDelay;
        private const float HintSeconds = 7f;

        private void Start()
        {
            gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogWarning("FoodTruckController: GameManager belum ada di scene.", this);
                enabled = false;
                return;
            }

            night = gm.Phase == GamePhase.Malam;
            // Kesulitan diatur per hari lewat DayConfig; Siang-Sore = mode ringan.
            var cfg = gm.Config;
            maxCustomers = Mathf.Min(customers.Length, night ? cfg.maxCustomers : Mathf.Min(2, cfg.maxCustomers));
            qteCount = night ? 2 : 1;
            timeLeft = night ? nightSeconds : daySeconds;
            spawnInterval = night ? cfg.spawnInterval : cfg.spawnInterval * 1.5f;
            patienceBase = night ? cfg.patienceSeconds : cfg.patienceSeconds * 1.4f;
            spawnTimer = 1.5f;

            // Bisikan petunjuk lebih panjang dari pesan biasa: kecilkan otomatis agar muat satu baris.
            if (messageText)
            {
                messageText.enableAutoSizing = true;
                messageText.fontSizeMin = 14f;
                messageText.fontSizeMax = messageText.fontSize;
            }

            // Hanya untuk uji scene langsung (GameManager "(uji)"): beri stok bila kosong.
            if (gm.name.Contains("(uji)") && !gm.Inventory.Items.Any()) GrantTestStock();

            foreach (var r in allRecipes)
                if (r != null && (!r.isSpecial || gm.Recipes.IsUnlocked(r))) menu.Add(r);

            foreach (var t in tools)
            {
                t.contents = new List<IngredientData>();
                var slot = t;
                t.button.onClick.AddListener(() => OnToolClicked(slot));
                t.qteButton.onClick.AddListener(() => ResolveQte(slot, true));
                t.qtePrompt.SetActive(false);
                if (t.highlight) t.highlight.enabled = false;
            }
            for (int i = 0; i < customers.Length; i++)
            {
                int idx = i;
                customers[i].button.onClick.AddListener(() => OnCustomerClicked(idx));
                customers[i].root.SetActive(false);
            }
            clothButton.onClick.AddListener(OnClothClicked);
            continueButton.onClick.AddListener(Continue);
            summaryPanel.SetActive(false);

            quickHands = new Skill { name = "Tangan Cepat", desc = "masak 2x", unlocked = gm.QuickHandsUnlocked };
            calm = new Skill { name = "Tenang", desc = "kesabaran berhenti", unlocked = gm.CalmUnlocked };
            BuildSkills();

            gm.Inventory.Changed += OnInventoryChanged;
            RebuildShelf();
            RebuildDishes();
            RefreshTools();
            Say(night ? "Malam: sajikan sebanyak mungkin pesanan!" : "Siang-Sore: servis ringan. Selamat berjualan!");
        }

        private void OnDestroy()
        {
            if (gm != null) gm.Inventory.Changed -= OnInventoryChanged;
        }

        private void GrantTestStock()
        {
            foreach (var r in allRecipes)
            {
                if (r == null || r.ingredients == null) continue;
                foreach (var e in r.ingredients) gm.Inventory.Add(e.ingredient, 5);
            }
            Debug.Log("FoodTruck: stok uji diberikan (GameManager uji).");
        }

        // ---------------- Loop ----------------

        private void Update()
        {
            if (finished || PauseMenu.IsPaused) return;

            // Layar ini dimainkan dengan mouse: jangan biarkan tombol terpilih ikut "diklik" oleh Spasi/Enter.
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es && es.currentSelectedGameObject) es.SetSelectedGameObject(null);

            timeLeft -= Time.deltaTime;
            if (timerText) timerText.text = $"Waktu {Mathf.CeilToInt(Mathf.Max(0, timeLeft))}s";
            if (timeLeft <= 0f) { Finish(); return; }

            UpdateSkills();
            UpdateSpawning();
            UpdateCustomers();
            foreach (var t in tools) UpdateTool(t);

            if (messageText && messageTimer > 0f)
            {
                messageTimer -= Time.deltaTime;
                if (messageTimer <= 0f) messageText.text = "";
            }
            if (queuedMessage != null && (queuedDelay -= Time.deltaTime) <= 0f)
            {
                Say(queuedMessage, HintSeconds);
                queuedMessage = null;
            }
            if (comboText) comboText.text = combo > 1 ? $"Combo x{combo}" : "";
        }

        // ---------------- Pelanggan ----------------

        private void UpdateSpawning()
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f || menu.Count == 0) return;

            int active = customers.Count(c => c.active);
            var free = customers.Take(maxCustomers).FirstOrDefault(c => !c.active);
            if (free != null && active < maxCustomers)
            {
                free.active = true;
                free.type = CustomerTypes.Roll(menu.Any(r => r.isSpecial));
                free.order = PickOrder(free.type);
                free.patienceMax = Mathf.Max(8f, patienceBase * CustomerTypes.Patience(free.type));
                free.patience = free.patienceMax;
                string who = Names[UnityEngine.Random.Range(0, Names.Length)];
                free.nameText.text = $"{who} <size=80%><color=#FFD24D>{CustomerTypes.Label(free.type)}</color></size>";
                free.orderText.text = free.order.displayName;
                var body = free.button.GetComponent<Image>();
                if (body) body.color = CustomerTypes.BodyColor(free.type);
                free.root.SetActive(true);
                RefreshTickets();
            }
            spawnTimer = spawnInterval * UnityEngine.Random.Range(0.8f, 1.2f);
        }

        private void UpdateCustomers()
        {
            foreach (var c in customers)
            {
                if (!c.active) continue;
                if (!calm.Active) c.patience -= Time.deltaTime; // skill Tenang menghentikan kesabaran
                c.patienceFill.fillAmount = Mathf.Clamp01(c.patience / c.patienceMax);
                if (c.patience <= 0f)
                {
                    c.active = false;
                    c.root.SetActive(false);
                    lost++;
                    gm.Stats.customersLost++;
                    combo = 0;
                    Say($"{FirstLine(c.nameText.text)} pergi karena kelamaan menunggu.");
                    RefreshTickets();
                }
            }
        }

        private void OnCustomerClicked(int index)
        {
            var c = customers[index];
            if (finished || !c.active) return;
            if (selectedDish < 0) { Say("Pilih hidangan di wadah dulu."); return; }

            var dish = dishes[selectedDish];
            string who = FirstLine(c.nameText.text);
            int basePay = Mathf.RoundToInt(dish.recipe.sellPrice * (0.8f + 0.4f * dish.quality));

            if (dish.recipe != c.order)
            {
                // Salah sajian tetap diterima, tapi dibayar separuh, tanpa tip, dan combo putus.
                int half = Mathf.Max(1, basePay / 2);
                gm.AddRevenue(half);
                gm.Stats.wrongServes++;
                revenue += half;
                served++;
                combo = 0;
                Say($"{who} memesan {c.order.displayName}, bukan {dish.recipe.displayName}. Dibayar separuh: +Rp {half}");
            }
            else
            {
                // Tip = bonus kecepatan (sisa kesabaran) + combo; Royal memberi dua kali lipat.
                float speed = Mathf.Clamp01(c.patience / c.patienceMax);
                int tip = Mathf.RoundToInt(dish.recipe.sellPrice * (speedTip * speed + comboTip * Mathf.Min(combo, 6))
                                           * CustomerTypes.Tip(c.type));
                int pay = basePay + tip;
                gm.AddRevenue(pay);
                gm.Stats.customersServed++;
                gm.Stats.AddDish(dish.recipe);
                revenue += pay;
                served++;
                combo++;
                string tipText = tip > 0 ? $" (tip Rp {tip})" : "";
                Say($"+Rp {pay}{tipText}");

                if (!hintGiven)
                {
                    string hint = ExperimentHints.TryCustomerHint(gm, c.type);
                    if (hint != null)
                    {
                        hintGiven = true;
                        lastHint = hint;
                        queuedMessage = $"{who} berbisik: <color=#FFD24D>{hint}</color>";
                        queuedDelay = 1.5f;
                    }
                }
            }

            dishes.RemoveAt(selectedDish);
            selectedDish = -1;
            c.active = false;
            c.root.SetActive(false);
            RebuildDishes();
            RefreshTickets();
        }

        private void RefreshTickets()
        {
            if (!ticketText) return;
            var lines = customers.Where(c => c.active).Select(c =>
            {
                var items = c.order.ingredients == null ? "" :
                    string.Join(", ", c.order.ingredients.Where(e => e.ingredient != null).Select(e => e.ingredient.displayName));
                return $"{c.order.displayName} ({c.order.tool})\n  {items}";
            });
            ticketText.text = "Pesanan:\n" + string.Join("\n", lines);
        }

        // ---------------- Alat & memasak ----------------

        private void OnToolClicked(ToolSlot slot)
        {
            if (finished) return;
            switch (slot.state)
            {
                case ToolState.Dirty: Say("Alat kotor! Klik Kain untuk membersihkan."); return;
                case ToolState.Cooking: return;
                case ToolState.Done:
                    if (dishes.Count >= maxDishes) { Say("Wadah penuh."); return; }
                    dishes.Add(new Dish { recipe = slot.recipe, quality = slot.quality });
                    slot.state = ToolState.Dirty;
                    slot.recipe = null;
                    slot.contents.Clear();
                    RebuildDishes();
                    break;
                case ToolState.Filling:
                    if (selectedTool == slot)
                    {
                        slot.contents.Clear();
                        slot.state = ToolState.Idle;
                        Say("Bahan dibuang.");
                    }
                    else Select(slot);
                    break;
                default: // Idle
                    Select(slot);
                    break;
            }
            RefreshTools();
        }

        private void Select(ToolSlot slot)
        {
            selectedTool = slot;
            RefreshTools();
        }

        private void AddIngredient(IngredientData item)
        {
            if (finished) return;
            if (selectedTool == null) { Say("Pilih alat dulu (penggorengan, panci, atau talenan)."); return; }
            if (selectedTool.state != ToolState.Idle && selectedTool.state != ToolState.Filling) { Say("Alat ini sedang tidak bisa diisi."); return; }
            if (!gm.Inventory.TryConsume(item)) return;

            selectedTool.contents.Add(item);
            selectedTool.state = ToolState.Filling;

            var match = Match(selectedTool.tool, selectedTool.contents);
            if (match != null)
            {
                selectedTool.state = ToolState.Cooking;
                selectedTool.recipe = match;
                selectedTool.time = 0f;
                selectedTool.quality = 0f;
                selectedTool.nextQte = 0;
                selectedTool.failStreak = 0;
                Say($"Memasak {match.displayName}...");
            }
            RefreshTools();
        }

        private void UpdateTool(ToolSlot t)
        {
            if (t.state != ToolState.Cooking) return;

            t.time += Time.deltaTime * (quickHands.Active ? 2f : 1f); // skill Tangan Cepat
            float total = Mathf.Max(0.5f, t.recipe.cookSeconds * gm.Upgrades.CookMultiplier(t.tool));
            if (t.progress) t.progress.fillAmount = Mathf.Clamp01(t.time / total);

            // Jendela QTE pada pecahan waktu masak yang rata.
            if (!t.qtePrompt.activeSelf && t.nextQte < qteCount && t.time >= total * (t.nextQte + 1f) / (qteCount + 1f))
            {
                t.qteKey = QteKeys[UnityEngine.Random.Range(0, QteKeys.Length)];
                t.qteTimeLeft = qteWindow * gm.Upgrades.QteMultiplier(t.tool);
                t.qteText.text = $"Tekan {t.qteKey}!";
                t.qtePrompt.SetActive(true);
            }

            if (t.qtePrompt.activeSelf)
            {
                t.qteTimeLeft -= Time.deltaTime;
                var kb = Keyboard.current;
                if (kb != null)
                {
                    foreach (var k in QteKeys)
                    {
                        if (!kb[k].wasPressedThisFrame) continue;
                        ResolveQte(t, k == t.qteKey);
                        break;
                    }
                }
                if (t.qtePrompt.activeSelf && t.qteTimeLeft <= 0f) ResolveQte(t, false);
            }

            if (t.state != ToolState.Cooking) return; // gosong saat QTE
            if (t.time >= total)
            {
                if (t.qtePrompt.activeSelf) ResolveQte(t, false);
                if (t.state != ToolState.Cooking) return;
                t.state = ToolState.Done;
                Say($"{t.recipe.displayName} siap! Klik alat untuk memindahkan ke wadah.");
                RefreshTools();
            }
        }

        private void ResolveQte(ToolSlot t, bool success)
        {
            if (!t.qtePrompt.activeSelf) return;
            t.qtePrompt.SetActive(false);
            t.nextQte++;
            if (success) { combo++; t.quality += 1f / qteCount; t.failStreak = 0; return; }

            combo = 0;
            t.failStreak++;
            if (t.failStreak >= 2)
            {
                // Gosong: hidangan dibuang, alat kotor, bahan (sudah terpakai) hilang.
                gm.Stats.burntDishes++;
                Say($"{t.recipe.displayName} GOSONG! Dibuang, alat kotor.");
                t.state = ToolState.Dirty;
                t.recipe = null;
                t.contents.Clear();
                RefreshTools();
                return;
            }
            Say("QTE gagal! Sekali lagi bisa gosong.");
        }

        private void OnClothClicked()
        {
            if (finished) return;
            int cleaned = 0;
            foreach (var t in tools)
                if (t.state == ToolState.Dirty) { t.state = ToolState.Idle; cleaned++; }
            Say(cleaned > 0 ? "Alat dibersihkan." : "Tidak ada yang kotor.");
            RefreshTools();
        }

        private RecipeData Match(ToolType tool, List<IngredientData> contents)
        {
            var have = contents.GroupBy(i => i).ToDictionary(g => g.Key, g => g.Count());
            foreach (var r in menu)
            {
                if (r.tool != tool || r.ingredients == null) continue;
                var need = new Dictionary<IngredientData, int>();
                foreach (var e in r.ingredients)
                {
                    if (e.ingredient == null) continue;
                    need.TryGetValue(e.ingredient, out int n);
                    need[e.ingredient] = n + Mathf.Max(1, e.amount);
                }
                if (need.Count == have.Count && need.All(kv => have.TryGetValue(kv.Key, out int n) && n == kv.Value)) return r;
            }
            return null;
        }

        private void RefreshTools()
        {
            foreach (var t in tools)
            {
                if (t.highlight) t.highlight.enabled = t == selectedTool;
                if (t.progress && t.state != ToolState.Cooking) t.progress.fillAmount = t.state == ToolState.Done ? 1f : 0f;
                if (!t.status) continue;
                switch (t.state)
                {
                    case ToolState.Idle: t.status.text = $"{t.tool}\nKosong"; break;
                    case ToolState.Filling: t.status.text = $"{t.tool}\n" + string.Join(", ", t.contents.Select(c => c.displayName)); break;
                    case ToolState.Cooking: t.status.text = $"{t.tool}\nMemasak {t.recipe.displayName}"; break;
                    case ToolState.Done: t.status.text = $"{t.tool}\nSIAP: {t.recipe.displayName}"; break;
                    default: t.status.text = $"{t.tool}\nKOTOR"; break;
                }
            }
        }

        // ---------------- Rak bahan & wadah ----------------

        private void OnInventoryChanged(IngredientData _, int __) => RebuildShelf();

        private static int ShelfIndex(IngredientCategory c)
        {
            switch (c)
            {
                case IngredientCategory.Sayur: return 0;
                case IngredientCategory.Daging:
                case IngredientCategory.Ikan: return 1;
                default: return 2;
            }
        }

        private void RebuildShelf()
        {
            foreach (var p in shelfParents)
                for (int i = p.childCount - 1; i >= 0; i--) Destroy(p.GetChild(i).gameObject);

            foreach (var kv in gm.Inventory.Items.OrderBy(k => k.Key.displayName))
            {
                var item = kv.Key;
                var parent = shelfParents[Mathf.Min(ShelfIndex(item.category), shelfParents.Length - 1)];
                var b = Instantiate(listItemPrefab, parent);
                b.GetComponentInChildren<TMP_Text>().text = $"{item.displayName}\nx{kv.Value}";
                // Bahan yang basi setelah hari ini: ikon berubah warna sebagai peringatan.
                if (gm.Inventory.CountExpiringAfter(item, gm.Day) > 0) b.GetComponent<Image>().color = new Color(1f, 0.65f, 0.3f, 0.95f);
                b.onClick.AddListener(() => AddIngredient(item));
            }
        }

        private void RebuildDishes()
        {
            foreach (var b in dishButtons) if (b) Destroy(b);
            dishButtons.Clear();
            for (int i = 0; i < dishes.Count; i++)
            {
                int idx = i;
                var b = Instantiate(listItemPrefab, dishParent);
                b.GetComponentInChildren<TMP_Text>().text = (selectedDish == i ? "> " : "") + dishes[i].recipe.displayName;
                b.onClick.AddListener(() => { selectedDish = selectedDish == idx ? -1 : idx; RebuildDishes(); });
                dishButtons.Add(b.gameObject);
            }
        }

        // ---------------- Selesai ----------------

        private void Finish()
        {
            finished = true;
            foreach (var c in customers)
            {
                if (!c.active) continue;
                c.active = false;
                c.root.SetActive(false);
                lost++;
                gm.Stats.customersLost++;
            }
            foreach (var t in tools) t.qtePrompt.SetActive(false);
            quickHands.timeLeft = calm.timeLeft = 0f;
            RefreshSkills();

            summaryText.text = $"{(night ? "Servis Malam" : "Servis Siang-Sore")} selesai!\n\n" +
                               $"Disajikan: {served}\nPelanggan pergi: {lost}\nPendapatan: Rp {revenue:N0}\n" +
                               $"Poin hari ini: {gm.TodayPoints} / target {gm.DailyTarget}" +
                               // Ulangi bisikan pelanggan supaya tidak terlewat bila tertimpa pesan lain.
                               (lastHint != null ? $"\n\n<size=80%>Bisikan pelanggan: <color=#FFD24D>{lastHint}</color></size>" : "");
            summaryPanel.SetActive(true);
        }

        private void Continue()
        {
            // Setelah malam: hari berakhir (AdvancePhase memanggil EndDay) lalu ke Rekap.
            // Setelah siang-sore: maju ke Malam dan kembali ke Rumah.
            gm.AdvancePhase();
            SceneLoader.Load(night ? GameScene.Rekap : GameScene.Rumah);
        }

        // ---------------- Pesanan berbobot karakter ----------------

        /// <summary>
        /// Pilih pesanan dari menu terbuka sesuai tipe: Pemilih minta resep spesial, Terburu-buru suka yang cepat
        /// dimasak, Royal suka yang mahal, Sabar acak. Resep yang bahannya tersedia di rak diberi bobot lebih.
        /// </summary>
        private RecipeData PickOrder(CustomerType type)
        {
            var pool = type == CustomerType.Pemilih && menu.Any(r => r.isSpecial) ? menu.Where(r => r.isSpecial).ToList() : menu;
            var weights = pool.Select(r =>
            {
                float w = 1f;
                if (type == CustomerType.TerburuBuru) w = 10f / Mathf.Max(1f, r.cookSeconds);
                else if (type == CustomerType.Royal) w = Mathf.Max(1f, r.sellPrice) / 40f;
                return CanCook(r) ? w * 3f : w;
            }).ToList();

            float roll = UnityEngine.Random.value * weights.Sum();
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0f) return pool[i];
            }
            return pool[pool.Count - 1];
        }

        private bool CanCook(RecipeData r)
        {
            if (r.ingredients == null) return false;
            foreach (var e in r.ingredients)
                if (e.ingredient != null && !gm.Inventory.Has(e.ingredient, Mathf.Max(1, e.amount))) return false;
            return true;
        }

        // Nama pelanggan tanpa label tipe (label diawali spasi + tag ukuran).
        private static string FirstLine(string s)
        {
            int i = s.IndexOf(" <size", System.StringComparison.Ordinal);
            return i < 0 ? s : s.Substring(0, i);
        }

        // ---------------- Skill koki ----------------

        private void BuildSkills()
        {
            if (!skillArea) return;
            var h = skillArea.GetComponent<HorizontalLayoutGroup>();
            if (!h) h = skillArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = true;
            foreach (var s in new[] { quickHands, calm })
            {
                var skill = s;
                skill.button = RuntimeUi.Button(skillArea, skill.name, RuntimeUi.Blue, () => Activate(skill), 0f, 22f);
                skill.label = RuntimeUi.Label(skill.button);
            }
            RefreshSkills();
        }

        private void UpdateSkills()
        {
            // Spasi memakai skill berikutnya yang belum dipakai (Tangan Cepat dulu, lalu Tenang).
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame)
            {
                var next = new[] { quickHands, calm }.FirstOrDefault(s => s.unlocked && !s.used);
                if (next != null) Activate(next);
            }

            bool changed = false;
            foreach (var s in new[] { quickHands, calm })
            {
                if (!s.Active) continue;
                s.timeLeft -= Time.deltaTime;
                if (s.timeLeft <= 0f) { s.timeLeft = 0f; Say($"{s.name} selesai."); }
                changed = true;
            }
            if (changed) RefreshSkills();
        }

        private void Activate(Skill s)
        {
            if (finished) return;
            if (!s.unlocked) { Say($"{s.name} terbuka mulai hari {(s == quickHands ? GameManager.QuickHandsDay : GameManager.CalmDay)}."); return; }
            if (s.used) { Say($"{s.name} sudah dipakai di servis ini."); return; }
            s.used = true;
            s.timeLeft = skillSeconds;
            Say($"{s.name} aktif: {s.desc} selama {skillSeconds:0} detik!");
            RefreshSkills();
        }

        private void RefreshSkills()
        {
            foreach (var s in new[] { quickHands, calm })
            {
                if (s == null || !s.button) continue;
                string state = !s.unlocked ? $"terbuka hari {(s == quickHands ? GameManager.QuickHandsDay : GameManager.CalmDay)}"
                             : s.Active ? $"AKTIF {Mathf.CeilToInt(s.timeLeft)}s"
                             : s.used ? "sudah dipakai" : $"{s.desc} (Spasi)";
                s.label.text = $"{s.name}\n<size=75%>{state}</size>";
                s.button.interactable = s.unlocked && (!s.used || s.Active); // tetap terang selama aktif
                s.button.GetComponent<Image>().color = s.Active ? new Color(1f, 0.75f, 0.2f) : RuntimeUi.Blue;
            }
        }

        private void Say(string text, float seconds = 3f)
        {
            if (!messageText) return;
            messageText.text = text;
            messageTimer = seconds;
        }
    }
}
