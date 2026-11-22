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
        [Tooltip("Hari sebelum bahan basi (0 = tidak basi).")]
        [Min(0)] public int shelfLifeDays;
    }
}
