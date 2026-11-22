using UnityEngine.SceneManagement;

namespace RollingKitchen
{
    /// <summary>Daftar scene permainan (nama enum = nama file scene).</summary>
    public enum GameScene { TitleScreen, Rumah, Pasar, FoodTruck, Rekap }

    /// <summary>Utilitas statis untuk pindah scene.</summary>
    public static class SceneLoader
    {
        public static void Load(GameScene scene) => SceneManager.LoadScene(scene.ToString());

        // Versi string agar bisa dipanggil dari UnityEvent tombol: "Rumah", "Pasar", dst.
        public static void Load(string sceneName) => SceneManager.LoadScene(sceneName);
    }
}
