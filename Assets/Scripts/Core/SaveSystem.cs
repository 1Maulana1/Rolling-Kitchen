using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RollingKitchen
{
    /// <summary>Isi satu slot simpan (disimpan sebagai JSON).</summary>
    [Serializable]
    public class SaveData
    {
        // Versi 2: hari beli per kelompok stok, upgrade alat, statistik, flag cerita.
        // Simpanan versi 1 tetap bisa dimuat (field baru bernilai bawaan).
        public int version = 2;
        public string savedAt;

        public int day;
        public GamePhase phase;
        public int money;
        public int moneyAtDayStart;
        public int revenueToday;
        public int pointsToday;
        public int failedDays;
        public int experimentFails;
        public bool introSeen;

        public List<ItemCount> inventory = new List<ItemCount>();
        public List<string> recipes = new List<string>();
        public List<ToolLevel> tools = new List<ToolLevel>();
        public List<ItemCount> dishesServed = new List<ItemCount>();
        public List<ItemCount> marketBought = new List<ItemCount>(); // dibeli dari pedagang pada `day` (stok pedagang berkurang)

        public int totalRevenue, totalStars, customersServed, customersLost, wrongServes,
                   burntDishes, experimentsTried, experimentsSucceeded, spoiledUnits;

        [Serializable]
        public struct ItemCount
        {
            public string id;
            public int count;
            public int day; // hari beli (0 = simpanan lama: dianggap dibeli hari ini)
        }

        [Serializable]
        public struct ToolLevel
        {
            public ToolType tool;
            public int level;
            public ToolPerk perk;
        }
    }

    /// <summary>Baca/tulis 3 slot simpan di folder data aplikasi (persistentDataPath).</summary>
    public static class SaveSystem
    {
        public const int SlotCount = 3;

        public static string PathOf(int slot) => Path.Combine(Application.persistentDataPath, $"save_slot{slot + 1}.json");

        public static bool Exists(int slot) => IsValid(slot) && File.Exists(PathOf(slot));

        public static void Write(int slot, SaveData data)
        {
            if (!IsValid(slot) || data == null) return;
            data.savedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            // Tulis ke berkas sementara dulu agar slot lama tidak rusak bila penulisan gagal di tengah.
            string path = PathOf(slot), tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        /// <summary>Isi slot, atau null bila kosong/rusak.</summary>
        public static SaveData Read(int slot)
        {
            if (!Exists(slot)) return null;
            try
            {
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(PathOf(slot)));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Slot {slot + 1} tidak bisa dibaca: {e.Message}");
                return null;
            }
        }

        public static void Delete(int slot)
        {
            if (Exists(slot)) File.Delete(PathOf(slot));
        }

        private static bool IsValid(int slot) => slot >= 0 && slot < SlotCount;
    }
}
