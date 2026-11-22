using System;
using UnityEngine;

namespace RollingKitchen
{
    /// <summary>Pengaturan satu hari (satu level).</summary>
    [Serializable]
    public struct DayConfig
    {
        [Min(0)] public int dailyTarget;
        [Tooltip("Apakah Eksperimen Dapur tersedia di hari ini.")]
        public bool allowExperiment;
        [Tooltip("Pelanggan maksimal di jendela saat Malam (1-3).")]
        [Range(1, 3)] public int maxCustomers;
        [Tooltip("Kesabaran dasar pelanggan (detik) saat Malam.")]
        [Min(5f)] public float patienceSeconds;
        [Tooltip("Jeda antar pelanggan baru (detik) saat Malam.")]
        [Min(1f)] public float spawnInterval;

        public static DayConfig Default => new DayConfig
        {
            dailyTarget = 100,
            allowExperiment = true,
            maxCustomers = 3,
            patienceSeconds = 32f,
            spawnInterval = 9f,
        };
    }

    /// <summary>Daftar pengaturan hari + aturan global satu permainan. Dimuat dari Resources/Levels.</summary>
    [CreateAssetMenu(menuName = "Rolling Kitchen/Level Set", fileName = "Levels")]
    public class LevelSet : ScriptableObject
    {
        [Min(0)] public int startingMoney = 100;
        [Min(0)] public int finalTarget = 700;
        [Min(0)] public int maxFailedDays = 2;
        [Tooltip("Satu entri per hari; jumlah entri = jumlah hari permainan.")]
        public DayConfig[] days;
    }
}
