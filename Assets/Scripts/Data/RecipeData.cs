using System;
using UnityEngine;

namespace RollingKitchen
{
    /// <summary>Alat masak di Food Truck.</summary>
    public enum ToolType { Penggorengan, Panci, Talenan }

    /// <summary>Data satu resep makanan.</summary>
    [CreateAssetMenu(menuName = "Rolling Kitchen/Recipe", fileName = "NewRecipe")]
    public class RecipeData : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public IngredientData ingredient;
            [Min(1)] public int amount;
        }

        public string displayName;
        public Sprite icon;
        [Tooltip("Resep Spesial: tidak dimiliki di awal, dibuka lewat Eksperimen Dapur.")]
        public bool isSpecial;
        [Min(0)] public int sellPrice;
        public ToolType tool;
        [Min(0f)] public float cookSeconds = 5f;
        public Entry[] ingredients;
    }
}
