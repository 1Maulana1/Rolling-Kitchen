using System;
using System.Collections.Generic;
using UnityEngine;

namespace RollingKitchen
{
    /// <summary>Fase dalam satu hari.</summary>
    public enum GamePhase { Pagi, SiangSore, Malam }

    /// <summary>Hasil akhir permainan.</summary>
    public enum GameOutcome { Berjalan, Bangkrut, Kalah, Menang }

    /// <summary>
    /// Singleton lintas scene: menyimpan hari, fase, uang, dan aturan menang/kalah.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Awake lebih dulu dari view mana pun yang memakai Instance di OnEnable
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Progres")]
        [SerializeField, Range(1, 30)] private int day = 1;
        [SerializeField] private GamePhase phase = GamePhase.Pagi;
        [SerializeField] private int money = 100;

        [Header("Aturan")]
        [SerializeField, Min(1)] private int totalDays = 30;
        [SerializeField, Min(0)] private int dailyTarget = 100;
        [SerializeField, Min(0)] private int finalTarget = 3000;
        [SerializeField, Min(0)] private int failedDays = 0;
        [SerializeField, Min(0)] private int maxFailedDays = 5;

        [Header("Ambang bintang (kelipatan target harian)")]
        [SerializeField] private float star1Threshold = 1.0f;
        [SerializeField] private float star2Threshold = 1.25f;
        [SerializeField] private float star3Threshold = 1.5f;

        // Uang di awal hari, dipakai untuk menghitung pendapatan hari ini.
        private int moneyAtDayStart;

        public int Day => day;
        public GamePhase Phase => phase;
        public int Money => money;
        public int DailyTarget => dailyTarget;
        public int FinalTarget => finalTarget;
        public int FailedDays => failedDays;
        public int MaxFailedDays => maxFailedDays;
        public int TotalDays => totalDays;
        public int TodayEarnings => money - moneyAtDayStart;
        /// <summary>Stok bahan pemain.</summary>
        public Inventory Inventory { get; } = new Inventory();
        /// <summary>Resep yang sudah terbuka.</summary>
        public RecipeBook Recipes { get; } = new RecipeBook();
        /// <summary>Upgrade alat masak (Toko Alat di Pasar).</summary>
        public ToolUpgrades Upgrades { get; } = new ToolUpgrades();
        /// <summary>Statistik sepanjang permainan (ringkasan akhir).</summary>
        public GameStats Stats { get; } = new GameStats();
        /// <summary>Cutscene pembuka sudah ditonton di slot ini.</summary>
        public bool IntroSeen { get; set; }
        /// <summary>Bahan yang basi dan dibuang pagi ini (ditampilkan di Rumah).</summary>
        public List<KeyValuePair<IngredientData, int>> SpoiledToday { get; private set; } = new List<KeyValuePair<IngredientData, int>>();

        /// <summary>Skill koki terbuka mulai hari tertentu.</summary>
        public const int QuickHandsDay = 4, CalmDay = 6;
        public bool QuickHandsUnlocked => day >= QuickHandsDay;
        public bool CalmUnlocked => day >= CalmDay;
        [Header("Level")]
        [SerializeField] private LevelSet levels;

        /// <summary>Pengaturan hari ini.</summary>
        public DayConfig Config => ConfigFor(day);

        /// <summary>Pengaturan hari tertentu (hari di luar rentang dijepit ke hari pertama/terakhir).</summary>
        public DayConfig ConfigFor(int dayNumber)
        {
            if (levels == null || levels.days == null || levels.days.Length == 0) return DayConfig.Default;
            return levels.days[Mathf.Clamp(dayNumber - 1, 0, levels.days.Length - 1)];
        }

        // Terapkan target harian dari data level (bila ada).
        private void ApplyDayConfig()
        {
            if (levels != null && levels.days != null && levels.days.Length > 0) dailyTarget = Config.dailyTarget;
        }

        public GameOutcome Outcome { get; private set; } = GameOutcome.Berjalan;
        /// <summary>Bintang hari terakhir yang diselesaikan (0-3).</summary>
        public int LastDayStars { get; private set; }

        public event Action<int> DayChanged;
        public event Action<GamePhase> PhaseChanged;
        public event Action<int> MoneyChanged;
        public event Action<GameOutcome> GameEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (levels == null) levels = Resources.Load<LevelSet>("Levels");
            if (levels != null && levels.days != null && levels.days.Length > 0)
            {
                totalDays = levels.days.Length;
                finalTarget = levels.finalTarget;
                maxFailedDays = levels.maxFailedDays;
                money = levels.startingMoney;
            }
            else Debug.LogWarning("GameManager: Resources/Levels belum ada (menu Rolling Kitchen > Create Level Data); memakai nilai Inspector.", this);

            ApplyDayConfig();
            moneyAtDayStart = money;
            Inventory.Today = day;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // Pendapatan penjualan hari ini (tidak termasuk pengeluaran belanja).
        private int revenueToday;
        public int TodayRevenue => revenueToday;
        public int LastDayNumber { get; private set; }
        public int LastDayRevenue { get; private set; }
        public int LastDayNet { get; private set; }

        [Header("Poin (dasar bintang)")]
        [Tooltip("Bonus poin per hidangan yang semua QTE-nya berhasil.")]
        [SerializeField] private int perfectDishBonus = 5;
        [Tooltip("Penalti poin per QTE gagal, mulai gagal beruntun ke-2.")]
        [SerializeField] private int failStreakPenalty = 5;
        [Tooltip("Bonus uang saat bintang 3, sebagai pecahan pendapatan penjualan hari itu.")]
        [SerializeField, Range(0f, 1f)] private float threeStarBonus = 0.1f;

        // Poin = pendapatan + bonus kualitas - penalti gagal beruntun. Bintang dihitung dari poin.
        private int pointsToday;
        private int qteFailStreak;
        public int TodayPoints => pointsToday;
        public int LastDayPoints { get; private set; }
        public int LastDayBonus { get; private set; }
        public event Action<int> PointsChanged;

        /// <summary>Tambah uang dari penjualan; dihitung ke pendapatan dan poin harian.</summary>
        public void AddRevenue(int amount)
        {
            if (amount <= 0) return;
            revenueToday += amount;
            Stats.totalRevenue += amount;
            AddMoney(amount);
            AddPoints(amount);
        }

        /// <summary>Hidangan tersaji; bila semua QTE-nya berhasil, dapat bonus poin. Mengembalikan bonusnya.</summary>
        public int RegisterServedDish(bool perfect)
        {
            if (!perfect) return 0;
            AddPoints(perfectDishBonus);
            return perfectDishBonus;
        }

        /// <summary>Catat hasil satu QTE. Gagal beruntun ke-2 dan seterusnya mengurangi poin. Mengembalikan perubahan poin.</summary>
        public int RegisterQte(bool success)
        {
            if (success) { qteFailStreak = 0; return 0; }
            qteFailStreak++;
            if (qteFailStreak < 2) return 0;
            AddPoints(-failStreakPenalty);
            return -failStreakPenalty;
        }

        private void AddPoints(int amount)
        {
            if (amount == 0) return;
            pointsToday += amount;
            PointsChanged?.Invoke(pointsToday);
        }

        // ---------- Eksperimen Dapur (pity global) ----------
        [Header("Eksperimen Dapur")]
        [SerializeField, Range(0f, 1f)] private float experimentBaseChance = 0.4f;
        [SerializeField, Range(0f, 1f)] private float experimentPityStep = 0.1f;
        [SerializeField, Range(0f, 1f)] private float experimentMaxChance = 0.9f;
        private int experimentFails;

        /// <summary>Peluang berhasil saat kombinasi benar: 40% + 10% tiap gagal, maks 90%.</summary>
        public float ExperimentChance => Mathf.Min(experimentMaxChance, experimentBaseChance + experimentPityStep * experimentFails);

        /// <summary>Catat hasil eksperimen dengan kombinasi benar: berhasil me-reset pity, gagal menaikkannya.</summary>
        public void RegisterExperiment(bool success) => experimentFails = success ? 0 : experimentFails + 1;

        /// <summary>Tambah (atau kurangi, bila negatif) uang.</summary>
        public void AddMoney(int amount)
        {
            if (amount == 0) return;
            money += amount;
            MoneyChanged?.Invoke(money);
        }

        /// <summary>Pindah ke fase berikutnya; setelah Malam hari berakhir.</summary>
        public void AdvancePhase()
        {
            if (Outcome != GameOutcome.Berjalan) return;

            if (phase == GamePhase.Malam)
            {
                EndDay();
                return;
            }
            phase = phase + 1;
            PhaseChanged?.Invoke(phase);
        }

        /// <summary>Akhiri hari: cek bangkrut, kalah, menang, lalu lanjut ke hari berikutnya.</summary>
        public void EndDay()
        {
            if (Outcome != GameOutcome.Berjalan) return;

            // Catat ringkasan hari ini untuk layar Rekap (sebelum hari maju).
            LastDayNumber = day;
            LastDayRevenue = revenueToday;
            LastDayPoints = pointsToday;

            // Bintang dihitung dari poin (pendapatan + bonus kualitas - penalti), bukan laba bersih.
            LastDayStars = CalculateStars(pointsToday);
            Stats.totalStars += LastDayStars;
            if (LastDayStars == 0) failedDays++;

            // Bintang 3: bonus uang dari pendapatan penjualan hari ini.
            LastDayBonus = LastDayStars == 3 ? Mathf.RoundToInt(revenueToday * threeStarBonus) : 0;
            if (LastDayBonus > 0) AddMoney(LastDayBonus);
            LastDayNet = TodayEarnings;

            if (money < 0)
            {
                Finish(GameOutcome.Bangkrut);
                return;
            }
            if (failedDays > maxFailedDays)
            {
                Finish(GameOutcome.Kalah);
                return;
            }
            if (day >= totalDays)
            {
                // Menang hanya di hari terakhir bila target akhir tercapai.
                Finish(money >= finalTarget ? GameOutcome.Menang : GameOutcome.Kalah);
                return;
            }

            day++;
            phase = GamePhase.Pagi;
            moneyAtDayStart = money;
            revenueToday = 0;
            Inventory.Today = day;
            // Bahan yang lewat umurnya dibuang saat pagi tiba (peringatannya sudah tampil kemarin).
            SpoiledToday = Inventory.RemoveExpired(day);
            foreach (var s in SpoiledToday) Stats.spoiledUnits += s.Value;
            ResetDailyPoints();
            ApplyDayConfig();
            DayChanged?.Invoke(day);
            PhaseChanged?.Invoke(phase);
        }

        private void ResetDailyPoints()
        {
            pointsToday = 0;
            qteFailStreak = 0;
            PointsChanged?.Invoke(pointsToday);
        }

        /// <summary>Ambang bintang ke-(i+1) sebagai kelipatan target harian (1,0 / 1,25 / 1,5).</summary>
        public float StarThreshold(int i) => i <= 0 ? star1Threshold : i == 1 ? star2Threshold : star3Threshold;

        /// <summary>Hitung bintang (0-3) dari poin hari ini terhadap target harian.</summary>
        public int CalculateStars(int points)
        {
            if (dailyTarget <= 0) return 3;
            float ratio = (float)points / dailyTarget;
            if (ratio >= star3Threshold) return 3;
            if (ratio >= star2Threshold) return 2;
            if (ratio >= star1Threshold) return 1;
            return 0;
        }

        /// <summary>Mulai ulang permainan dari hari pertama.</summary>
        public void ResetGame(int startingMoney = 100)
        {
            day = 1;
            phase = GamePhase.Pagi;
            money = levels != null ? levels.startingMoney : startingMoney;
            failedDays = 0;
            LastDayStars = 0;
            Inventory.Clear();
            Inventory.Today = day;
            Recipes.Clear();
            Upgrades.Clear();
            Stats.Clear();
            IntroSeen = false;
            SpoiledToday.Clear();
            Outcome = GameOutcome.Berjalan;
            moneyAtDayStart = money;
            revenueToday = 0;
            experimentFails = 0;
            LastDayPoints = LastDayBonus = 0;
            ResetDailyPoints();
            ApplyDayConfig();
            DayChanged?.Invoke(day);
            PhaseChanged?.Invoke(phase);
            MoneyChanged?.Invoke(money);
        }

        private void Finish(GameOutcome outcome)
        {
            Outcome = outcome;
            GameEnded?.Invoke(outcome);
        }

        // ---------- Simpan / Muat (3 slot) ----------

        /// <summary>Slot yang sedang dimainkan (0-2), atau -1 bila scene diuji langsung tanpa slot.</summary>
        public int CurrentSlot { get; private set; } = -1;

        /// <summary>Mulai permainan baru di slot tertentu (menimpa isinya) dan langsung simpan.</summary>
        public void NewGame(int slot)
        {
            ResetGame();
            CurrentSlot = slot;
            SaveCurrent();
        }

        /// <summary>Muat slot. False bila slot kosong/rusak.</summary>
        public bool LoadGame(int slot)
        {
            var data = SaveSystem.Read(slot);
            if (data == null) return false;
            ResetGame();
            ApplyState(data);
            CurrentSlot = slot;
            return true;
        }

        /// <summary>Simpan ke slot aktif. False bila tidak ada slot (mode uji) atau permainan sudah selesai.</summary>
        public bool SaveCurrent()
        {
            if (CurrentSlot < 0 || Outcome != GameOutcome.Berjalan) return false;
            SaveSystem.Write(CurrentSlot, CaptureState());
            return true;
        }

        /// <summary>Permainan di slot aktif selesai (menang/kalah): kosongkan slotnya.</summary>
        public void ClearCurrentSlot()
        {
            if (CurrentSlot >= 0) SaveSystem.Delete(CurrentSlot);
            CurrentSlot = -1;
        }

        private SaveData CaptureState()
        {
            var d = new SaveData
            {
                day = day, phase = phase, money = money, moneyAtDayStart = moneyAtDayStart,
                revenueToday = revenueToday, pointsToday = pointsToday,
                failedDays = failedDays, experimentFails = experimentFails, introSeen = IntroSeen,
                totalRevenue = Stats.totalRevenue, totalStars = Stats.totalStars,
                customersServed = Stats.customersServed, customersLost = Stats.customersLost,
                wrongServes = Stats.wrongServes, burntDishes = Stats.burntDishes,
                experimentsTried = Stats.experimentsTried, experimentsSucceeded = Stats.experimentsSucceeded,
                spoiledUnits = Stats.spoiledUnits,
            };
            foreach (var kv in Inventory.Items)
                foreach (var b in Inventory.BatchesOf(kv.Key))
                    d.inventory.Add(new SaveData.ItemCount { id = ItemDatabase.IdOf(kv.Key), count = b.count, day = b.day });
            foreach (var r in Recipes.All) d.recipes.Add(ItemDatabase.IdOf(r));
            foreach (ToolType t in Enum.GetValues(typeof(ToolType)))
                if (Upgrades.Level(t) > 0)
                    d.tools.Add(new SaveData.ToolLevel { tool = t, level = Upgrades.Level(t), perk = Upgrades.Perk(t) });
            foreach (var kv in Stats.dishes) d.dishesServed.Add(new SaveData.ItemCount { id = kv.Key, count = kv.Value });
            return d;
        }

        private void ApplyState(SaveData d)
        {
            day = Mathf.Clamp(d.day, 1, totalDays);
            phase = d.phase;
            money = d.money;
            moneyAtDayStart = d.moneyAtDayStart;
            revenueToday = d.revenueToday;
            pointsToday = d.pointsToday;
            failedDays = d.failedDays;
            experimentFails = d.experimentFails;
            IntroSeen = d.introSeen || d.version < 2 || day > 1; // simpanan lama: jangan putar ulang pembuka
            Inventory.Today = day;

            Stats.totalRevenue = d.totalRevenue;
            Stats.totalStars = d.totalStars;
            Stats.customersServed = d.customersServed;
            Stats.customersLost = d.customersLost;
            Stats.wrongServes = d.wrongServes;
            Stats.burntDishes = d.burntDishes;
            Stats.experimentsTried = d.experimentsTried;
            Stats.experimentsSucceeded = d.experimentsSucceeded;
            Stats.spoiledUnits = d.spoiledUnits;
            if (d.dishesServed != null) foreach (var it in d.dishesServed) Stats.dishes[it.id] = it.count;
            if (d.tools != null) foreach (var t in d.tools) Upgrades.Set(t.tool, t.level, t.perk);

            var db = ItemDatabase.Instance;
            if (db == null) Debug.LogWarning("ItemDatabase belum ada (menu Rolling Kitchen > Update Item Database): stok dan resep tidak dimuat.");
            else
            {
                foreach (var it in d.inventory)
                {
                    var item = db.FindIngredient(it.id);
                    if (item != null) Inventory.Add(item, it.count, it.day > 0 ? it.day : day);
                    else Debug.LogWarning($"Bahan simpanan tidak dikenal: {it.id}");
                }
                foreach (var id in d.recipes)
                {
                    var r = db.FindRecipe(id);
                    if (r != null) Recipes.Unlock(r);
                }
            }

            ApplyDayConfig();
            DayChanged?.Invoke(day);
            PhaseChanged?.Invoke(phase);
            MoneyChanged?.Invoke(money);
            PointsChanged?.Invoke(pointsToday);
        }
    }
}
