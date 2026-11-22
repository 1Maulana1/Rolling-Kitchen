using UnityEngine;

namespace RollingKitchen
{
    /// <summary>Pintu ke scene lain. Hanya bisa dipakai pada fase yang dicentang.</summary>
    public class Door : Interactable
    {
        [SerializeField] private GameScene target;
        [SerializeField] private string label = "Pintu";
        [SerializeField] private bool openInPagi;
        [SerializeField] private bool openInSiangSore;
        [SerializeField] private bool openInMalam;

        private bool IsOpen
        {
            get
            {
                var gm = GameManager.Instance;
                if (gm == null) return true; // uji scene tanpa GameManager
                switch (gm.Phase)
                {
                    case GamePhase.Pagi: return openInPagi;
                    case GamePhase.SiangSore: return openInSiangSore;
                    default: return openInMalam;
                }
            }
        }

        public override string GetPrompt() => IsOpen ? $"E: {label}" : $"{label} (belum bisa sekarang)";

        public override void Interact()
        {
            if (IsOpen) SceneLoader.Load(target);
        }
    }
}
