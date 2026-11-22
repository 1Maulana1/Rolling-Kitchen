using UnityEngine;

namespace RollingKitchen
{
    /// <summary>Kasur di Rumah: simpan permainan ke slot aktif kapan saja.</summary>
    public class Bed : Interactable
    {
        private float savedMessageUntil;

        public override string GetPrompt()
        {
            var gm = GameManager.Instance;
            if (Time.unscaledTime < savedMessageUntil) return "Tersimpan!";
            if (gm == null || gm.CurrentSlot < 0) return "Kasur (simpan hanya bila main dari Title)";
            return $"E: Simpan permainan (Slot {gm.CurrentSlot + 1})";
        }

        public override void Interact()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.SaveCurrent()) savedMessageUntil = Time.unscaledTime + 1.5f;
        }
    }
}
