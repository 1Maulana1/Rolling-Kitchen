using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RollingKitchen
{
    /// <summary>
    /// Pemandu lintas scene: cutscene pembuka saat pertama masuk Rumah, petunjuk tutorial teks di hari 1-2,
    /// dan peringatan bahan basi di Rumah pagi hari. Dibuat otomatis; UI dari kode.
    /// </summary>
    public class SceneGuide : MonoBehaviour
    {
        public const int TutorialDays = 2;

        private GameObject box;
        private TMP_Text noticeText, tutorialText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var go = new GameObject("SceneGuide");
            DontDestroyOnLoad(go);
            go.AddComponent<SceneGuide>().Build();
        }

        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
        private void Start() => Show(SceneManager.GetActiveScene().name); // scene pertama sudah termuat sebelum objek ini ada

        private void OnSceneLoaded(Scene s, LoadSceneMode _) => Show(s.name);

        private void Update()
        {
            // Petunjuk disembunyikan saat panel/cutscene/menu jeda terbuka (isinya diperbarui tiap scene dimuat).
            if (!box) return;
            bool hide = DialogueOverlay.IsOpen || InfoPanel.IsOpen || PauseMenu.IsPaused || OptionsMenu.IsOpen;
            var cg = box.GetComponent<CanvasGroup>();
            cg.alpha = hide ? 0f : 1f;
        }

        private void Show(string scene)
        {
            var gm = GameManager.Instance;
            bool playable = scene == nameof(GameScene.Rumah) || scene == nameof(GameScene.Pasar) || scene == nameof(GameScene.FoodTruck);
            if (gm == null || !playable) { box.SetActive(false); return; }

            string notice = scene == nameof(GameScene.Rumah) && gm.Phase == GamePhase.Pagi ? SpoilNotice(gm) : "";
            string tutorial = gm.Day <= TutorialDays ? Tutorial(scene, gm) : "";
            noticeText.text = notice;
            noticeText.gameObject.SetActive(notice.Length > 0);
            tutorialText.text = tutorial;
            tutorialText.gameObject.SetActive(tutorial.Length > 0);
            box.SetActive(notice.Length > 0 || tutorial.Length > 0);

            if (scene == nameof(GameScene.Rumah) && !gm.IntroSeen && gm.CurrentSlot >= 0)
                DialogueOverlay.Play(Story.Intro(gm), () => { gm.IntroSeen = true; gm.SaveCurrent(); });
        }

        private static string SpoilNotice(GameManager gm)
        {
            string s = "";
            if (gm.SpoiledToday.Count > 0)
                s += "Basi dan dibuang pagi ini: " + string.Join(", ", gm.SpoiledToday.Select(kv => $"{kv.Key.displayName} x{kv.Value}")) + ".";
            var soon = gm.Inventory.Items
                .Select(kv => (item: kv.Key, n: gm.Inventory.CountExpiringAfter(kv.Key, gm.Day)))
                .Where(x => x.n > 0).ToList();
            if (soon.Count > 0)
                s += (s.Length > 0 ? "\n" : "") + "Pakai hari ini, besok basi: " + string.Join(", ", soon.Select(x => $"{x.item.displayName} x{x.n}")) + ".";
            return s;
        }

        private static string Tutorial(string scene, GameManager gm)
        {
            if (scene == nameof(GameScene.Pasar))
                return "Dekati lapak dan tekan E untuk belanja. Toko Alat ada di ruko kanan atas. Klik \"Selesai Belanja\" untuk pulang.";
            if (scene == nameof(GameScene.FoodTruck))
                return "Klik alat > klik bahan di rak > tekan huruf QTE > klik alat (ambil) > klik hidangan > klik pelanggan. Kain = bersihkan alat.";
            switch (gm.Phase)
            {
                case GamePhase.Pagi: return "Pagi: belanja bahan di Pasar lewat pintu KIRI. Papan target dan buku resep ada di ruang tamu.";
                case GamePhase.SiangSore: return "Siang/Sore: servis ringan di Food Truck lewat pintu KANAN. Kompor di dapur untuk eksperimen resep spesial.";
                default: return "Malam: servis utama di Food Truck (pintu KANAN). Setelahnya hari ditutup dan disimpan.";
            }
        }

        private void Build()
        {
            var canvas = RuntimeUi.Canvas("Panduan Canvas", 40, transform);
            box = new GameObject("Panduan", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter), typeof(CanvasGroup));
            box.transform.SetParent(canvas.transform, false);
            box.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.07f, 0.82f);
            box.GetComponent<Image>().raycastTarget = false;
            box.GetComponent<CanvasGroup>().blocksRaycasts = false;
            var rt = (RectTransform)box.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -84);
            rt.sizeDelta = new Vector2(1040, 0);
            var v = box.GetComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(18, 18, 10, 10);
            v.spacing = 4f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            box.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            noticeText = RuntimeUi.Text(box.transform, "", 24);
            noticeText.color = new Color(1f, 0.65f, 0.3f);
            tutorialText = RuntimeUi.Text(box.transform, "", 22);
            box.SetActive(false);
        }
    }
}
