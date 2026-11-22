using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RollingKitchen
{
    /// <summary>
    /// Pengaturan pemain (PlayerPrefs): volume musik/efek, resolusi, layar penuh, ukuran teks.
    /// Ukuran teks diterapkan ke semua teks TMP di scene (ukuran asli diingat agar bisa diubah berulang).
    /// </summary>
    public static class GameSettings
    {
        public static readonly float[] TextScales = { 0.85f, 1f, 1.2f };
        public static readonly string[] TextScaleNames = { "Kecil", "Normal", "Besar" };

        private const string KeyBgm = "rk_bgm", KeySfx = "rk_sfx", KeyText = "rk_textScale";

        public static float BgmVolume
        {
            get => PlayerPrefs.GetFloat(KeyBgm, 0.8f);
            set { PlayerPrefs.SetFloat(KeyBgm, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        /// <summary>Volume efek; juga dipakai sebagai volume AudioListener sampai ada mixer terpisah.</summary>
        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(KeySfx, 0.8f);
            set { PlayerPrefs.SetFloat(KeySfx, Mathf.Clamp01(value)); PlayerPrefs.Save(); AudioListener.volume = SfxVolume; }
        }

        public static int TextScaleIndex
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(KeyText, 1), 0, TextScales.Length - 1);
            set { PlayerPrefs.SetInt(KeyText, Mathf.Clamp(value, 0, TextScales.Length - 1)); PlayerPrefs.Save(); ApplyTextScaleAll(); }
        }

        public static float TextScale => TextScales[TextScaleIndex];

        // ---------- Resolusi ----------

        /// <summary>Daftar resolusi unik (lebar x tinggi) yang didukung layar, dari kecil ke besar.</summary>
        public static List<Vector2Int> Resolutions()
        {
            var list = new List<Vector2Int>();
            foreach (var r in Screen.resolutions)
            {
                var v = new Vector2Int(r.width, r.height);
                if (!list.Contains(v)) list.Add(v);
            }
            if (list.Count == 0) list.Add(new Vector2Int(Screen.width, Screen.height));
            return list;
        }

        public static void SetResolution(Vector2Int size, bool fullscreen) =>
            Screen.SetResolution(size.x, size.y, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);

        // ---------- Ukuran teks ----------

        // Ukuran asli per teks. Kunci = objeknya sendiri; teks yang sudah hancur dibersihkan saat scene berganti.
        private static readonly Dictionary<TMP_Text, float> baseSizes = new Dictionary<TMP_Text, float>();

        /// <summary>Terapkan skala teks ke satu teks (dipanggil juga untuk UI yang dibuat dari kode).</summary>
        public static void ApplyTextScale(TMP_Text t)
        {
            if (!t) return;
            if (!baseSizes.TryGetValue(t, out float size)) baseSizes[t] = size = t.fontSize;
            t.fontSize = size * TextScale;
        }

        public static void ApplyTextScaleAll()
        {
            foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include)) ApplyTextScale(t);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            AudioListener.volume = SfxVolume;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene s, LoadSceneMode m)
        {
            // Hanya buang teks yang sudah hancur: teks lintas scene (menu jeda) harus tetap ingat ukuran aslinya.
            var dead = new List<TMP_Text>();
            foreach (var k in baseSizes.Keys) if (!k) dead.Add(k);
            foreach (var k in dead) baseSizes.Remove(k);
            if (TextScaleIndex != 1) ApplyTextScaleAll();
        }
    }
}
