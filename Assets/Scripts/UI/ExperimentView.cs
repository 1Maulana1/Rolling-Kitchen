using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RollingKitchen
{
    /// <summary>
    /// Eksperimen Dapur: pilih 2-3 bahan dari stok lalu masak. Bila kombinasi cocok dengan Resep Spesial
    /// yang belum terbuka, peluang berhasilnya acak. Bahan terpakai apa pun hasilnya.
    /// Menutup layar setelah memasak memajukan fase ke Malam.
    /// </summary>
    public class ExperimentView : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform itemParent;
        [SerializeField] private Button itemButtonPrefab;
        [SerializeField] private TMP_Text selectedText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Button cookButton;
        [SerializeField] private TMP_Text closeLabel;
        [SerializeField] private RecipeData[] specialRecipes;
        [SerializeField, Min(1)] private int minItems = 2;
        [SerializeField, Min(1)] private int maxItems = 3;

        private readonly List<IngredientData> selected = new List<IngredientData>();
        private readonly List<GameObject> itemButtons = new List<GameObject>();
        private GameManager gm;
        private bool cooked;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            if (panelRoot) panelRoot.SetActive(false);
        }

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (IsOpen && kb != null && kb.escapeKey.wasPressedThisFrame) Close();
        }

        public void Open()
        {
            gm = GameManager.Instance;
            if (gm == null) return;

            cooked = false;
            selected.Clear();
            Rebuild();
            if (resultText) resultText.text = $"Pilih {minItems}-{maxItems} bahan lalu tekan Masak.";
            if (closeLabel) closeLabel.text = "Tutup";
            panelRoot.SetActive(true);
            GameInput.Locked = true;
            UpdateUi();
        }

        /// <summary>Tutup layar. Jika sudah memasak, siang-sore selesai dan fase maju ke Malam.</summary>
        public void Close()
        {
            panelRoot.SetActive(false);
            GameInput.Locked = false;
            if (cooked && gm != null) gm.AdvancePhase();
            cooked = false;
        }

        public void Cook()
        {
            if (cooked || gm == null || selected.Count < minItems) return;

            var picks = selected.ToArray();
            foreach (var p in picks) gm.Inventory.TryConsume(p);

            var match = FindMatch(picks);
            // Peluang berhasil memakai pity global di GameManager (naik tiap gagal dengan kombinasi benar).
            bool success = match != null && Random.value <= gm.ExperimentChance;
            if (match != null) gm.RegisterExperiment(success);

            if (success)
            {
                gm.Recipes.Unlock(match);
                resultText.text = $"Berhasil! Resep Spesial terbuka: {match.displayName}";
            }
            else
            {
                resultText.text = match != null
                    ? "Hampir! Kombinasinya tepat, tapi kali ini gagal. Bahan terbuang.\nPercobaan berikutnya lebih mungkin berhasil."
                    : "Gagal. Bahan terbuang.";
            }

            cooked = true;
            selected.Clear();
            Rebuild();
            if (closeLabel) closeLabel.text = "Selesai";
            UpdateUi();
        }

        // Resep Spesial belum terbuka yang himpunan bahannya persis sama dengan pilihan pemain.
        private RecipeData FindMatch(IngredientData[] picks)
        {
            foreach (var r in specialRecipes)
            {
                if (r == null || gm.Recipes.IsUnlocked(r) || r.ingredients == null) continue;
                var need = r.ingredients.Select(e => e.ingredient).Where(i => i != null).Distinct().ToList();
                if (need.Count == picks.Length && picks.All(need.Contains)) return r;
            }
            return null;
        }

        private void Rebuild()
        {
            foreach (var b in itemButtons) if (b) Destroy(b);
            itemButtons.Clear();

            foreach (var kv in gm.Inventory.Items.OrderBy(k => k.Key.displayName))
            {
                var item = kv.Key;
                var btn = Instantiate(itemButtonPrefab, itemParent);
                btn.onClick.AddListener(() => Toggle(item));
                itemButtons.Add(btn.gameObject);
                var label = btn.GetComponentInChildren<TMP_Text>();
                label.text = Label(item);
                btn.name = item.displayName;
            }
        }

        private string Label(IngredientData item) =>
            $"{(selected.Contains(item) ? "[x] " : "[  ] ")}{item.displayName}  x{gm.Inventory.Get(item)}";

        private void Toggle(IngredientData item)
        {
            if (cooked) return;
            if (!selected.Remove(item))
            {
                if (selected.Count >= maxItems) return;
                selected.Add(item);
            }
            foreach (var b in itemButtons)
            {
                var label = b.GetComponentInChildren<TMP_Text>();
                var it = gm.Inventory.Items.Select(k => k.Key).FirstOrDefault(i => i.displayName == b.name);
                if (it != null) label.text = Label(it);
            }
            UpdateUi();
        }

        private void UpdateUi()
        {
            if (selectedText)
                selectedText.text = selected.Count == 0 ? "Terpilih: -" : "Terpilih: " + string.Join(", ", selected.Select(s => s.displayName));
            if (cookButton) cookButton.interactable = !cooked && selected.Count >= minItems;
        }
    }
}
