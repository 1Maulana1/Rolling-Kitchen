using System.Collections.Generic;

namespace RollingKitchen
{
    /// <summary>Naskah cutscene pembuka dan penutup (teks sementara; potret masih kotak warna).</summary>
    public static class Story
    {
        private const string Me = "Kamu", Mom = "Ibu";

        public static List<DialogueLine> Intro(GameManager gm) => new List<DialogueLine>
        {
            new DialogueLine(Mom, "Food truck tua di garasi itu akhirnya bisa jalan lagi. Kamu yakin mau meneruskannya?", DialogueOverlay.MomColor),
            new DialogueLine(Me, "Yakin, Bu! Aku mau jualan tiap hari sampai food truck ini dikenal satu kota.", DialogueOverlay.MeColor),
            new DialogueLine(Mom, $"Modal kita cuma Rp {gm.Money:N0}. Dalam {gm.TotalDays} hari, kumpulkan Rp {gm.FinalTarget:N0}. " +
                                  "Kalau tidak, truknya terpaksa dijual.", DialogueOverlay.MomColor),
            new DialogueLine(Me, "Pagi belanja di pasar, siang dan malam berjualan. Siap!", DialogueOverlay.MeColor),
            new DialogueLine(Mom, "Lihat papan target di dinding, dan tidurlah di kasur supaya kemajuanmu tersimpan.", DialogueOverlay.MomColor),
        };

        public static List<DialogueLine> Ending(GameManager gm)
        {
            switch (gm.Outcome)
            {
                case GameOutcome.Menang:
                    return new List<DialogueLine>
                    {
                        new DialogueLine(Mom, $"Rp {gm.Money:N0}! Targetnya tercapai. Food truck kita selamat!", DialogueOverlay.MomColor),
                        new DialogueLine(Me, "Pelanggan tetap kita juga makin banyak. Besok kita buka lagi, Bu!", DialogueOverlay.MeColor),
                    };
                case GameOutcome.Bangkrut:
                    return new List<DialogueLine>
                    {
                        new DialogueLine(Me, "Uangnya habis... belanja bahan saja sudah tidak cukup.", DialogueOverlay.MeColor),
                        new DialogueLine(Mom, "Tidak apa-apa. Kita kumpulkan modal lagi, lalu coba sekali lagi.", DialogueOverlay.MomColor),
                    };
                default:
                    return new List<DialogueLine>
                    {
                        new DialogueLine(Mom, "Targetnya belum tercapai. Truknya harus kita lepas dulu.", DialogueOverlay.MomColor),
                        new DialogueLine(Me, "Aku sudah tahu apa yang kurang. Lain kali pasti berhasil.", DialogueOverlay.MeColor),
                    };
            }
        }
    }
}
