using UnityEngine;

namespace RollingKitchen
{
    /// <summary>
    /// Kamera ortografik mengikuti target, dijepit agar tidak keluar dari batas latar.
    /// Bila latar lebih kecil dari layar pada satu sumbu, kamera dipusatkan di sumbu itu.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Rect bounds = new Rect(-15f, -6.25f, 30f, 12.5f);
        [SerializeField, Min(0f)] private float smoothTime = 0.12f;

        private Camera cam;
        private Vector3 velocity;

        private void Awake() => cam = GetComponent<Camera>();

        private void Start()
        {
            if (target) transform.position = Clamp(target.position); // tanpa meluncur saat scene dibuka
        }

        private void LateUpdate()
        {
            if (!target) return;
            var goal = Clamp(target.position);
            transform.position = smoothTime > 0f
                ? Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime)
                : goal;
        }

        private Vector3 Clamp(Vector3 p)
        {
            float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
            float x = bounds.width <= halfW * 2f ? bounds.center.x : Mathf.Clamp(p.x, bounds.xMin + halfW, bounds.xMax - halfW);
            float y = bounds.height <= halfH * 2f ? bounds.center.y : Mathf.Clamp(p.y, bounds.yMin + halfH, bounds.yMax - halfH);
            return new Vector3(x, y, transform.position.z);
        }
    }
}
