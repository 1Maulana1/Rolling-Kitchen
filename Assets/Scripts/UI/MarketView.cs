using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace RollingKitchen
{
    /// <summary>Panel beli: menampilkan dagangan satu pedagang (zona) dengan harga hari ini.</summary>
    public class MarketView : MonoBehaviour
    {
        [Tooltip("Semua bahan yang ada di game; tiap pedagang menjual subset acak per hari.")]
        [SerializeField] private IngredientData[] catalog;
        [SerializeField] private MarketRowView rowPrefab;
        [SerializeField] private Transform rowParent;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;

        private readonly List<MarketRowView> rows = new List<MarketRowView>();
        private GameManager gm;
        private PasarZone currentZone;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            if (panelRoot) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogWarning("MarketView: GameManager belum ada di scene.", this);
                return;
            }
            gm.MoneyChanged += OnMoneyChanged;
            gm.Inventory.Changed += OnStockChanged;
        }

        private void OnDisable()
        {
            if (gm == null) return;
            gm.MoneyChanged -= OnMoneyChanged;
            gm.Inventory.Changed -= OnStockChanged;
            gm = null;
        }

        /// <summary>Buka panel untuk pedagang di zona ini.</summary>
        public void Open(PasarZone zone)
        {
            if (gm == null || zone == null) return;
            currentZone = zone;

            ClearRows();
            if (titleText)
            {
                string title = zone.Title;
                float disc = Market.DiscountFor(zone.Category, gm.Day);
                if (disc > 0f) title += $"\n<size=70%><color=#FFD24D>Diskon langganan {disc * 100f:0}% hari ini!</color></size>";
                titleText.text = title;
            }
            foreach (var item in Market.StockFor(zone.Category, catalog, gm.Day))
            {
                var row = Instantiate(rowPrefab, rowParent);
                row.Bind(item, Buy);
                rows.Add(row);
            }
            // Petunjuk yang sudah diberikan pedagang ini hari ini tetap tampil bila panel dibuka lagi.
            ShowHint(hintsToday.TryGetValue(zone.Category, out var given) ? given : null);

            panelRoot.SetActive(true);
            GameInput.Locked = true;
            RefreshAll();
        }

        public void Close()
        {
            currentZone = null;
            if (panelRoot) panelRoot.SetActive(false);
            GameInput.Locked = false;
        }

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (IsOpen && kb != null && kb.escapeKey.wasPressedThisFrame) Close();
        }

        private void ClearRows()
        {
            foreach (var r in rows) if (r) Destroy(r.gameObject);
            rows.Clear();
        }

        // Harga & sisa stok ikut diperbarui lewat event Inventory.Changed (dipicu setelah pembelian dicatat).
        private void Buy(IngredientData item)
        {
            if (!Market.TryBuy(item) || currentZone == null) return;

            // Pembelian pertama di lapak ini hari ini: pedagang mungkin membocorkan petunjuk resep spesial.
            string hint = ExperimentHints.TryVendorHint(gm, currentZone.Category);
            if (hint != null)
            {
                hintsToday[currentZone.Category] = hint;
                ShowHint(hint);
            }
        }

        // ---------- Petunjuk pedagang ----------
        private readonly Dictionary<IngredientCategory, string> hintsToday = new Dictionary<IngredientCategory, string>();
        private TMP_Text hintText;

        private void ShowHint(string hint)
        {
            if (hintText == null)
            {
                hintText = RuntimeUi.Text(rowParent, "", 22);
                hintText.name = "Petunjuk Pedagang";
                hintText.alignment = TextAlignmentOptions.Center;
                hintText.color = new Color(1f, 0.82f, 0.3f);
            }
            hintText.transform.SetAsLastSibling(); // di bawah baris dagangan
            hintText.text = hint == null ? "" : $"<color=#CCCCCC>Pedagang:</color> {hint}";
            hintText.gameObject.SetActive(hint != null);
        }

        private void OnMoneyChanged(int _) => RefreshAll();
        private void OnStockChanged(IngredientData _, int __) => RefreshAll();

        private void RefreshAll()
        {
            if (gm == null) return;
            foreach (var row in rows)
            {
                if (!row) continue;
                var item = row.Ingredient;
                int basePrice = Market.GetPrice(item, gm.Day);
                int price = Market.GetSalePrice(item, gm.Day);
                int vendorStock = Market.VendorStock(item, gm.Day);
                row.Refresh(price, basePrice, Market.GetTrend(item, gm.Day), vendorStock, gm.Inventory.Get(item),
                            vendorStock > 0 && gm.Money >= price);
            }
        }

        /// <summary>Tombol "Selesai Belanja": lanjut ke fase berikutnya dan kembali ke Rumah.</summary>
        public void FinishShopping()
        {
            if (gm == null) return;
            gm.AdvancePhase();
            SceneLoader.Load(GameScene.Rumah);
        }
    }
}
