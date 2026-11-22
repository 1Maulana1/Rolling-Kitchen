using System.Linq;
using System.Text;
using UnityEngine;

namespace RollingKitchen
{
    /// <summary>Buku resep di ruang tamu: resep dasar lengkap, resep spesial terbuka, dan petunjuk samar yang belum.</summary>
    public class RecipeBookStand : Interactable
    {
        [SerializeField] private RecipeData[] recipes;

        public override string GetPrompt() => "E: Baca buku resep";

        public override void Interact()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var list = recipes != null && recipes.Length > 0 ? recipes
                     : ItemDatabase.Instance != null ? ItemDatabase.Instance.recipes : new RecipeData[0];

            var sb = new StringBuilder();
            foreach (var r in list.Where(r => r != null).OrderBy(r => r.isSpecial).ThenBy(r => r.sellPrice))
            {
                bool known = !r.isSpecial || gm.Recipes.IsUnlocked(r);
                if (known)
                {
                    string items = r.ingredients == null ? "" : string.Join(" + ",
                        r.ingredients.Where(e => e.ingredient != null)
                                     .Select(e => e.amount > 1 ? $"{e.amount} {e.ingredient.displayName}" : e.ingredient.displayName));
                    sb.AppendLine($"<b>{r.displayName}</b>{(r.isSpecial ? " <color=#FFD24D>(spesial)</color>" : "")}  Rp {r.sellPrice}  [{r.tool}]");
                    sb.AppendLine($"   {items}");
                }
                else
                {
                    // Petunjuk samar: jumlah bahan dan satu bahan pertama.
                    int n = r.ingredients?.Count(e => e.ingredient != null) ?? 0;
                    var first = r.ingredients?.FirstOrDefault(e => e.ingredient != null).ingredient;
                    sb.AppendLine($"<color=#999999><b>??? (resep spesial)</b>  {n} bahan, salah satunya {(first ? first.displayName : "?")}. " +
                                  "Coba di Dapur.</color>");
                }
            }
            InfoPanel.Show("Buku Resep", sb.ToString());
        }
    }
}
