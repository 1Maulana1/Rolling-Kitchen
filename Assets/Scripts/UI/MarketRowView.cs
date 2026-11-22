using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RollingKitchen
{
    /// <summary>Satu baris bahan di layar Pasar: nama (+ jumlah dimiliki), harga + tren, sisa stok pedagang, tombol beli.</summary>
    public class MarketRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private TMP_Text stockText;
        [SerializeField] private Button buyButton;

        public IngredientData Ingredient { get; private set; }

        public void Bind(IngredientData item, Action<IngredientData> onBuy)
        {
            Ingredient = item;
            if (nameText) nameText.text = item.displayName;
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => onBuy(item));
        }

        /// <summary>Perbarui angka. Baris meredup bila stok pedagang habis.</summary>
        public void Refresh(int price, int basePrice, int trend, int vendorStock, int owned, bool canBuy)
        {
            string arrow = trend > 0 ? " <color=#E64D4D>▲</color>" : trend < 0 ? " <color=#4DCC66>▼</color>" : "";
            string old = basePrice > price ? $"<size=75%><color=#999999><s>{basePrice:N0}</s></color></size> " : "";
            if (priceText) priceText.text = $"{old}Rp {price:N0}{arrow}";

            if (nameText)
                nameText.text = owned > 0
                    ? $"{Ingredient.displayName} <size=70%><color=#AAAAAA>punya {owned}</color></size>"
                    : Ingredient.displayName;

            // Sisa sedikit = harga mahal, jadi diberi warna peringatan.
            if (stockText)
                stockText.text = vendorStock <= 0 ? "<color=#E64D4D>Habis</color>"
                               : vendorStock <= 3 ? $"<color=#FFB040>Sisa {vendorStock}</color>"
                               : $"Sisa {vendorStock}";
            if (buyButton) buyButton.interactable = canBuy;

            var c = vendorStock > 0 ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            if (nameText) nameText.color = c;
            if (priceText) priceText.color = c;
        }
    }
}
