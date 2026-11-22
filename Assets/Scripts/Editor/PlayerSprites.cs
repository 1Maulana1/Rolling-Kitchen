using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace RollingKitchen.EditorTools
{
    /// <summary>
    /// Mengimpor lembar sprite koki (ArtSource/Characters/koki.js -> Assets/Art/Characters/koki.png)
    /// dan memotongnya jadi 12 frame 16x24: bawah 0-2, kiri 3-5, kanan 6-8, atas 9-11.
    /// </summary>
    public static class PlayerSprites
    {
        public const string SheetPath = "Assets/Art/Characters/koki.png";
        public const float Ppu = 32f;
        private const int FrameW = 16, FrameH = 24, Cols = 3, Rows = 4;
        private static readonly string[] DirNames = { "bawah", "kiri", "kanan", "atas" };

        /// <summary>12 sprite urut sesuai PlayerSpriteAnimator, atau null bila lembar belum ada.</summary>
        public static Sprite[] Load()
        {
            var imp = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
            if (imp == null) return null;
            Slice(imp);

            var sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().ToDictionary(s => s.name);
            var result = new Sprite[Cols * Rows];
            for (int i = 0; i < result.Length; i++)
                if (!sprites.TryGetValue(FrameName(i), out result[i])) return null;
            return result;
        }

        private static string FrameName(int i) => $"koki_{DirNames[i / Cols]}_{i % Cols}";

        private static void Slice(TextureImporter imp)
        {
            bool already = imp.spriteImportMode == SpriteImportMode.Multiple && imp.spritePixelsPerUnit == Ppu
                           && AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().Count() == Cols * Rows;
            if (already) return;

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Multiple;
            imp.spritePixelsPerUnit = Ppu;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dp = factory.GetSpriteEditorDataProviderFromObject(imp);
            dp.InitSpriteEditorDataProvider();

            var rects = new List<SpriteRect>();
            for (int i = 0; i < Cols * Rows; i++)
            {
                int col = i % Cols, row = i / Cols;
                // Rect sprite diukur dari kiri-bawah tekstur; baris 0 (bawah/menghadap kamera) ada di atas lembar.
                rects.Add(new SpriteRect
                {
                    name = FrameName(i),
                    rect = new Rect(col * FrameW, (Rows - 1 - row) * FrameH, FrameW, FrameH),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = GUID.Generate(),
                });
            }
            dp.SetSpriteRects(rects.ToArray());
            var names = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
            dp.Apply();
            imp.SaveAndReimport();
        }
    }
}
