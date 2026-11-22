using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RollingKitchen
{
    /// <summary>Satu baris bahan di layar Pasar: nama, harga + tren, stok, tombol beli.</summary>
    public class MarketRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private TMP_Text stockText;
        [SerializeField] private Button buyButton;

        private static readonly Color Up = new Color(0.9f, 0.3f, 0.3f);    // harga naik = merah
        private static readonly Color Down = new Color(0.3f, 0.8f, 0.4f);  // harga turun = hijau

        public IngredientData Ingredient { get; private set; }

        public void Bind(IngredientData item, Action<IngredientData> onBuy)
        {
            Ingredient = item;
            if (nameText) nameText.text = item.displayName;
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => onBuy(item));
        }

        /// <summary>Perbarui angka; baris meredup bila stok 0 (sesuai GDD).</summary>
        public void Refresh(int price, int basePrice, int trend, int stock, bool canAfford)
        {
            string arrow = trend > 0 ? " <color=#E64D4D>▲</color>" : trend < 0 ? " <color=#4DCC66>▼</color>" : "";
            string old = basePrice > price ? $"<size=75%><color=#999999><s>{basePrice:N0}</s></color></size> " : "";
            if (priceText) priceText.text = $"{old}Rp {price:N0}{arrow}";
            if (stockText) stockText.text = $"x{stock}";
            if (buyButton) buyButton.interactable = canAfford;

            var c = (stock > 0) ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            if (nameText) nameText.color = c;
            if (stockText) stockText.color = c;
        }
    }
}
