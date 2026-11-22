using System;
using System.Collections.Generic;

namespace RollingKitchen
{
    /// <summary>
    /// Stok bahan pemain, dicatat per kelompok hari beli supaya bahan bisa basi.
    /// Pemakaian selalu mengambil kelompok tertua dulu (FIFO). Memberi event saat jumlah berubah.
    /// </summary>
    public class Inventory
    {
        /// <summary>Satu kelompok bahan yang dibeli di hari yang sama.</summary>
        public struct Batch
        {
            public int day;
            public int count;
        }

        private readonly Dictionary<IngredientData, List<Batch>> stock = new Dictionary<IngredientData, List<Batch>>();

        /// <summary>Hari berjalan; dipakai sebagai hari beli saat Add tanpa hari. Diatur GameManager.</summary>
        public int Today { get; set; } = 1;

        /// <summary>Dipanggil dengan (bahan, jumlah baru).</summary>
        public event Action<IngredientData, int> Changed;

        /// <summary>Semua bahan yang stoknya lebih dari 0, dengan jumlah totalnya.</summary>
        public IEnumerable<KeyValuePair<IngredientData, int>> Items
        {
            get
            {
                foreach (var kv in stock)
                {
                    int n = Sum(kv.Value);
                    if (n > 0) yield return new KeyValuePair<IngredientData, int>(kv.Key, n);
                }
            }
        }

        /// <summary>Kelompok stok satu bahan (tertua dulu); untuk simpan dan tampilan.</summary>
        public IReadOnlyList<Batch> BatchesOf(IngredientData item) =>
            item != null && stock.TryGetValue(item, out var list) ? list : (IReadOnlyList<Batch>)Array.Empty<Batch>();

        public int Get(IngredientData item) => item != null && stock.TryGetValue(item, out var list) ? Sum(list) : 0;

        public bool Has(IngredientData item, int amount = 1) => Get(item) >= amount;

        public void Add(IngredientData item, int amount) => Add(item, amount, Today);

        /// <summary>Tambah stok dengan hari beli tertentu (dipakai saat memuat simpanan).</summary>
        public void Add(IngredientData item, int amount, int day)
        {
            if (item == null || amount <= 0) return;
            if (!stock.TryGetValue(item, out var list)) stock[item] = list = new List<Batch>();

            int i = list.FindIndex(b => b.day == day);
            if (i >= 0) list[i] = new Batch { day = day, count = list[i].count + amount };
            else
            {
                list.Add(new Batch { day = day, count = amount });
                list.Sort((a, b) => a.day.CompareTo(b.day));
            }
            Changed?.Invoke(item, Sum(list));
        }

        /// <summary>Kurangi stok (kelompok tertua dulu); false (dan tidak berubah) bila stok kurang.</summary>
        public bool TryConsume(IngredientData item, int amount = 1)
        {
            if (item == null || amount <= 0 || !Has(item, amount)) return false;
            var list = stock[item];
            while (amount > 0)
            {
                var b = list[0];
                int take = Math.Min(amount, b.count);
                amount -= take;
                b.count -= take;
                if (b.count <= 0) list.RemoveAt(0);
                else list[0] = b;
            }
            Changed?.Invoke(item, Sum(list));
            return true;
        }

        /// <summary>Hari terakhir bahan dari kelompok ini masih bisa dipakai.</summary>
        public static int LastUsableDay(IngredientData item, int boughtDay) => boughtDay + item.ShelfLife - 1;

        /// <summary>Jumlah unit bahan ini yang basi setelah hari tertentu berakhir (dibuang esok paginya).</summary>
        public int CountExpiringAfter(IngredientData item, int day)
        {
            int n = 0;
            foreach (var b in BatchesOf(item))
                if (LastUsableDay(item, b.day) <= day) n += b.count;
            return n;
        }

        /// <summary>Buang semua kelompok yang sudah lewat umurnya pada hari ini. Mengembalikan (bahan, jumlah) yang dibuang.</summary>
        public List<KeyValuePair<IngredientData, int>> RemoveExpired(int today)
        {
            var removed = new List<KeyValuePair<IngredientData, int>>();
            foreach (var kv in stock)
            {
                int gone = 0;
                kv.Value.RemoveAll(b =>
                {
                    bool old = LastUsableDay(kv.Key, b.day) < today;
                    if (old) gone += b.count;
                    return old;
                });
                if (gone > 0) removed.Add(new KeyValuePair<IngredientData, int>(kv.Key, gone));
            }
            foreach (var r in removed) Changed?.Invoke(r.Key, Get(r.Key));
            return removed;
        }

        public void Clear()
        {
            var keys = new List<IngredientData>(stock.Keys);
            stock.Clear();
            foreach (var k in keys) Changed?.Invoke(k, 0);
        }

        private static int Sum(List<Batch> list)
        {
            int n = 0;
            foreach (var b in list) n += b.count;
            return n;
        }
    }
}
