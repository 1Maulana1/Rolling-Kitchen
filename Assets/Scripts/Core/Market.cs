using UnityEngine;

namespace RollingKitchen
{
    /// <summary>
    /// Harga pasar harian. Harga tiap bahan = harga dasar x pengali acak yang deterministik
    /// per (bahan, hari), jadi membuka ulang Pasar di hari yang sama memberi harga yang sama
    /// dan tren dibanding kemarin selalu konsisten tanpa perlu menyimpan data.
    /// </summary>
    public static class Market
    {
        // Rentang fluktuasi harga terhadap harga dasar.
        public const float MinMultiplier = 0.7f;
        public const float MaxMultiplier = 1.4f;

        /// <summary>Harga satu unit bahan pada hari tertentu (minimal 1).</summary>
        public static int GetPrice(IngredientData item, int day)
        {
            if (item == null) return 0;
            var rng = new System.Random(Seed(item, day));
            float mult = MinMultiplier + (float)rng.NextDouble() * (MaxMultiplier - MinMultiplier);

            // Pengali musim dari pengaturan hari (bila ada GameManager).
            var gm = GameManager.Instance;
            if (gm != null) mult *= gm.ConfigFor(day).priceMultiplier;
            return Mathf.Max(1, Mathf.RoundToInt(item.buyPrice * mult));
        }

        /// <summary>+1 naik, -1 turun, 0 tetap/hari pertama, dibanding hari sebelumnya.</summary>
        public static int GetTrend(IngredientData item, int day)
        {
            if (day <= 1) return 0;
            return (int)Mathf.Sign(GetPrice(item, day) - GetPrice(item, day - 1));
        }

        // Jumlah jenis bahan yang dijual satu pedagang per hari.
        public const int StockPerVendor = 4;

        // Diskon langganan: tiap pedagang (kategori) punya peluang ini per hari untuk memotong harga.
        public const float DiscountChance = 0.2f;
        public const float MinDiscount = 0.10f;
        public const float MaxDiscount = 0.20f;

        /// <summary>Potongan harga pedagang kategori ini hari ini (0 bila tidak ada), dibulatkan ke 5%.</summary>
        public static float DiscountFor(IngredientCategory category, int day)
        {
            var rng = new System.Random(day * 6151 + (int)category * 97 + 3);
            if (rng.NextDouble() >= DiscountChance) return 0f;
            float d = MinDiscount + (float)rng.NextDouble() * (MaxDiscount - MinDiscount);
            return Mathf.Round(d * 20f) / 20f;
        }

        /// <summary>Harga yang dibayar pemain: harga hari ini dikurangi diskon langganan pedagangnya.</summary>
        public static int GetSalePrice(IngredientData item, int day)
        {
            int price = GetPrice(item, day);
            if (item == null) return price;
            float d = DiscountFor(item.category, day);
            return d > 0f ? Mathf.Max(1, Mathf.RoundToInt(price * (1f - d))) : price;
        }

        /// <summary>Kategori yang dijual zona tertentu hari ini (penempatan diacak per hari, tiap kategori sekali).</summary>
        public static IngredientCategory CategoryForZone(int zoneIndex, int day)
        {
            var cats = (IngredientCategory[])System.Enum.GetValues(typeof(IngredientCategory));
            var rng = new System.Random(day * 7919 + 13);
            for (int i = cats.Length - 1; i > 0; i--) // Fisher-Yates
            {
                int j = rng.Next(i + 1);
                (cats[i], cats[j]) = (cats[j], cats[i]);
            }
            return cats[Mathf.Abs(zoneIndex) % cats.Length];
        }

        /// <summary>Bahan yang dijual untuk kategori tertentu hari ini: subset acak (deterministik per hari).</summary>
        public static System.Collections.Generic.List<IngredientData> StockFor(IngredientCategory category, IngredientData[] catalog, int day)
        {
            var pool = new System.Collections.Generic.List<IngredientData>();
            foreach (var item in catalog)
                if (item != null && item.category == category) pool.Add(item);

            var rng = new System.Random(day * 104729 + (int)category * 31 + 7);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            if (pool.Count > StockPerVendor) pool.RemoveRange(StockPerVendor, pool.Count - StockPerVendor);
            return pool;
        }

        /// <summary>Beli bahan memakai uang di GameManager. False bila uang kurang.</summary>
        public static bool TryBuy(IngredientData item, int amount = 1)
        {
            var gm = GameManager.Instance;
            if (gm == null || item == null || amount <= 0) return false;

            int cost = GetSalePrice(item, gm.Day) * amount;
            if (gm.Money < cost) return false;

            gm.AddMoney(-cost);
            gm.Inventory.Add(item, amount);
            return true;
        }

        // Hash nama yang stabil antar-sesi (string.GetHashCode tidak stabil).
        private static int Seed(IngredientData item, int day)
        {
            unchecked
            {
                int h = 17;
                foreach (char c in item.name) h = h * 31 + c;
                return h * 7919 + day;
            }
        }
    }
}
