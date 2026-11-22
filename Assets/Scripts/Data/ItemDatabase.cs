using UnityEngine;

namespace RollingKitchen
{
    /// <summary>
    /// Daftar semua bahan dan resep, dipakai sistem simpan untuk mengubah nama aset kembali menjadi data.
    /// Disimpan di Resources/ItemDatabase; diisi otomatis lewat menu Rolling Kitchen > Update Item Database.
    /// </summary>
    [CreateAssetMenu(menuName = "Rolling Kitchen/Item Database", fileName = "ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        public IngredientData[] ingredients;
        public RecipeData[] recipes;

        private static ItemDatabase cached;

        /// <summary>Database dari Resources (null bila belum dibuat).</summary>
        public static ItemDatabase Instance => cached != null ? cached : cached = Resources.Load<ItemDatabase>("ItemDatabase");

        /// <summary>ID simpan = nama aset (stabil selama aset tidak diganti nama).</summary>
        public static string IdOf(Object asset) => asset != null ? asset.name : null;

        public IngredientData FindIngredient(string id)
        {
            if (ingredients != null)
                foreach (var i in ingredients) if (i != null && i.name == id) return i;
            return null;
        }

        public RecipeData FindRecipe(string id)
        {
            if (recipes != null)
                foreach (var r in recipes) if (r != null && r.name == id) return r;
            return null;
        }
    }
}
