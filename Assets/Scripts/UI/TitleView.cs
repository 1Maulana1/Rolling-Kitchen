using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RollingKitchen
{
    /// <summary>
    /// Layar judul: Mulai membuka pilihan 3 slot. Tiap slot bisa Lanjut (bila ada simpanan)
    /// atau Main Baru (menimpa simpanan butuh klik kedua untuk konfirmasi).
    /// </summary>
    public class TitleView : MonoBehaviour
    {
        [Serializable]
        public class SlotRow
        {
            public TMP_Text info;
            public Button continueButton;
            public Button newButton;
            public TMP_Text newLabel;
        }

        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject slotPanel;
        [SerializeField] private SlotRow[] slots = new SlotRow[SaveSystem.SlotCount];

        private int confirmSlot = -1;

        private void Start()
        {
            Time.timeScale = 1f;
            GameInput.Locked = false;
            CloseSlots();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && slotPanel.activeSelf && !OptionsMenu.OwnsEscape) CloseSlots();
        }

        public void OpenOptions() => OptionsMenu.Open();

        public void OpenSlots()
        {
            confirmSlot = -1;
            Refresh();
            menuPanel.SetActive(false);
            slotPanel.SetActive(true);
        }

        public void CloseSlots()
        {
            slotPanel.SetActive(false);
            menuPanel.SetActive(true);
        }

        public void ContinueSlot(int slot)
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.LoadGame(slot)) { Refresh(); return; }
            SceneLoader.Load(GameScene.Rumah);
        }

        public void NewGameInSlot(int slot)
        {
            // Slot berisi: klik pertama hanya meminta konfirmasi.
            if (SaveSystem.Exists(slot) && confirmSlot != slot)
            {
                confirmSlot = slot;
                Refresh();
                return;
            }
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.NewGame(slot);
            SceneLoader.Load(GameScene.Rumah);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Refresh()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var row = slots[i];
                if (row == null) continue;
                var d = SaveSystem.Read(i);
                row.info.text = d == null
                    ? $"<b>Slot {i + 1}</b>\nKosong"
                    : $"<b>Slot {i + 1}</b>   Hari {d.day} - {PhaseName(d.phase)}\nRp {d.money:N0}   <size=80%>disimpan {d.savedAt}</size>";
                row.continueButton.interactable = d != null;
                row.newLabel.text = confirmSlot == i ? "Yakin timpa?" : "Main Baru";
            }
        }

        public static string PhaseName(GamePhase p) => p == GamePhase.SiangSore ? "Siang-Sore" : p.ToString();
    }
}
