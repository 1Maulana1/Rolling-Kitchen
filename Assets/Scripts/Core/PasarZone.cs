using TMPro;
using UnityEngine;

namespace RollingKitchen
{
    /// <summary>
    /// Satu lapak slot di Pasar. Kategori dagangannya diacak tiap hari oleh Market.CategoryForZone;
    /// gambar dagangan di meja (dan papan nama, bila ada) ikut diperbarui.
    /// </summary>
    public class PasarZone : MonoBehaviour
    {
        [SerializeField] private int zoneIndex;
        [SerializeField] private TMP_Text label;
        [Tooltip("Gambar dagangan di meja lapak.")]
        [SerializeField] private SpriteRenderer goodsRenderer;
        [Tooltip("Urutan sama dengan IngredientCategory: Sayur, Daging, Ikan, Lainnya.")]
        [SerializeField] private Sprite[] goodsByCategory = new Sprite[4];

        public IngredientCategory Category =>
            Market.CategoryForZone(zoneIndex, GameManager.Instance != null ? GameManager.Instance.Day : 1);

        public string Title => $"Pedagang {CategoryName(Category)}";

        /// <summary>Nama kategori untuk pemain (Lainnya dijual sebagai bumbu & bahan pokok).</summary>
        public static string CategoryName(IngredientCategory c) => c == IngredientCategory.Lainnya ? "Bumbu" : c.ToString();

        private void OnEnable()
        {
            if (GameManager.Instance != null) GameManager.Instance.DayChanged += OnDayChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.DayChanged -= OnDayChanged;
        }

        private void OnDayChanged(int _) => Refresh();

        private void Refresh()
        {
            var cat = Category;
            if (label) label.text = CategoryName(cat);
            int i = (int)cat;
            if (goodsRenderer && goodsByCategory != null && i < goodsByCategory.Length)
                goodsRenderer.sprite = goodsByCategory[i];
        }
    }
}
