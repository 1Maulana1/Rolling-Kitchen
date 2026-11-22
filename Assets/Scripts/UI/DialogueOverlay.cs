using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RollingKitchen
{
    /// <summary>Satu baris dialog cutscene.</summary>
    public struct DialogueLine
    {
        public string speaker;
        public string text;
        public Color color; // warna potret (placeholder sampai ada gambar potret)

        public DialogueLine(string speaker, string text, Color color)
        {
            this.speaker = speaker;
            this.text = text;
            this.color = color;
        }
    }

    /// <summary>
    /// Cutscene sederhana: potret + nama + kotak dialog di bawah layar. Lanjut dengan klik, E, Spasi, atau Enter;
    /// Esc melewati semuanya. Input permainan dikunci selama tampil.
    /// </summary>
    public class DialogueOverlay : MonoBehaviour
    {
        public static bool IsOpen => instance != null && instance.root.activeSelf;

        public static readonly Color MeColor = new Color(0.95f, 0.55f, 0.45f);
        public static readonly Color MomColor = new Color(0.55f, 0.7f, 0.9f);

        private static DialogueOverlay instance;
        private GameObject root;
        private Image portrait;
        private TMP_Text initial, nameText, bodyText, hintText;
        private readonly Queue<DialogueLine> lines = new Queue<DialogueLine>();
        private Action onDone;
        private int openedFrame;

        public static void Play(IEnumerable<DialogueLine> script, Action done = null)
        {
            if (instance == null)
            {
                var go = new GameObject("DialogueOverlay");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<DialogueOverlay>();
                instance.Build();
            }
            instance.lines.Clear();
            foreach (var l in script) instance.lines.Enqueue(l);
            instance.onDone = done;
            instance.openedFrame = Time.frameCount;
            instance.root.SetActive(true);
            GameInput.Locked = true;
            instance.Next();
        }

        private void Next()
        {
            if (lines.Count == 0) { Finish(); return; }
            var l = lines.Dequeue();
            portrait.color = l.color;
            initial.text = string.IsNullOrEmpty(l.speaker) ? "" : l.speaker.Substring(0, 1);
            nameText.text = l.speaker;
            bodyText.text = l.text;
            hintText.text = lines.Count > 0 ? "Klik / E / Spasi: lanjut     Esc: lewati" : "Klik / E / Spasi: selesai";
        }

        private void Finish()
        {
            root.SetActive(false);
            GameInput.Locked = false;
            var cb = onDone;
            onDone = null;
            cb?.Invoke();
        }

        private void Update()
        {
            if (!root.activeSelf || Time.frameCount == openedFrame) return; // tombol yang membuka dialog tidak ikut melanjutkan
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) { lines.Clear(); Finish(); return; }
            bool next = (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                        || (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (next) Next();
        }

        private void Build()
        {
            var canvas = RuntimeUi.Canvas("Dialog Canvas", 90, transform);
            root = new GameObject("Dialog", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvas.transform, false);
            RuntimeUi.Stretch((RectTransform)root.transform);
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f); // menahan klik ke dunia di belakang

            var box = new GameObject("Kotak", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(root.transform, false);
            box.GetComponent<Image>().color = new Color(0.1f, 0.08f, 0.08f, 1f);
            var brt = (RectTransform)box.transform;
            brt.anchorMin = new Vector2(0.08f, 0f);
            brt.anchorMax = new Vector2(0.92f, 0f);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = new Vector2(0, 40);
            brt.sizeDelta = new Vector2(0, 260);

            var p = new GameObject("Potret", typeof(RectTransform), typeof(Image));
            p.transform.SetParent(box.transform, false);
            portrait = p.GetComponent<Image>();
            var prt = (RectTransform)p.transform;
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0, 0.5f);
            prt.anchoredPosition = new Vector2(24, 0);
            prt.sizeDelta = new Vector2(210, 210);
            initial = RuntimeUi.Text(p.transform, "", 110);
            RuntimeUi.Stretch(initial.rectTransform);
            initial.color = new Color(0f, 0f, 0f, 0.45f);

            nameText = RuntimeUi.Text(box.transform, "", 34, TextAlignmentOptions.TopLeft);
            Place(nameText.rectTransform, 260, -20, 50);
            nameText.color = new Color(1f, 0.85f, 0.4f);
            bodyText = RuntimeUi.Text(box.transform, "", 30, TextAlignmentOptions.TopLeft);
            Place(bodyText.rectTransform, 260, -74, 140);
            hintText = RuntimeUi.Text(box.transform, "", 20, TextAlignmentOptions.BottomRight);
            Place(hintText.rectTransform, 260, -224, 30);
            hintText.color = new Color(1f, 1f, 1f, 0.5f);
            root.SetActive(false);
        }

        private static void Place(RectTransform rt, float left, float top, float height)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0, 1);
            rt.offsetMin = new Vector2(left, top - height);
            rt.offsetMax = new Vector2(-24, top);
        }
    }
}
