using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RollingKitchen
{
    /// <summary>
    /// Pembuat UI dari kode untuk panel yang tidak perlu ditata di scene
    /// (menu jeda, Options, Toko Alat, papan target, buku resep, dialog, ringkasan akhir).
    /// </summary>
    public static class RuntimeUi
    {
        public static readonly Color PanelColor = new Color(0.1f, 0.08f, 0.08f, 1f);
        public static readonly Color Dim = new Color(0f, 0f, 0f, 0.6f);
        public static readonly Color Blue = new Color(0.2f, 0.6f, 0.9f);
        public static readonly Color Green = new Color(0.2f, 0.6f, 0.3f);
        public static readonly Color Red = new Color(0.75f, 0.25f, 0.2f);
        public static readonly Color Gray = new Color(0.35f, 0.35f, 0.4f);

        /// <summary>Canvas overlay 1920x1080 dengan urutan gambar tertentu.</summary>
        public static Canvas Canvas(string name, int sortingOrder, Transform parent = null)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent) go.transform.SetParent(parent, false);
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = sortingOrder;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.matchWidthOrHeight = 0.5f;
            return c;
        }

        /// <summary>Lapisan gelap selayar penuh (menangkap klik) yang menjadi akar sebuah modal.</summary>
        public static GameObject Backdrop(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            go.GetComponent<Image>().color = Dim;
            return go;
        }

        /// <summary>Panel bertumpuk vertikal di tengah layar.</summary>
        public static RectTransform Panel(Transform parent, Vector2 size, Color color, int padding = 28, float spacing = 14f)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            var v = go.GetComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(padding, padding, padding, padding);
            v.spacing = spacing;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return rt;
        }

        /// <summary>Baris horizontal dalam layout (untuk tombol berdampingan).</summary>
        public static RectTransform Row(Transform parent, float height, float spacing = 12f)
        {
            var go = new GameObject("Baris", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = height;
            var h = go.GetComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            return (RectTransform)go.transform;
        }

        public static TextMeshProUGUI Text(Transform parent, string text, float size,
            TextAlignmentOptions align = TextAlignmentOptions.Center, float height = -1f)
        {
            var go = new GameObject("Teks", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            t.raycastTarget = false;
            GameSettings.ApplyTextScale(t);
            if (height > 0f) go.AddComponent<LayoutElement>().preferredHeight = height;
            return t;
        }

        /// <summary>Tombol berlabel. Mengembalikan tombolnya; labelnya = anak pertama.</summary>
        public static Button Button(Transform parent, string label, Color color, UnityAction onClick, float height = 64f, float fontSize = 28f)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            go.GetComponent<LayoutElement>().preferredHeight = height;
            var b = go.GetComponent<Button>();
            b.targetGraphic = go.GetComponent<Image>();
            if (onClick != null) b.onClick.AddListener(onClick);
            var t = Text(go.transform, label, fontSize);
            Stretch(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(8, 0);
            t.rectTransform.offsetMax = new Vector2(-8, 0);
            return b;
        }

        public static TMP_Text Label(Button b) => b.GetComponentInChildren<TMP_Text>();

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>Hapus semua anak (untuk membangun ulang isi panel).</summary>
        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i);
                c.gameObject.SetActive(false); // keluar dari layout sekarang; dihancurkan akhir frame
                Object.Destroy(c.gameObject);
            }
        }
    }
}
