using UnityEngine;

namespace RollingKitchen
{
    /// <summary>Objek yang bisa dipakai pemain dengan tombol E saat berada dekat.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        /// <summary>Teks yang muncul di layar saat pemain dekat.</summary>
        public abstract string GetPrompt();

        /// <summary>Dipanggil saat pemain menekan E.</summary>
        public abstract void Interact();
    }

    /// <summary>Kunci input global: true saat panel UI terbuka sehingga pemain tidak bergerak/berinteraksi.</summary>
    public static class GameInput
    {
        public static bool Locked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad() => Locked = false;
    }
}
