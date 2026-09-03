using System;

namespace SparkAge.Model.Map
{
    /// <summary>
    /// 纯 C# 值噪声：先在几个"随机控制点"上撒随机值，
    /// 再在它们之间平滑插值 → 相邻格子得到接近的数值 → 地形成片。
    /// </summary>
    public static class ValueNoise
    {
        static float[,] _grid;
        static int _n;

        public static void Generate(int seed, int gridSize)
        {
            _n = gridSize;
            _grid = new float[gridSize, gridSize];
            var rng = new Random(seed);
            for (int y = 0; y < gridSize; y++)
                for (int x = 0; x < gridSize; x++)
                    _grid[x, y] = (float)rng.NextDouble(); // 控制点：0~1 随机值
        }

        // 给格子 (q, r) 返回一个 0~1 的噪声值
        public static float Sample(int q, int r, int width, int height)
        {
            // 把格子坐标缩放到控制点网格上
            float fx = (float)q / (width - 1) * (_n - 1);
            float fy = (float)r / (height - 1) * (_n - 1);

            int x0 = (int)fx, y0 = (int)fy;
            int x1 = Math.Min(x0 + 1, _n - 1);
            int y1 = Math.Min(y0 + 1, _n - 1);
            float tx = fx - x0, ty = fy - y0;
            tx = tx * tx * (3 - 2 * tx); // smoothstep：让过渡更自然
            ty = ty * ty * (3 - 2 * ty);

            float v00 = _grid[x0, y0], v10 = _grid[x1, y0];
            float v01 = _grid[x0, y1], v11 = _grid[x1, y1];
            float top = v00 + (v10 - v00) * tx;
            float bottom = v01 + (v11 - v01) * tx;
            return top + (bottom - top) * ty;
        }
    }
}