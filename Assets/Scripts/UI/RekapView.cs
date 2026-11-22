using TMPro;
using UnityEngine;

namespace RollingKitchen
{
    /// <summary>Rekap harian: pendapatan, bintang, hari gagal, dan hasil akhir bila permainan berakhir.</summary>
    public class RekapView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text continueLabel;

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                bodyText.text = "GameManager belum ada. Mulai dari TitleScreen.";
                return;
            }

            string stars = new string('*', gm.LastDayStars) + new string('-', 3 - gm.LastDayStars);
            bool over = gm.Outcome != GameOutcome.Berjalan;

            titleText.text = $"Rekap Hari {gm.LastDayNumber}";
            bodyText.text =
                $"Pendapatan penjualan: Rp {gm.LastDayRevenue:N0}\n" +
                $"Poin: {gm.LastDayPoints} / target {gm.DailyTarget}\n" +
                $"Bintang: {stars}" + (gm.LastDayBonus > 0 ? $"  (bonus Rp {gm.LastDayBonus:N0})\n" : "\n") +
                $"Laba bersih (setelah belanja): Rp {gm.LastDayNet:N0}\n" +
                $"Hari gagal: {gm.FailedDays}/{gm.MaxFailedDays}\n" +
                $"Uang sekarang: Rp {gm.Money:N0}";

            if (over)
            {
                bodyText.text += $"\n\n== {OutcomeText(gm.Outcome)} ==";
                continueLabel.text = "Kembali ke Title";
            }
            else continueLabel.text = "Tidur (hari berikutnya)";
        }

        private static string OutcomeText(GameOutcome o)
        {
            switch (o)
            {
                case GameOutcome.Menang: return "MENANG! Food truck-mu sukses";
                case GameOutcome.Bangkrut: return "BANGKRUT";
                default: return "KALAH";
            }
        }

        /// <summary>Tombol lanjut: tidur ke Rumah, atau reset dan kembali ke Title bila permainan berakhir.</summary>
        public void Continue()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.Outcome != GameOutcome.Berjalan)
            {
                gm.ResetGame();
                SceneLoader.Load(GameScene.TitleScreen);
            }
            else SceneLoader.Load(GameScene.Rumah);
        }
    }
}
