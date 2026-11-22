using UnityEngine;

namespace RollingKitchen
{
    /// <summary>Jenis dagangan; tiap pedagang di Pasar menjual satu kategori.</summary>
    public enum IngredientCategory { Sayur, Daging, Ikan, Lainnya }

    /// <summary>Data satu bahan makanan.</summary>
    [CreateAssetMenu(menuName = "Rolling Kitchen/Ingredient", fileName = "NewIngredient")]
    public class IngredientData : ScriptableObject
    {
        public string displayName;
        public Sprite icon;
        public IngredientCategory category;
        [Min(0)] public int buyPrice;
        [Tooltip("Berapa hari bahan bisa dipakai, termasuk hari beli (0 = ikut kategori: ikan/daging 3, sayur 4, lainnya 7).")]
        [Min(0)] public int shelfLifeDays;

        /// <summary>Umur pakai dalam hari. Dibeli hari d, masih bisa dipakai sampai hari d + umur - 1.</summary>
        public int ShelfLife => shelfLifeDays > 0 ? shelfLifeDays : DefaultShelfLife(category);

        public static int DefaultShelfLife(IngredientCategory c)
        {
            switch (c)
            {
                case IngredientCategory.Daging:
                case IngredientCategory.Ikan: return 3;
                case IngredientCategory.Sayur: return 4;
                default: return 7;
            }
        }
    }
}
