using UnityEngine;

namespace RollingKitchen
{
    /// <summary>Data satu tipe pelanggan.</summary>
    [CreateAssetMenu(menuName = "Rolling Kitchen/Customer", fileName = "NewCustomer")]
    public class CustomerData : ScriptableObject
    {
        public string displayName;
        public Sprite portrait;
        [Tooltip("Lama sabar menunggu pesanan (detik).")]
        [Min(1f)] public float patienceSeconds = 30f;
        [Tooltip("Pengali bayaran/tip.")]
        [Min(0f)] public float tipMultiplier = 1f;
        public RecipeData[] favoriteRecipes;
    }
}
