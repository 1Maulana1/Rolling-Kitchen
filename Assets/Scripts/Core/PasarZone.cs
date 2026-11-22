using TMPro;
using UnityEngine;

namespace RollingKitchen
{
    /// <summary>
    /// Satu lapak slot di Pasar. Kategorinya tetap (Market.CategoryForZone); yang berganti tiap hari
    /// hanya jenis bahan, stok, dan harganya. Gambar dagangan di meja dan papan nama mengikuti kategori.
    /// </summary>
    public class PasarZone : MonoBehaviour
    {
        [SerializeField] private int zoneIndex;
        [SerializeField] private TMP_Text label;
        [Tooltip("Gambar dagangan di meja lapak.")]
        [SerializeField] private SpriteRenderer goodsRenderer;
        [Tooltip("Urutan sama dengan IngredientCategory: Sayur, Daging, Ikan, Lainnya.")]
        [SerializeField] private Sprite[] goodsByCategory = new Sprite[4];

        public IngredientCategory Category => Market.CategoryForZone(zoneIndex);

        public string Title => $"Pedagang {CategoryName(Category)}";

        /// <summary>Nama kategori untuk pemain (Lainnya dijual sebagai bumbu & bahan pokok).</summary>
        public static string CategoryName(IngredientCategory c) => c == IngredientCategory.Lainnya ? "Bumbu" : c.ToString();

        private void OnEnable() => Refresh();

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
