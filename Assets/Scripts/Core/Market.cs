using System.Collections.Generic;
using UnityEngine;

namespace RollingKitchen
{
    /// <summary>
    /// Pasar harian. Tiap pedagang punya stok terbatas per bahan per hari (acak tapi deterministik per
    /// (bahan, hari), jadi membuka ulang Pasar di hari yang sama memberi angka yang sama). Harga mengikuti
    /// ketersediaan: makin sedikit sisa stok, makin mahal; tiap pembelian mengurangi stok sehingga harga naik.
    /// </summary>
    public static class Market
    {
        // Stok awal pedagang per bahan per hari.
        public const int MinVendorStock = 3;
        public const int MaxVendorStock = 10;

        // Pengali harga terhadap harga dasar: sisa 1 = paling mahal, sisa MaxVendorStock = paling murah.
        public const float ScarceMultiplier = 1.4f;
        public const float PlentyMultiplier = 0.75f;

        /// <summary>Stok yang dibawa pedagang di awal hari.</summary>
        public static int OpeningStock(IngredientData item, int day)
        {
            if (item == null) return 0;
            var rng = new System.Random(Seed(item, day));
            return rng.Next(MinVendorStock, MaxVendorStock + 1);
        }

        /// <summary>Sisa stok pedagang: stok awal dikurangi yang sudah dibeli pemain hari itu.</summary>
        public static int VendorStock(IngredientData item, int day)
        {
            int stock = OpeningStock(item, day);
            var gm = GameManager.Instance;
            if (gm != null && gm.Day == day) stock -= gm.BoughtToday(item);
            return Mathf.Max(0, stock);
        }

        /// <summary>Harga satu unit pada tingkat sisa stok tertentu (minimal 1). Stok 0 dihargai seperti sisa 1.</summary>
        public static int PriceAtStock(IngredientData item, int stock)
        {
            if (item == null) return 0;
            float t = Mathf.InverseLerp(1, MaxVendorStock, Mathf.Max(1, stock));
            float mult = Mathf.Lerp(ScarceMultiplier, PlentyMultiplier, t);
            return Mathf.Max(1, Mathf.RoundToInt(item.buyPrice * mult));
        }

        /// <summary>Harga sekarang (mengikuti sisa stok pedagang), sebelum diskon.</summary>
        public static int GetPrice(IngredientData item, int day) => PriceAtStock(item, VendorStock(item, day));

        /// <summary>+1 naik, -1 turun, 0 tetap/hari pertama: harga buka hari ini dibanding harga buka kemarin.</summary>
        public static int GetTrend(IngredientData item, int day)
        {
            if (day <= 1) return 0;
            int today = PriceAtStock(item, OpeningStock(item, day));
            int yesterday = PriceAtStock(item, OpeningStock(item, day - 1));
            return today > yesterday ? 1 : today < yesterday ? -1 : 0;
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

        /// <summary>Harga yang dibayar pemain: harga sekarang dikurangi diskon langganan pedagangnya.</summary>
        public static int GetSalePrice(IngredientData item, int day)
        {
            int price = GetPrice(item, day);
            if (item == null) return price;
            float d = DiscountFor(item.category, day);
            return d > 0f ? Mathf.Max(1, Mathf.RoundToInt(price * (1f - d))) : price;
        }

        /// <summary>Kategori lapak slot ini. Tetap setiap hari: urutan slot = urutan IngredientCategory.</summary>
        public static IngredientCategory CategoryForZone(int zoneIndex)
        {
            var cats = (IngredientCategory[])System.Enum.GetValues(typeof(IngredientCategory));
            return cats[Mathf.Abs(zoneIndex) % cats.Length];
        }

        /// <summary>Bahan yang dijual untuk kategori tertentu hari ini: subset acak (deterministik per hari).</summary>
        public static List<IngredientData> StockFor(IngredientCategory category, IngredientData[] catalog, int day)
        {
            var pool = new List<IngredientData>();
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

        /// <summary>Beli satu unit bahan. False bila stok pedagang habis atau uang kurang.</summary>
        public static bool TryBuy(IngredientData item)
        {
            var gm = GameManager.Instance;
            if (gm == null || item == null) return false;
            if (VendorStock(item, gm.Day) <= 0) return false;

            int cost = GetSalePrice(item, gm.Day);
            if (gm.Money < cost) return false;

            gm.AddMoney(-cost);
            gm.RecordPurchase(item);
            gm.Inventory.Add(item, 1);
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
