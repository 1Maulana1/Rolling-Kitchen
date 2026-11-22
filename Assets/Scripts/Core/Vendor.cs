using UnityEngine;

namespace RollingKitchen
{
    /// <summary>Pedagang yang bisa diajak bicara (tekan E). Menjual dagangan zona tempatnya berada.</summary>
    public class Vendor : Interactable
    {
        [SerializeField] private PasarZone zone;
        private MarketView view;

        public PasarZone Zone => zone;

        public override string GetPrompt() => $"E: Beli dari {zone.Title}";

        public override void Interact()
        {
            if (view == null) view = FindAnyObjectByType<MarketView>();
            if (view != null) view.Open(zone);
        }
    }
}
