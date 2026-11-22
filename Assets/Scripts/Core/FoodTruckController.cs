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

        [Header("Data")]
        [Tooltip("Semua resep (dasar + spesial). Spesial hanya muncul bila sudah terbuka.")]
        [SerializeField] private RecipeData[] allRecipes;

        [Header("Penyetelan")]
        [SerializeField] private float nightSeconds = 100f;
        [SerializeField] private float daySeconds = 60f;
        [SerializeField] private float qteWindow = 1.5f;
        [SerializeField] private int maxDishes = 4;

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

            timeLeft -= Time.deltaTime;
            if (timerText) timerText.text = $"Waktu {Mathf.CeilToInt(Mathf.Max(0, timeLeft))}s";
            if (timeLeft <= 0f) { Finish(); return; }

            UpdateSpawning();
            UpdateCustomers();
            foreach (var t in tools) UpdateTool(t);

            if (messageText && messageTimer > 0f)
            {
                messageTimer -= Time.deltaTime;
                if (messageTimer <= 0f) messageText.text = "";
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
                free.order = menu[UnityEngine.Random.Range(0, menu.Count)];
                free.patienceMax = Mathf.Max(10f, patienceBase);
                free.patience = free.patienceMax;
                free.nameText.text = Names[UnityEngine.Random.Range(0, Names.Length)];
                free.orderText.text = free.order.displayName;
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
                c.patience -= Time.deltaTime;
                c.patienceFill.fillAmount = Mathf.Clamp01(c.patience / c.patienceMax);
                if (c.patience <= 0f)
                {
                    c.active = false;
                    c.root.SetActive(false);
                    lost++;
                    combo = 0;
                    Say($"{c.nameText.text} pergi karena kelamaan menunggu.");
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
            if (dish.recipe != c.order) { Say($"{c.nameText.text} tidak memesan {dish.recipe.displayName}."); return; }

            float patienceBonus = c.patience / c.patienceMax > 0.5f ? 1.1f : 1f;
            float comboBonus = 1f + 0.05f * Mathf.Min(combo, 6);
            int pay = Mathf.RoundToInt(dish.recipe.sellPrice * (0.8f + 0.4f * dish.quality) * comboBonus * patienceBonus);

            gm.AddRevenue(pay);
            int bonus = gm.RegisterServedDish(dish.quality >= 0.999f);
            revenue += pay;
            served++;
            combo++;
            Say(bonus > 0 ? $"+Rp {pay}  Sempurna! +{bonus} poin" : $"+Rp {pay}");

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
                Say($"Memasak {match.displayName}...");
            }
            RefreshTools();
        }

        private void UpdateTool(ToolSlot t)
        {
            if (t.state != ToolState.Cooking) return;

            t.time += Time.deltaTime;
            float total = Mathf.Max(0.5f, t.recipe.cookSeconds);
            if (t.progress) t.progress.fillAmount = Mathf.Clamp01(t.time / total);

            // Jendela QTE pada pecahan waktu masak yang rata.
            if (!t.qtePrompt.activeSelf && t.nextQte < qteCount && t.time >= total * (t.nextQte + 1f) / (qteCount + 1f))
            {
                t.qteKey = QteKeys[UnityEngine.Random.Range(0, QteKeys.Length)];
                t.qteTimeLeft = qteWindow;
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

            if (t.state == ToolState.Cooking && t.time >= total)
            {
                if (t.qtePrompt.activeSelf) ResolveQte(t, false);
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
            int delta = gm.RegisterQte(success);
            if (success) { combo++; t.quality += 1f / qteCount; }
            else { combo = 0; Say(delta < 0 ? $"QTE gagal beruntun! {delta} poin" : "QTE gagal!"); }
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
            }
            foreach (var t in tools) t.qtePrompt.SetActive(false);

            summaryText.text = $"{(night ? "Servis Malam" : "Servis Siang-Sore")} selesai!\n\n" +
                               $"Disajikan: {served}\nPelanggan pergi: {lost}\nPendapatan: Rp {revenue:N0}\n" +
                               $"Poin hari ini: {gm.TodayPoints} / target {gm.DailyTarget}";
            summaryPanel.SetActive(true);
        }

        private void Continue()
        {
            // Setelah malam: hari berakhir (AdvancePhase memanggil EndDay) lalu ke Rekap.
            // Setelah siang-sore: maju ke Malam dan kembali ke Rumah.
            gm.AdvancePhase();
            SceneLoader.Load(night ? GameScene.Rekap : GameScene.Rumah);
        }

        private void Say(string text)
        {
            if (!messageText) return;
            messageText.text = text;
            messageTimer = 3f;
        }
    }
}
