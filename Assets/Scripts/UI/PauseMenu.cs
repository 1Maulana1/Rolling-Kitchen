using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RollingKitchen
{
    /// <summary>
    /// Menu jeda (Esc) di Rumah, Pasar, dan Food Truck: Lanjut, Options, Keluar ke Title.
    /// Esc hanya membuka menu bila tidak ada panel lain yang terbuka (panel itu memakai Esc untuk menutup diri).
    /// Dibuat otomatis sekali dan hidup lintas scene; UI-nya dibangun dari kode.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        public static bool IsPaused { get; private set; }

        private GameObject root;
        private bool lockedLastFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            IsPaused = false;
            var go = new GameObject("PauseMenu");
            DontDestroyOnLoad(go);
            go.AddComponent<PauseMenu>().Build();
        }

        private static bool SceneAllowsPause()
        {
            string s = SceneManager.GetActiveScene().name;
            return s == nameof(GameScene.Rumah) || s == nameof(GameScene.Pasar) || s == nameof(GameScene.FoodTruck);
        }

        private void OnEnable() => SceneManager.activeSceneChanged += OnSceneChanged;
        private void OnDisable() => SceneManager.activeSceneChanged -= OnSceneChanged;
        private void OnSceneChanged(Scene _, Scene __) { if (IsPaused) Resume(); }

        // Menu jeda dibuat sekali, sebelum skala teks dari Options sempat diterapkan.
        private void Start() => GameSettings.ApplyTextScaleAll();

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            if (OptionsMenu.OwnsEscape || DialogueOverlay.IsOpen) return;

            if (IsPaused) Resume();
            // Bila frame lalu ada panel terbuka, Esc kali ini milik panel itu (menutupnya), bukan menu jeda.
            else if (!lockedLastFrame && !GameInput.Locked && SceneAllowsPause()) Pause();
        }

        private void LateUpdate() => lockedLastFrame = GameInput.Locked && !IsPaused;

        public void Pause()
        {
            IsPaused = true;
            Time.timeScale = 0f;
            GameInput.Locked = true;
            RefreshLabels();
            root.SetActive(true);
        }

        public void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            GameInput.Locked = false;
            root.SetActive(false);
        }

        private void QuitToTitle()
        {
            Resume();
            var gm = GameManager.Instance;
            if (gm != null) gm.SaveCurrent(); // tanpa slot (uji scene langsung) tidak ada yang disimpan
            SceneLoader.Load(GameScene.TitleScreen);
        }

        // Label tombol keluar mengikuti ada/tidaknya slot aktif (diperbarui tiap menu dibuka).
        private TextMeshProUGUI quitLabel, noteLabel;

        private void RefreshLabels()
        {
            bool hasSlot = GameManager.Instance != null && GameManager.Instance.CurrentSlot >= 0;
            if (quitLabel) quitLabel.text = hasSlot ? "Simpan & Keluar ke Title" : "Keluar ke Title";
            if (noteLabel) noteLabel.text = hasSlot
                ? $"Slot {GameManager.Instance.CurrentSlot + 1}"
                : "Mode uji: progres tidak disimpan.";
        }

        // ---------- UI ----------

        private void Build()
        {
            var canvasGo = new GameObject("Pause Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            root = new GameObject("Jeda", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvasGo.transform, false);
            Stretch((RectTransform)root.transform);
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(root.transform, false);
            panel.GetComponent<Image>().color = new Color(0.1f, 0.08f, 0.08f, 1f);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(520, 440);
            var v = panel.GetComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(32, 32, 28, 28);
            v.spacing = 16f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var title = Text(panel.transform, "Jeda", 44);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 70f;

            Button(panel.transform, "Lanjut", new Color(0.2f, 0.6f, 0.3f), Resume, true);
            Button(panel.transform, "Options", new Color(0.35f, 0.35f, 0.4f), () => OptionsMenu.Open(), true);
            quitLabel = Button(panel.transform, "Keluar ke Title", new Color(0.75f, 0.25f, 0.2f), QuitToTitle, true);

            noteLabel = Text(panel.transform, "", 22);
            noteLabel.color = new Color(1f, 1f, 1f, 0.6f);
            noteLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

            root.SetActive(false);
        }

        private static TextMeshProUGUI Text(Transform parent, string text, float size)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
            t.raycastTarget = false;
            return t;
        }

        private static TextMeshProUGUI Button(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick, bool interactable)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            go.GetComponent<LayoutElement>().preferredHeight = 64f;
            var b = go.GetComponent<Button>();
            b.interactable = interactable;
            if (onClick != null) b.onClick.AddListener(onClick);
            var t = Text(go.transform, label, 28);
            Stretch(t.rectTransform);
            return t;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
