using UnityEngine;

namespace SparkAge.Game
{
    /// <summary>
    ///临时占位：运行时生成六边形贴图，不用美术素材。换真实美术后删掉。
    ///</summary>
    public static class HexSpriteFactory
    {
        //生成六边形精灵
        public static Sprite CreateHexSprite(int size, Color color)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = size / 2f;
            var center = new Vector2(r, r);
            //修改纹理，对每个像素点遍历
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, IsInHex(new Vector2(x + 0.5f, y + 0.5f), center, r) ? color : Color.clear);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }
        //生成六边形环精灵
        public static Sprite CreateHexCircleSprite(int size, Color color)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r1 = size / 2f, r2 = size / 2f - 4;
            var center = new Vector2(r1, r1);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, IsInHex(new Vector2(x + 0.5f, y + 0.5f), center, r1) && !IsInHex(new Vector2(x + 0.5f, y + 0.5f), center, r2) ? color : Color.clear);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }

        // 判断点是否在"尖朝上"的六边形内
        static bool IsInHex(Vector2 p, Vector2 c, float r)
        {
            Vector2 d = p - c;
            float halfWidth = r * 0.8660254f;              // 半宽 = r * √3/2
            if (Mathf.Abs(d.x) > halfWidth) return false;
            float limit = r - Mathf.Abs(d.x) / 1.7320508f; // 上下边界随 x 收窄
            return Mathf.Abs(d.y) <= limit;
        }
    }
}