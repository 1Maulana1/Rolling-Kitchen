using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RollingKitchen
{
    /// <summary>Gerak top-down (WASD/panah); tekan E di dekat objek Interactable untuk memakainya.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [SerializeField] private float speed = 5f;
        [SerializeField] private float interactRange = 1.6f;
        [SerializeField] private TMP_Text promptText;

        private Rigidbody2D rb;
        private Vector2 input;
        private Interactable[] targets;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        private void Start() => targets = FindObjectsByType<Interactable>();

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (GameInput.Locked)
            {
                input = Vector2.zero;
                SetPrompt(null);
                return;
            }

            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
            float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
            input = new Vector2(x, y).normalized;

            var nearest = FindNearest();
            SetPrompt(nearest != null ? nearest.GetPrompt() : null);
            if (nearest != null && kb.eKey.wasPressedThisFrame) nearest.Interact();
        }

        private void FixedUpdate() => rb.linearVelocity = input * speed;

        private Interactable FindNearest()
        {
            Interactable best = null;
            float bestDist = interactRange;
            foreach (var t in targets)
            {
                if (t == null) continue;
                float d = Vector2.Distance(transform.position, t.transform.position);
                if (d <= bestDist) { bestDist = d; best = t; }
            }
            return best;
        }

        private void SetPrompt(string text)
        {
            if (!promptText) return;
            promptText.gameObject.SetActive(!string.IsNullOrEmpty(text));
            if (text != null) promptText.text = text;
        }
    }
}
