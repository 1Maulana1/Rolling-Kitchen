using System;
using System.Collections.Generic;

namespace RollingKitchen
{
    /// <summary>Stok bahan pemain. Memberi event saat jumlah berubah (untuk HUD / ikon meredup).</summary>
    public class Inventory
    {
        private readonly Dictionary<IngredientData, int> stock = new Dictionary<IngredientData, int>();

        /// <summary>Dipanggil dengan (bahan, jumlah baru).</summary>
        public event Action<IngredientData, int> Changed;

        /// <summary>Semua bahan yang stoknya lebih dari 0.</summary>
        public IEnumerable<KeyValuePair<IngredientData, int>> Items
        {
            get
            {
                foreach (var kv in stock)
                    if (kv.Value > 0) yield return kv;
            }
        }

        public int Get(IngredientData item) => item != null && stock.TryGetValue(item, out int n) ? n : 0;

        public bool Has(IngredientData item, int amount = 1) => Get(item) >= amount;

        public void Add(IngredientData item, int amount)
        {
            if (item == null || amount <= 0) return;
            int now = Get(item) + amount;
            stock[item] = now;
            Changed?.Invoke(item, now);
        }

        /// <summary>Kurangi stok; false (dan tidak berubah) bila stok kurang.</summary>
        public bool TryConsume(IngredientData item, int amount = 1)
        {
            if (item == null || amount <= 0 || !Has(item, amount)) return false;
            int now = Get(item) - amount;
            stock[item] = now;
            Changed?.Invoke(item, now);
            return true;
        }

        public void Clear()
        {
            var keys = new List<IngredientData>(stock.Keys);
            stock.Clear();
            foreach (var k in keys) Changed?.Invoke(k, 0);
        }
    }
}
