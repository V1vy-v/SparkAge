using UnityEngine;

namespace SparkAge.Core.Hex
{
    public static class HexLayout
    {
        // 轴向坐标 → 世界位置（pointy-top）
        public static Vector3 HexToPixel(HexCoord h, float size, float height)
        {
            float x = size * (Mathf.Sqrt(3f) * h.Q + Mathf.Sqrt(3f) / 2f * h.R);
            float y = size * (1.5f * h.R);
            return new Vector3(x, height, y);
        }

        // 世界位置 → 轴向坐标（cube 四舍五入）
        public static HexCoord PixelToHex(Vector3 p, float size)
        {
            float q = (Mathf.Sqrt(3f) / 3f * p.x - 1f / 3f * p.z) / size;
            float r = (2f / 3f * p.z) / size;
            return CubeRound(q, -q - r,  r);
        }

        static HexCoord CubeRound(float x, float y, float z)
        {
            int rx = Mathf.RoundToInt(x);
            int ry = Mathf.RoundToInt(y);
            int rz = Mathf.RoundToInt(z);
            float dx = Mathf.Abs(rx - x), dy = Mathf.Abs(ry - y), dz = Mathf.Abs(rz - z);
            if (dx > dy && dx > dz) rx = -ry - rz;
            else if (dy > dz) ry = -rx - rz;
            else rz = -rx - ry;
            return new HexCoord(rx, rz);
        }
    }
}