using System.Text;
using UnityEngine;

namespace RollingKitchen
{
    /// <summary>Papan target di dinding Rumah: target hari ini, ambang bintang, target akhir, hari gagal.</summary>
    public class TargetBoard : Interactable
    {
        public override string GetPrompt() => "E: Lihat papan target";

        public override void Interact()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var sb = new StringBuilder();
            sb.AppendLine($"<b>Hari {gm.Day} dari {gm.TotalDays}</b>   {gm.Config.seasonName}");
            sb.AppendLine();
            sb.AppendLine($"Target poin hari ini: <b>{gm.DailyTarget}</b>");
            for (int i = 0; i < 3; i++)
                sb.AppendLine($"   Bintang {i + 1}: {Mathf.CeilToInt(gm.DailyTarget * gm.StarThreshold(i))} poin" +
                              (i == 2 ? "  (bonus 10% pendapatan)" : ""));
            sb.AppendLine($"Poin sejauh ini: {gm.TodayPoints}");
            sb.AppendLine("Poin = pendapatan + 5 per hidangan sempurna - 5 per QTE gagal beruntun.");
            sb.AppendLine();
            sb.AppendLine($"Target akhir (hari {gm.TotalDays}): <b>Rp {gm.FinalTarget:N0}</b>   Uangmu: Rp {gm.Money:N0}");
            sb.AppendLine($"Hari gagal (0 bintang): {gm.FailedDays} / maks {gm.MaxFailedDays}");
            sb.AppendLine();
            sb.Append("Skill koki: Tangan Cepat ");
            sb.Append(gm.QuickHandsUnlocked ? "(terbuka)" : $"(hari {GameManager.QuickHandsDay})");
            sb.Append(", Tenang ");
            sb.AppendLine(gm.CalmUnlocked ? "(terbuka)" : $"(hari {GameManager.CalmDay})");
            InfoPanel.Show("Papan Target", sb.ToString());
        }
    }
}
