using TMPro;
using UnityEngine;

namespace RollingKitchen
{
    /// <summary>HUD sederhana: hari, fase, dan uang dari event GameManager.</summary>
    public class HudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text dayText;
        [SerializeField] private TMP_Text phaseText;
        [SerializeField] private TMP_Text moneyText;

        private GameManager gm;

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

            // Tampilkan nilai awal.
            OnDayChanged(gm.Day);
            OnPhaseChanged(gm.Phase);
            OnMoneyChanged(gm.Money);
        }

        private void OnDisable()
        {
            if (gm == null) return;
            gm.DayChanged -= OnDayChanged;
            gm.PhaseChanged -= OnPhaseChanged;
            gm.MoneyChanged -= OnMoneyChanged;
            gm = null;
        }

        private void OnDayChanged(int day)
        {
            if (dayText) dayText.text = $"Hari {day}/{gm.TotalDays}";
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            if (phaseText) phaseText.text = phase == GamePhase.SiangSore ? "Siang/Sore" : phase.ToString();
        }

        private void OnMoneyChanged(int money)
        {
            if (moneyText) moneyText.text = $"Rp {money:N0}";
        }
    }
}
