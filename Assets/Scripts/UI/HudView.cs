using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RollingKitchen
{
    /// <summary>
    /// HUD: hari, fase, uang (berkedip hijau/merah saat berubah), bar poin dengan 3 penanda bintang,
    /// dan bar stok bahan (ikon meredup saat habis, oranye bila basi besok). Bar dibangun dari kode.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text dayText;
        [SerializeField] private TMP_Text phaseText;
        [SerializeField] private TMP_Text moneyText;
        [Tooltip("Tampilkan bar stok bahan (Food Truck tidak perlu: raknya sudah menampilkan stok).")]
        [SerializeField] private bool showStock = true;
        [SerializeField] private bool showPoints = true;

        private static readonly Color Gold = new Color(1f, 0.82f, 0.25f);
        private static readonly Color StarOff = new Color(0.4f, 0.4f, 0.45f);
        private static readonly Color MoneyUp = new Color(0.35f, 0.95f, 0.45f);
        private static readonly Color MoneyDown = new Color(1f, 0.4f, 0.35f);
        private static readonly Color ChipColor = new Color(0.12f, 0.12f, 0.15f, 0.85f);
        private static readonly Color ChipWarn = new Color(0.85f, 0.45f, 0.1f, 0.95f);

        private GameManager gm;
        private int lastMoney;
        private float flashTime;
        private Color flashColor;
        private TMP_Text deltaText;

        private Image pointsFill;
        private TMP_Text pointsText;
        private readonly Image[] stars = new Image[3];
        private readonly RectTransform[] starRects = new RectTransform[3];

        private readonly Dictionary<IngredientData, (Image bg, TMP_Text label)> chips = new Dictionary<IngredientData, (Image, TMP_Text)>();

        private void Awake()
        {
            if (moneyText) BuildMoneyDelta();
            if (showPoints) BuildPointsBar();
            if (showStock) BuildStockBar();
        }

        private void OnEnable()
        {
            gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogWarning("HudView: GameManager belum ada di scene.", this);
                return;
            }

            gm.DayChanged += OnDayChanged;
            gm.PhaseChanged += OnPhaseChanged;
            gm.MoneyChanged += OnMoneyChanged;
            gm.PointsChanged += OnPointsChanged;
            gm.Inventory.Changed += OnStockChanged;

            // Tampilkan nilai awal.
            lastMoney = gm.Money;
            OnDayChanged(gm.Day);
            OnPhaseChanged(gm.Phase);
            if (moneyText) moneyText.text = $"Rp {gm.Money:N0}";
            OnPointsChanged(gm.TodayPoints);
            RefreshStock();
        }

        private void OnDisable()
        {
            if (gm == null) return;
            gm.DayChanged -= OnDayChanged;
            gm.PhaseChanged -= OnPhaseChanged;
            gm.MoneyChanged -= OnMoneyChanged;
            gm.PointsChanged -= OnPointsChanged;
            gm.Inventory.Changed -= OnStockChanged;
            gm = null;
        }

        private void Update()
        {
            if (flashTime <= 0f || !moneyText) return;
            flashTime -= Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(flashTime / 1.2f);
            moneyText.color = Color.Lerp(Color.white, flashColor, k);
            if (deltaText) deltaText.alpha = k;
        }

        private void OnDayChanged(int day)
        {
            if (dayText) dayText.text = $"Hari {day}/{gm.TotalDays}";
            OnPointsChanged(gm.TodayPoints); // target berganti tiap hari
            RefreshStock();
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            if (phaseText) phaseText.text = phase == GamePhase.SiangSore ? "Siang/Sore" : phase.ToString();
        }

        private void OnMoneyChanged(int money)
        {
            if (!moneyText) return;
            int delta = money - lastMoney;
            lastMoney = money;
            moneyText.text = $"Rp {money:N0}";
            if (delta == 0) return;

            var c = delta > 0 ? MoneyUp : MoneyDown;
            moneyText.color = flashColor = c;
            if (deltaText)
            {
                deltaText.text = delta > 0 ? $"+Rp {delta:N0}" : $"-Rp {-delta:N0}";
                deltaText.color = c;
            }
            flashTime = 1.2f;
        }

        // ---------- Bar poin ----------

        private void OnPointsChanged(int points)
        {
            if (!pointsFill || gm == null) return;
            float top = Mathf.Max(1f, gm.DailyTarget * gm.StarThreshold(2));
            pointsFill.fillAmount = Mathf.Clamp01(points / top);
            pointsText.text = $"Poin {points} / {gm.DailyTarget}";

            for (int i = 0; i < 3; i++)
            {
                float at = gm.StarThreshold(i) / gm.StarThreshold(2);
                starRects[i].anchorMin = starRects[i].anchorMax = new Vector2(at, 0.5f);
                stars[i].color = points >= gm.DailyTarget * gm.StarThreshold(i) ? Gold : StarOff;
            }
        }

        private void BuildPointsBar()
        {
            var bg = NewImage("Bar Poin", transform, new Color(0.08f, 0.08f, 0.1f, 0.85f));
            var rt = bg.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -22);
            rt.sizeDelta = new Vector2(560, 26);

            pointsFill = NewImage("Isi", rt, new Color(0.3f, 0.8f, 0.45f));
            RuntimeUi.Stretch(pointsFill.rectTransform);
            pointsFill.rectTransform.offsetMin = new Vector2(3, 3);
            pointsFill.rectTransform.offsetMax = new Vector2(-3, -3);
            pointsFill.sprite = WhiteSprite();
            pointsFill.type = Image.Type.Filled;
            pointsFill.fillMethod = Image.FillMethod.Horizontal;

            for (int i = 0; i < 3; i++)
            {
                stars[i] = NewImage($"Bintang {i + 1}", rt, StarOff);
                starRects[i] = stars[i].rectTransform;
                starRects[i].pivot = new Vector2(0.5f, 0.5f);
                starRects[i].sizeDelta = new Vector2(22, 22);
                starRects[i].localRotation = Quaternion.Euler(0, 0, 45f); // belah ketupat sebagai penanda bintang
                starRects[i].anchoredPosition = Vector2.zero;
            }

            pointsText = RuntimeUi.Text(rt, "", 22);
            var tr = pointsText.rectTransform;
            tr.anchorMin = new Vector2(0, 0);
            tr.anchorMax = new Vector2(1, 0);
            tr.pivot = new Vector2(0.5f, 1f);
            tr.anchoredPosition = new Vector2(0, -2);
            tr.sizeDelta = new Vector2(0, 30);
        }

        // ---------- Bar stok ----------

        private void OnStockChanged(IngredientData _, int __) => RefreshStock();

        private void RefreshStock()
        {
            if (gm == null) return;
            foreach (var kv in chips)
            {
                int n = gm.Inventory.Get(kv.Key);
                bool warn = n > 0 && gm.Inventory.CountExpiringAfter(kv.Key, gm.Day) > 0;
                kv.Value.bg.color = warn ? ChipWarn : ChipColor;
                kv.Value.label.text = $"{kv.Key.displayName}\nx{n}";
                kv.Value.label.alpha = n > 0 ? 1f : 0.35f;
                var c = kv.Value.bg.color;
                if (n == 0) kv.Value.bg.color = new Color(c.r, c.g, c.b, 0.35f);
            }
        }

        private void BuildStockBar()
        {
            var db = ItemDatabase.Instance;
            if (db == null || db.ingredients == null) return;

            var go = new GameObject("Bar Stok", typeof(RectTransform), typeof(GridLayoutGroup));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            // Kiri bawah, di atas baris petunjuk kontrol (atas tengah dipakai bar poin dan panduan).
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(24, 66);
            rt.sizeDelta = new Vector2(7 * 78, 2 * 54);
            var grid = go.GetComponent<GridLayoutGroup>();
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.cellSize = new Vector2(74, 50);
            grid.spacing = new Vector2(4, 4);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 7;

            foreach (var item in db.ingredients)
            {
                if (item == null) continue;
                var bg = NewImage(item.displayName, rt, ChipColor);
                var label = RuntimeUi.Text(bg.transform, "", 16);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                RuntimeUi.Stretch(label.rectTransform);
                label.lineSpacing = -10f;
                chips[item] = (bg, label);
            }
        }

        private void BuildMoneyDelta()
        {
            deltaText = RuntimeUi.Text(moneyText.transform, "", 24, TextAlignmentOptions.TopRight);
            var rt = deltaText.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(0, -34);
            rt.sizeDelta = new Vector2(300, 30);
            deltaText.alpha = 0f;
        }

        // ---------- Util ----------

        private static Image NewImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static Sprite white;
        // Image.Type.Filled butuh sprite; buat sprite putih 4x4 sekali.
        private static Sprite WhiteSprite()
        {
            if (white) return white;
            var tex = new Texture2D(4, 4);
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            white = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            return white;
        }
    }
}
