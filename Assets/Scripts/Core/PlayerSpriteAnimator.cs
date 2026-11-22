using UnityEngine;

namespace RollingKitchen
{
    /// <summary>
    /// Animasi jalan 4 arah untuk pemain dari lembar 4 baris x 3 frame
    /// (urutan frames: bawah 0-2, kiri 3-5, kanan 6-8, atas 9-11; frame 0 tiap arah = diam).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames = new Sprite[12];
        [SerializeField] private float framesPerSecond = 8f;

        private SpriteRenderer sr;
        private Rigidbody2D rb;
        private int dir;      // 0 bawah, 1 kiri, 2 kanan, 3 atas
        private float clock;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (frames == null || frames.Length < 12) return;
            Vector2 v = rb ? rb.linearVelocity : Vector2.zero;
            bool moving = v.sqrMagnitude > 0.01f;
            if (moving)
            {
                // Arah dominan; diagonal memakai sumbu yang lebih besar (horizontal menang saat seimbang).
                if (Mathf.Abs(v.x) >= Mathf.Abs(v.y)) dir = v.x < 0 ? 1 : 2;
                else dir = v.y < 0 ? 0 : 3;
                clock += Time.deltaTime * framesPerSecond;
            }
            else clock = 0f;

            // Siklus langkah: kiri, diam, kanan, diam.
            int[] cycle = { 1, 0, 2, 0 };
            int f = moving ? cycle[(int)clock % cycle.Length] : 0;
            sr.sprite = frames[dir * 3 + f];
        }
    }
}
