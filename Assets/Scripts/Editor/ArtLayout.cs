using UnityEditor;
using UnityEngine;

namespace RollingKitchen.EditorTools
{
    /// <summary>
    /// Bantuan untuk scene yang dibangun di atas gambar latar pixel art:
    /// impor sprite dengan setelan pixel art, dan konversi koordinat piksel gambar ke unit dunia.
    /// </summary>
    internal readonly struct ArtLayout
    {
        public readonly float Width, Height, Ppu;

        public ArtLayout(float width, float height, float ppu)
        {
            Width = width; Height = height; Ppu = ppu;
        }

        /// <summary>Ukuran gambar dalam unit dunia.</summary>
        public Vector2 WorldSize => new Vector2(Width / Ppu, Height / Ppu);

        /// <summary>Titik piksel (asal kiri-atas, y ke bawah) ke dunia (tengah gambar = 0,0).</summary>
        public Vector2 ToWorld(float px, float py) => new Vector2((px - Width * 0.5f) / Ppu, -(py - Height * 0.5f) / Ppu);

        /// <summary>Kotak piksel (x1,y1)-(x2,y2) ke titik tengah dan ukuran dunia.</summary>
        public void Rect(float x1, float y1, float x2, float y2, out Vector2 center, out Vector2 size)
        {
            center = ToWorld((x1 + x2) * 0.5f, (y1 + y2) * 0.5f);
            size = new Vector2((x2 - x1) / Ppu, (y2 - y1) / Ppu);
        }

        /// <summary>Collider statis tak terlihat dari kotak piksel.</summary>
        public void Solid(Transform parent, string name, float x1, float y1, float x2, float y2)
        {
            Rect(x1, y1, x2, y2, out var c, out var s);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = c;
            go.AddComponent<BoxCollider2D>().size = s;
        }

        /// <summary>Atur importer sebagai sprite pixel art (Point, tanpa kompresi/mipmap) lalu muat spritenya.</summary>
        public static Sprite ImportSprite(string path, float ppu)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null)
            {
                Debug.LogError($"Gambar tidak ditemukan: {path}");
                return null;
            }
            bool dirty = imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single
                || !Mathf.Approximately(imp.spritePixelsPerUnit, ppu) || imp.filterMode != FilterMode.Point
                || imp.textureCompression != TextureImporterCompression.Uncompressed || imp.mipmapEnabled;
            if (dirty)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = ppu;
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                var s = new TextureImporterSettings();
                imp.ReadTextureSettings(s);
                s.spriteMeshType = SpriteMeshType.FullRect;
                s.spriteAlignment = (int)SpriteAlignment.Center;
                imp.SetTextureSettings(s);
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Objek sprite (tanpa collider) di titik piksel tertentu.</summary>
        public GameObject Picture(Transform parent, string name, Sprite sprite, Vector2 worldPos, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return go;
        }
    }
}
