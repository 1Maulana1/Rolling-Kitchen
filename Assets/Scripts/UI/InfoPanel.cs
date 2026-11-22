using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RollingKitchen
{
    /// <summary>
    /// Panel teks modal sederhana (judul + isi + tombol) untuk papan target, buku resep, dan ringkasan akhir.
    /// Esc atau tombol menutupnya. Hidup lintas scene; dibangun dari kode sekali.
    /// </summary>
    public class InfoPanel : MonoBehaviour
    {
        public static bool IsOpen => instance != null && instance.root.activeSelf;

        private static InfoPanel instance;
        private GameObject root;
        private TMP_Text title, body, buttonLabel;
        private Action onClose;
        private bool escCloses;

        public static void Show(string heading, string text, string button = "Tutup (Esc)", Action closed = null, bool escCloses = true)
        {
            if (instance == null)
            {
                var go = new GameObject("InfoPanel");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<InfoPanel>();
                instance.Build();
            }
            instance.title.text = heading;
            instance.body.text = text;
            instance.buttonLabel.text = button;
            instance.onClose = closed;
            instance.escCloses = escCloses;
            instance.root.SetActive(true);
            GameInput.Locked = true;
        }

        private void Close()
        {
            root.SetActive(false);
            GameInput.Locked = false;
            var cb = onClose;
            onClose = null;
            cb?.Invoke();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (root.activeSelf && escCloses && kb != null && kb.escapeKey.wasPressedThisFrame) Close();
        }

        private void Build()
        {
            var canvas = RuntimeUi.Canvas("Info Canvas", 80, transform);
            root = RuntimeUi.Backdrop(canvas.transform, "Info");
            var panel = RuntimeUi.Panel(root.transform, new Vector2(1100, 860), RuntimeUi.PanelColor, 36, 16f);
            title = RuntimeUi.Text(panel, "", 44, TextAlignmentOptions.Center, 64f);
            body = RuntimeUi.Text(panel, "", 28, TextAlignmentOptions.TopLeft);
            body.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1f;
            body.enableAutoSizing = true;
            body.fontSizeMin = 16;
            body.fontSizeMax = 28;
            buttonLabel = RuntimeUi.Label(RuntimeUi.Button(panel, "Tutup", RuntimeUi.Blue, Close, 64f));
            root.SetActive(false);
        }
    }
}
