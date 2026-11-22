using System;
using System.Collections.Generic;

namespace RollingKitchen
{
    /// <summary>Resep yang sudah dimiliki pemain (mis. Resep Spesial hasil eksperimen).</summary>
    public class RecipeBook
    {
        private readonly HashSet<RecipeData> unlocked = new HashSet<RecipeData>();

        public event Action<RecipeData> Unlocked;

        public IEnumerable<RecipeData> All => unlocked;
        public bool IsUnlocked(RecipeData recipe) => recipe != null && unlocked.Contains(recipe);

        public void Unlock(RecipeData recipe)
        {
            if (recipe != null && unlocked.Add(recipe)) Unlocked?.Invoke(recipe);
        }

        public void Clear() => unlocked.Clear();
    }
}
