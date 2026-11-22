using System;

namespace RollingKitchen
{
    /// <summary>Pilihan upgrade alat di level 1 (level 2 mendapat keduanya).</summary>
    public enum ToolPerk { None, MasakCepat, QteLonggar }

    /// <summary>
    /// Upgrade alat masak, dibeli di Toko Alat (Pasar, pagi). Per alat:
    /// level 1 = salah satu (masak lebih cepat ATAU jendela QTE lebih longgar), level 2 = keduanya.
    /// </summary>
    public class ToolUpgrades
    {
        public const int MaxLevel = 2;
        public static readonly int[] Prices = { 150, 300 }; // harga ke level 1, ke level 2 (sementara)
        public const float FastCookMultiplier = 0.75f;      // waktu masak x0,75
        public const float WideQteMultiplier = 1.5f;        // jendela QTE x1,5

        private static readonly int Count = Enum.GetValues(typeof(ToolType)).Length;
        private readonly int[] levels = new int[Count];
        private readonly ToolPerk[] perks = new ToolPerk[Count];

        public event Action Changed;

        public int Level(ToolType t) => levels[(int)t];
        public ToolPerk Perk(ToolType t) => perks[(int)t];

        public bool FastCook(ToolType t) => Level(t) >= 2 || (Level(t) == 1 && Perk(t) == ToolPerk.MasakCepat);
        public bool WideQte(ToolType t) => Level(t) >= 2 || (Level(t) == 1 && Perk(t) == ToolPerk.QteLonggar);

        public float CookMultiplier(ToolType t) => FastCook(t) ? FastCookMultiplier : 1f;
        public float QteMultiplier(ToolType t) => WideQte(t) ? WideQteMultiplier : 1f;

        /// <summary>Harga ke level berikutnya, atau -1 bila sudah maksimal.</summary>
        public int NextPrice(ToolType t) => Level(t) >= MaxLevel ? -1 : Prices[Level(t)];

        /// <summary>Beli level berikutnya. Perk hanya dipakai saat naik ke level 1. Memotong uang GameManager.</summary>
        public bool TryBuy(GameManager gm, ToolType t, ToolPerk perk)
        {
            int price = NextPrice(t);
            if (gm == null || price < 0 || gm.Money < price) return false;
            if (Level(t) == 0 && perk == ToolPerk.None) return false;

            gm.AddMoney(-price);
            if (Level(t) == 0) perks[(int)t] = perk;
            levels[(int)t]++;
            Changed?.Invoke();
            return true;
        }

        public void Set(ToolType t, int level, ToolPerk perk)
        {
            levels[(int)t] = Math.Max(0, Math.Min(MaxLevel, level));
            perks[(int)t] = perk;
            Changed?.Invoke();
        }

        public void Clear()
        {
            Array.Clear(levels, 0, levels.Length);
            Array.Clear(perks, 0, perks.Length);
            Changed?.Invoke();
        }

        public static string Name(ToolPerk p) => p == ToolPerk.MasakCepat ? "Masak cepat" : p == ToolPerk.QteLonggar ? "QTE longgar" : "-";
    }
}
