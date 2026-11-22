using System.Collections.Generic;
using System.Linq;

namespace RollingKitchen
{
    /// <summary>Statistik sepanjang permainan untuk ringkasan akhir (disimpan bersama slot).</summary>
    public class GameStats
    {
        public int totalRevenue;
        public int totalStars;
        public int customersServed;
        public int customersLost;
        public int wrongServes;
        public int burntDishes;
        public int experimentsTried;
        public int experimentsSucceeded;
        public int spoiledUnits;

        /// <summary>Jumlah porsi tersaji per resep (kunci = id aset resep).</summary>
        public readonly Dictionary<string, int> dishes = new Dictionary<string, int>();

        public void AddDish(RecipeData r)
        {
            if (r == null) return;
            string id = ItemDatabase.IdOf(r);
            dishes.TryGetValue(id, out int n);
            dishes[id] = n + 1;
        }

        /// <summary>Nama hidangan terlaris dan jumlahnya, atau ("-", 0).</summary>
        public (string name, int count) BestSeller()
        {
            if (dishes.Count == 0) return ("-", 0);
            var top = dishes.OrderByDescending(kv => kv.Value).First();
            var r = ItemDatabase.Instance != null ? ItemDatabase.Instance.FindRecipe(top.Key) : null;
            return (r != null ? r.displayName : top.Key, top.Value);
        }

        public void Clear()
        {
            totalRevenue = totalStars = customersServed = customersLost = wrongServes = 0;
            burntDishes = experimentsTried = experimentsSucceeded = spoiledUnits = 0;
            dishes.Clear();
        }
    }
}
