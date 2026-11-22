using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RollingKitchen
{
    /// <summary>
    /// Panel Options (dari Title dan menu jeda): volume musik/efek, resolusi + layar penuh,
    /// ukuran teks, dan daftar kontrol. Hidup lintas scene; dibangun dari kode sekali.
    /// </summary>
    public class OptionsMenu : MonoBehaviour
    {
        public static bool IsOpen => instance != null && instance.root.activeSelf;
        /// <summary>True bila Esc di frame ini milik Options (terbuka, atau baru saja ditutup oleh Esc).</summary>
        public static bool OwnsEscape => IsOpen || closedFrame == Time.frameCount;
        private static int closedFrame = -1;

        private static OptionsMenu instance;
        private GameObject root;
        private TMP_Text bgmLabel, sfxLabel, resLabel, fullLabel, textLabel;
        private Action onClose;
        private int resIndex;

        public static void Open(Action closed = null)
        {
            if (instance == null)
            {
                var go = new GameObject("OptionsMenu");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<OptionsMenu>();
                instance.Build();
            }
            instance.onClose = closed;
            instance.SyncResolution();
            instance.Refresh();
            instance.root.SetActive(true);
        }

        public void Close()
        {
            root.SetActive(false);
            closedFrame = Time.frameCount;
            var cb = onClose;
            onClose = null;
            cb?.Invoke();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (root.activeSelf && kb != null && kb.escapeKey.wasPressedThisFrame) Close();
        }

        private void SyncResolution()
        {
            var list = GameSettings.Resolutions();
            resIndex = list.IndexOf(new Vector2Int(Screen.width, Screen.height));
            if (resIndex < 0) resIndex = list.Count - 1;
        }

        private void Refresh()
        {
            bgmLabel.text = $"Musik: {Mathf.RoundToInt(GameSettings.BgmVolume * 100)}%";
            sfxLabel.text = $"Efek: {Mathf.RoundToInt(GameSettings.SfxVolume * 100)}%";
            var r = GameSettings.Resolutions()[Mathf.Clamp(resIndex, 0, GameSettings.Resolutions().Count - 1)];
            resLabel.text = $"Resolusi: {r.x} x {r.y}";
            fullLabel.text = Screen.fullScreen ? "Layar penuh: Ya" : "Layar penuh: Tidak";
            textLabel.text = $"Ukuran teks: {GameSettings.TextScaleNames[GameSettings.TextScaleIndex]}";
        }

        private void StepVolume(bool music, float delta)
        {
            if (music) GameSettings.BgmVolume = Mathf.Round((GameSettings.BgmVolume + delta) * 10f) / 10f;
            else GameSettings.SfxVolume = Mathf.Round((GameSettings.SfxVolume + delta) * 10f) / 10f;
            Refresh();
        }

        private void StepResolution(int delta)
        {
            var list = GameSettings.Resolutions();
            resIndex = (resIndex + delta + list.Count) % list.Count;
            GameSettings.SetResolution(list[resIndex], Screen.fullScreen);
            Refresh();
        }

        private void ToggleFullscreen()
        {
            var list = GameSettings.Resolutions();
            bool full = !Screen.fullScreen;
            GameSettings.SetResolution(list[Mathf.Clamp(resIndex, 0, list.Count - 1)], full);
            fullLabel.text = full ? "Layar penuh: Ya" : "Layar penuh: Tidak"; // Screen.fullScreen baru berubah frame berikutnya
        }

        private void StepTextScale(int delta)
        {
            GameSettings.TextScaleIndex = GameSettings.TextScaleIndex + delta;
            Refresh();
        }

        // ---------- UI ----------

        private void Build()
        {
            var canvas = RuntimeUi.Canvas("Options Canvas", 110, transform);
            root = RuntimeUi.Backdrop(canvas.transform, "Options");
            var panel = RuntimeUi.Panel(root.transform, new Vector2(980, 900), RuntimeUi.PanelColor, 32, 12f);

            RuntimeUi.Text(panel, "Options", 44, TextAlignmentOptions.Center, 64f);
            bgmLabel = Stepper(panel, () => StepVolume(true, -0.1f), () => StepVolume(true, 0.1f));
            sfxLabel = Stepper(panel, () => StepVolume(false, -0.1f), () => StepVolume(false, 0.1f));
            RuntimeUi.Text(panel, "Audio menyusul; pengaturan volume sudah disimpan.", 20, TextAlignmentOptions.Center, 28f)
                .color = new Color(1f, 1f, 1f, 0.55f);
            resLabel = Stepper(panel, () => StepResolution(-1), () => StepResolution(1));
            fullLabel = RuntimeUi.Label(RuntimeUi.Button(panel, "Layar penuh", RuntimeUi.Gray, ToggleFullscreen, 56f, 26f));
            textLabel = Stepper(panel, () => StepTextScale(-1), () => StepTextScale(1));

            RuntimeUi.Text(panel, "Kontrol", 30, TextAlignmentOptions.Left, 44f);
            RuntimeUi.Text(panel,
                "WASD / Panah: jalan      E: bicara / pakai      Esc: tutup panel / jeda\n" +
                "Food Truck: klik alat, bahan, hidangan, pelanggan.  Q W E R: QTE.  Spasi: skill koki",
                22, TextAlignmentOptions.Left, 70f).color = new Color(1f, 1f, 1f, 0.8f);

            RuntimeUi.Button(panel, "Kembali (Esc)", RuntimeUi.Blue, Close, 60f);
            root.SetActive(false);
        }

        // Baris "[-]  label  [+]".
        private static TMP_Text Stepper(Transform parent, UnityEngine.Events.UnityAction minus, UnityEngine.Events.UnityAction plus)
        {
            var row = RuntimeUi.Row(parent, 56f);
            var h = row.GetComponent<HorizontalLayoutGroup>();
            h.childForceExpandWidth = false;
            var m = RuntimeUi.Button(row, "<", RuntimeUi.Gray, minus, 56f);
            m.GetComponent<LayoutElement>().preferredWidth = 80f;
            var label = RuntimeUi.Text(row, "", 28);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var p = RuntimeUi.Button(row, ">", RuntimeUi.Gray, plus, 56f);
            p.GetComponent<LayoutElement>().preferredWidth = 80f;
            return label;
        }
    }
}
