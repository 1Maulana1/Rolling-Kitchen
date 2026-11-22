using UnityEngine;

namespace RollingKitchen
{
    /// <summary>Dapur di Rumah: membuka layar Eksperimen Dapur, hanya pada fase Siang-Sore.</summary>
    public class KitchenStation : Interactable
    {
        [SerializeField] private ExperimentView view;

        private bool PhaseOk => GameManager.Instance == null || GameManager.Instance.Phase == GamePhase.SiangSore;
        private bool LevelOk => GameManager.Instance == null || GameManager.Instance.Config.allowExperiment;
        private bool Available => PhaseOk && LevelOk;

        public override string GetPrompt()
        {
            if (!LevelOk) return "Dapur (eksperimen belum tersedia hari ini)";
            return PhaseOk ? "E: Eksperimen Dapur" : "Dapur (hanya Siang-Sore)";
        }

        public override void Interact()
        {
            if (Available && view != null) view.Open();
        }
    }
}
