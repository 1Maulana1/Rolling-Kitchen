using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RollingKitchen
{
    /// <summary>
    /// Toko Alat di Pasar (pagi): upgrade alat masak. Level 1 memilih satu keunggulan
    /// (masak cepat ATAU QTE longgar), level 2 mendapat keduanya. Panel dibangun dari kode.
    /// </summary>
    public class ToolShop : Interactable
    {
        private GameObject root;
        private Transform list;
        private TMP_Text moneyText, noteText;
        private GameManager gm;

        public override string GetPrompt() => "E: Masuk Toko Alat (upgrade alat masak)";

        public override void Interact()
        {
            gm = GameManager.Instance;
            if (gm == null) return;
            if (root == null) Build();
            root.SetActive(true);
            GameInput.Locked = true;
            noteText.text = "";
            gm.MoneyChanged += OnMoney;
            Refresh();
        }

        public void Close()
        {
            if (root == null || !root.activeSelf) return;
            root.SetActive(false);
            GameInput.Locked = false;
            if (gm != null) gm.MoneyChanged -= OnMoney;
        }

        private void OnDisable() => Close();
        private void OnMoney(int _) => Refresh();

        private void Update()
        {
            var kb = Keyboard.current;
            if (root != null && root.activeSelf && kb != null && kb.escapeKey.wasPressedThisFrame) Close();
        }

        private void Buy(ToolType t, ToolPerk perk)
        {
            int price = gm.Upgrades.NextPrice(t);
            if (gm.Upgrades.TryBuy(gm, t, perk))
                noteText.text = $"{t} naik ke level {gm.Upgrades.Level(t)}!";
            else
                noteText.text = price > gm.Money ? "Uang tidak cukup." : "Tidak bisa dibeli.";
            Refresh();
        }

        private void Refresh()
        {
            if (gm == null || list == null) return;
            moneyText.text = $"Uangmu: Rp {gm.Money:N0}";
            RuntimeUi.Clear(list);

            foreach (ToolType t in Enum.GetValues(typeof(ToolType)))
            {
                var tool = t;
                int lv = gm.Upgrades.Level(t);
                string perks = lv == 0 ? "belum di-upgrade" : lv >= ToolUpgrades.MaxLevel ? "masak cepat + QTE longgar"
                             : ToolUpgrades.Name(gm.Upgrades.Perk(t)).ToLower();
                RuntimeUi.Text(list, $"<b>{t}</b>  Lv {lv}/{ToolUpgrades.MaxLevel}  <color=#BBBBBB>({perks})</color>",
                    26, TextAlignmentOptions.Left, 40f);

                var row = RuntimeUi.Row(list, 60f);
                int price = gm.Upgrades.NextPrice(t);
                if (price < 0)
                {
                    RuntimeUi.Text(row, "Level maksimal", 24);
                    continue;
                }
                bool afford = gm.Money >= price;
                if (lv == 0)
                {
                    RuntimeUi.Button(row, $"Masak cepat  Rp {price}", RuntimeUi.Green, () => Buy(tool, ToolPerk.MasakCepat), 60f, 24f).interactable = afford;
                    RuntimeUi.Button(row, $"QTE longgar  Rp {price}", RuntimeUi.Blue, () => Buy(tool, ToolPerk.QteLonggar), 60f, 24f).interactable = afford;
                }
                else
                {
                    var other = gm.Upgrades.Perk(t) == ToolPerk.MasakCepat ? ToolPerk.QteLonggar : ToolPerk.MasakCepat;
                    RuntimeUi.Button(row, $"Tambah {ToolUpgrades.Name(other).ToLower()}  Rp {price}", RuntimeUi.Green,
                        () => Buy(tool, ToolPerk.None), 60f, 24f).interactable = afford;
                }
            }
        }

        private void Build()
        {
            var canvas = RuntimeUi.Canvas("Toko Alat Canvas", 50, transform);
            root = RuntimeUi.Backdrop(canvas.transform, "Toko Alat");
            var panel = RuntimeUi.Panel(root.transform, new Vector2(900, 760), RuntimeUi.PanelColor);

            RuntimeUi.Text(panel, "Toko Alat", 44, TextAlignmentOptions.Center, 60f);
            RuntimeUi.Text(panel,
                $"Masak cepat: waktu masak x{ToolUpgrades.FastCookMultiplier:0.##}.  QTE longgar: waktu tekan x{ToolUpgrades.WideQteMultiplier:0.#}.\n" +
                "Level 1 pilih salah satu, level 2 dapat keduanya.",
                22, TextAlignmentOptions.Center, 64f).color = new Color(1f, 1f, 1f, 0.75f);
            moneyText = RuntimeUi.Text(panel, "", 26, TextAlignmentOptions.Center, 36f);

            var listGo = new GameObject("Daftar", typeof(RectTransform), typeof(UnityEngine.UI.VerticalLayoutGroup), typeof(UnityEngine.UI.LayoutElement));
            listGo.transform.SetParent(panel, false);
            listGo.GetComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1f;
            var v = listGo.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            v.spacing = 8f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            list = listGo.transform;

            noteText = RuntimeUi.Text(panel, "", 24, TextAlignmentOptions.Center, 34f);
            noteText.color = new Color(1f, 0.85f, 0.3f);
            RuntimeUi.Button(panel, "Tutup (Esc)", RuntimeUi.Gray, Close, 56f);
            root.SetActive(false);
        }
    }
}
