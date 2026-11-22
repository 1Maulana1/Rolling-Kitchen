using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RollingKitchen
{
    /// <summary>
    /// Petunjuk samar resep spesial yang belum terbuka, diucapkan pelanggan (Food Truck) dan pedagang (Pasar).
    /// Hanya kalimat: tidak tercatat di buku resep, pemain harus mengingatnya sendiri.
    /// Nama hidangan disebut, tetapi bahannya diganti julukan samar (lihat Phrases).
    /// </summary>
    public static class ExperimentHints
    {
        // Peluang pelanggan memberi petunjuk setelah pesanannya disajikan benar (maks 1 per servis).
        public const float CustomerChance = 0.25f;
        public const float RoyalChance = 0.5f;

        // Julukan samar per bahan (kunci = displayName). Ubah bebas; bahan baru tanpa entri memakai CategoryPhrase.
        private static readonly Dictionary<string, string> Phrases = new Dictionary<string, string>
        {
            { "Sayur",  "daun hijau yang segar" },
            { "Wortel", "umbi oranye yang renyah" },
            { "Bayam",  "daun lemas yang kaya zat besi" },
            { "Tomat",  "bulatan merah yang berair" },
            { "Ayam",   "yang tiap subuh berkokok" },
            { "Sapi",   "daging dari hewan yang suka melenguh" },
            { "Sosis",  "daging yang dibungkus memanjang" },
            { "Ikan",   "yang pandai berenang di sungai" },
            { "Udang",  "si bungkuk dari laut" },
            { "Cumi",   "yang bertinta dan berlengan banyak" },
            { "Beras",  "butiran putih yang ditanam di sawah" },
            { "Telur",  "yang bulat dari kandang" },
            { "Bumbu",  "racikan harum dari ulekan" },
            { "Minyak", "cairan keemasan yang licin" },
        };

        private static readonly string[] CustomerLines =
        {
            "Dengar-dengar, {dish} yang istimewa itu pakai {item}...",
            "Nenekku dulu bikin {dish} dengan {item}. Enak sekali!",
            "Kalau kamu bikin {dish}, coba tambah {item}.",
            "Temanku bilang rahasia {dish} ada di {item}.",
        };

        private static readonly string[] VendorLines =
        {
            "Koki-koki sering beli {item} buat {dish} istimewa, lho.",
            "Ini cocok sekali untuk {dish}. Rahasianya {item}!",
            "Kemarin ada yang borong {item}, katanya mau bikin {dish}.",
        };

        // Hari terakhir tiap pedagang (kategori) memberi petunjuk: sekali per lapak per hari.
        private static readonly Dictionary<IngredientCategory, int> vendorHintDay = new Dictionary<IngredientCategory, int>();

        /// <summary>Resep spesial yang belum terbuka.</summary>
        public static List<RecipeData> LockedSpecials(GameManager gm)
        {
            var db = ItemDatabase.Instance;
            if (gm == null || db == null || db.recipes == null) return new List<RecipeData>();
            return db.recipes.Where(r => r != null && r.isSpecial && !gm.Recipes.IsUnlocked(r) && r.ingredients != null).ToList();
        }

        /// <summary>
        /// Petunjuk dari pelanggan setelah disajikan benar, atau null bila tidak kali ini.
        /// Royal lebih sering memberi petunjuk.
        /// </summary>
        public static string TryCustomerHint(GameManager gm, CustomerType type)
        {
            float chance = type == CustomerType.Royal ? RoyalChance : CustomerChance;
            if (Random.value > chance) return null;

            var pairs = Candidates(LockedSpecials(gm), null);
            if (pairs.Count == 0) return null;
            var (recipe, item) = pairs[Random.Range(0, pairs.Count)];
            return Fill(CustomerLines[Random.Range(0, CustomerLines.Length)], recipe, item);
        }

        /// <summary>
        /// Petunjuk dari pedagang kategori ini setelah pembelian pertamanya hari ini, atau null bila
        /// sudah memberi hari ini / tidak ada resep spesial terkunci yang memakai dagangannya.
        /// </summary>
        public static string TryVendorHint(GameManager gm, IngredientCategory category)
        {
            if (gm == null) return null;
            if (vendorHintDay.TryGetValue(category, out int d) && d == gm.Day) return null;

            var pairs = Candidates(LockedSpecials(gm), category);
            if (pairs.Count == 0) return null;
            vendorHintDay[category] = gm.Day;

            // Deterministik per hari supaya pedagang tidak "berganti cerita" bila panel dibuka ulang.
            var rng = new System.Random(gm.Day * 7717 + (int)category * 131);
            var (recipe, item) = pairs[rng.Next(pairs.Count)];
            return Fill(VendorLines[rng.Next(VendorLines.Length)], recipe, item);
        }

        /// <summary>Lupakan petunjuk pedagang yang sudah diberikan (permainan baru / muat slot).</summary>
        public static void ResetVendorHints() => vendorHintDay.Clear();

        // Semua pasangan (resep, bahan) yang bisa dibocorkan; bila category diisi, hanya bahan kategori itu.
        private static List<(RecipeData, IngredientData)> Candidates(List<RecipeData> recipes, IngredientCategory? category)
        {
            var list = new List<(RecipeData, IngredientData)>();
            foreach (var r in recipes)
                foreach (var e in r.ingredients)
                    if (e.ingredient != null && (category == null || e.ingredient.category == category))
                        list.Add((r, e.ingredient));
            return list;
        }

        private static string Fill(string template, RecipeData recipe, IngredientData item) =>
            "“" + template.Replace("{dish}", DishName(recipe)).Replace("{item}", Phrase(item)) + "”";

        // "Nasi Goreng Spesial" -> "nasi goreng": nama hidangan tanpa kata "Spesial".
        private static string DishName(RecipeData r)
        {
            string n = r.displayName ?? "";
            int i = n.IndexOf(" Spesial", System.StringComparison.OrdinalIgnoreCase);
            return (i > 0 ? n.Substring(0, i) : n).ToLowerInvariant();
        }

        private static string Phrase(IngredientData item) =>
            item.displayName != null && Phrases.TryGetValue(item.displayName, out var p) ? p : CategoryPhrase(item.category);

        private static string CategoryPhrase(IngredientCategory c) => c switch
        {
            IngredientCategory.Sayur => "sesuatu dari kebun",
            IngredientCategory.Daging => "daging pilihan",
            IngredientCategory.Ikan => "hasil tangkapan nelayan",
            _ => "sesuatu dari dapur nenek",
        };
    }
}
