using UnityEngine;

namespace RollingKitchen
{
    /// <summary>Tipe pelanggan: memengaruhi kesabaran, tip, dan pilihan pesanan.</summary>
    public enum CustomerType { Sabar, TerburuBuru, Royal, Pemilih }

    public static class CustomerTypes
    {
        // Peluang muncul (dinormalkan). Pemilih hanya muncul bila ada resep spesial di menu.
        private static readonly float[] SpawnWeight = { 35f, 30f, 15f, 20f };

        public static CustomerType Roll(bool hasSpecial)
        {
            float total = 0f;
            for (int i = 0; i < SpawnWeight.Length; i++)
                if (hasSpecial || i != (int)CustomerType.Pemilih) total += SpawnWeight[i];
            float r = Random.value * total;
            for (int i = 0; i < SpawnWeight.Length; i++)
            {
                if (!hasSpecial && i == (int)CustomerType.Pemilih) continue;
                r -= SpawnWeight[i];
                if (r <= 0f) return (CustomerType)i;
            }
            return CustomerType.Sabar;
        }

        /// <summary>Pengali lama kesabaran.</summary>
        public static float Patience(CustomerType t)
        {
            switch (t)
            {
                case CustomerType.Sabar: return 1.5f;
                case CustomerType.TerburuBuru: return 0.6f;
                default: return 1f;
            }
        }

        /// <summary>Pengali tip (Royal memberi tip paling besar).</summary>
        public static float Tip(CustomerType t) => t == CustomerType.Royal ? 2f : 1f;

        public static string Label(CustomerType t) => t == CustomerType.TerburuBuru ? "Terburu-buru" : t.ToString();

        public static Color BodyColor(CustomerType t)
        {
            switch (t)
            {
                case CustomerType.Sabar: return new Color(0.35f, 0.6f, 0.45f);
                case CustomerType.TerburuBuru: return new Color(0.85f, 0.45f, 0.25f);
                case CustomerType.Royal: return new Color(0.75f, 0.6f, 0.2f);
                default: return new Color(0.55f, 0.4f, 0.75f);
            }
        }
    }
}
