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
                continueLabel.text = "Lanjut (penutup)";
            }
            else continueLabel.text = gm.CurrentSlot >= 0 ? "Tidur & simpan (hari berikutnya)" : "Tidur (hari berikutnya)";
        }

        /// <summary>Ringkasan statistik akhir permainan.</summary>
        public static string FinalStats(GameManager gm)
        {
            var s = gm.Stats;
            var best = s.BestSeller();
            int dishesTotal = s.customersServed + s.wrongServes + s.burntDishes;
            string expRate = s.experimentsTried > 0
                ? $"{s.experimentsSucceeded}/{s.experimentsTried} ({100f * s.experimentsSucceeded / s.experimentsTried:0}%)" : "belum pernah mencoba";
            string cookRate = dishesTotal > 0 ? $"{100f * s.customersServed / dishesTotal:0}%" : "-";
            int days = gm.LastDayNumber > 0 ? gm.LastDayNumber : gm.Day;
            return
                $"Hari dimainkan: {days}\n" +
                $"Total pendapatan: <b>Rp {s.totalRevenue:N0}</b>\n" +
                $"Uang akhir: Rp {gm.Money:N0}  (target Rp {gm.FinalTarget:N0})\n" +
                $"Total bintang: <b>{s.totalStars}</b> dari {days * 3}\n" +
                $"Hidangan terlaris: <b>{best.name}</b>" + (best.count > 0 ? $" ({best.count} porsi)" : "") + "\n" +
                $"Keberhasilan eksperimen resep: {expRate}\n" +
                $"Hidangan berhasil disajikan: {cookRate}  (salah saji {s.wrongServes}, gosong {s.burntDishes})\n" +
                $"Pelanggan dilayani: {s.customersServed}, pergi: {s.customersLost}\n" +
                $"Bahan basi terbuang: {s.spoiledUnits} unit";
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

        /// <summary>
        /// Tombol lanjut: tidur (autosave hari baru) lalu ke Rumah. Bila permainan berakhir,
        /// slotnya dikosongkan dan kembali ke Title.
        /// </summary>
        public void Continue()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.Outcome != GameOutcome.Berjalan)
            {
                // Permainan berakhir: cutscene penutup -> ringkasan statistik -> Title.
                if (DialogueOverlay.IsOpen || InfoPanel.IsOpen) return;
                DialogueOverlay.Play(Story.Ending(gm), () =>
                    InfoPanel.Show(OutcomeText(gm.Outcome), FinalStats(gm), "Kembali ke Title", () =>
                    {
                        gm.ClearCurrentSlot();
                        gm.ResetGame();
                        SceneLoader.Load(GameScene.TitleScreen);
                    }, false));
                return;
            }
            if (gm != null) gm.SaveCurrent(); // hari sudah maju di EndDay: yang tersimpan = pagi hari berikutnya
            SceneLoader.Load(GameScene.Rumah);
        }
    }
}
